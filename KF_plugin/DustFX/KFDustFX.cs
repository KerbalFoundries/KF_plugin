﻿/* DustFX is a derivative work based on CollisionFX by Pizzaoverhead.
 * Mofications have been made to stip out the spark system and strengthen the options
 * in customization for the dust effects purely for the use in wheels.  Previously,
 * A wheel would spark and dust whenever it rolled anywhere.  Now there is only dust, which
 * would be expected when rolling over a surface at the speeds we do in KSP.
 * Much of the config node system would have been impossible without the help of
 * xEvilReeperx who was very patient with my complete ignorance, and whom I may
 * return to in the future to further optimize the module as a whole.
 * 
 * Best used with Kerbal Foundries by lo-fi.  Reinventing the Wheel, quite literally!
 * Much thanks to xEvilReeperx for fixing the things I broke, without whom there would be no
 * config-node settings file, nor would I be sane enough to live alone... ever!
 * 
 * The end state of this code has nearly been rewritten completely and integrated into the mod
 * that it was meant to be used with: Kerbal Foundries.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KerbalFoundries;

namespace KerbalFoundries.DustFX
{
	/// <summary>DustFX class which is based on, but heavily modified from, CollisionFX by pizzaoverload.</summary>
	public class KFDustFX : PartModule
	{
		// Class-wide disabled warnings in SharpDevelop
		// disable AccessToStaticMemberViaDerivedType
		// disable RedundantDefaultFieldInitializer
		
		KFRepulsor _repModule;
		
		readonly KFLogUtil KFLog = new KFLogUtil("KFDustFX");
		
		/// <summary>The camera object we're using to get color info directly from the terrain.</summary>
		KFModuleCameraShot _ModuleCameraShot;
		
		/// <summary>Config field to compensate for part size.</summary>
		[KSPField]
		public float partSizeFactor = 1f;
		
		/// <summary>Local copy of the tweakScaleCorrector parameter in KFModuleWheel.</summary>
		/// <remarks>Passed here by the wheel module itself.</remarks>
		public float tweakScaleFactor = 1f;
		
		/// <summary>Specifies if the module is to be used for repulsors.</summary>
		/// <remarks>Default is "false"</remarks>
		[KSPField]
		public bool isRepulsor = false;
		
		/// <summary>Minimum scrape speed.</summary>
		/// <remarks>Default is 0.1f.  Repulsors should have this extremely low.</remarks>
		[KSPField]
		public float minScrapeSpeed = 0.1f;
		
		/// <summary>Minimum dust energy value.</summary>
		/// <remarks>Default is 0.1f.  Represents the minimum thickness of the particles.</remarks>
		[KSPField]
		public float minDustEnergy = 0.1f;
		
		/// <summary>Minimum dust energy value.</summary>
		/// <remarks>Default is 1f.  Represents the maximum thickness of the particles.</remarks>
		[KSPField]
		public float maxDustEnergy = 1f;
		
		/// <summary>Minimum emission value of the dust particles.</summary>
		/// <remarks>Default is 0.1f.  This is the number of particles to emit per second.</remarks>
		[KSPField]
		public float minDustEmission = 0.1f;
		
		/// <summary>Maximum emission value of the dust particles.</summary>
		/// <remarks>Default is 20f.  This is the number of particles to emit per second.</remarks>
		[KSPField]
		public float maxDustEmission = 20f;
		
		/// <summary>Minimum size value of the dust particles.</summary>
		/// <remarks>Default is 0.1f.  Represents the size of the particles themselves.</remarks>
		[KSPField]
		public float minDustSize = 0.1f;
		
		/// <summary>Maximum size value of the dust particles.</summary>
		/// <remarks>Default is 2f.  Represents the size of the particles themselves.</remarks>
		[KSPField]
		public float maxDustSize = 2f;
		
		/// <summary>Maximum emission energy divisor.</summary>
		/// <remarks>Default is 2f.  Divides the thickness by the value provided.</remarks>
		[KSPField]
		public float maxDustEnergyDiv = 2f;
		
		/// <summary>Maximum emission multiplier.</summary>
		/// <remarks>Default is 2f.</remarks>
		[KSPField]
		public float maxDustEmissionMult = 2f;
		
		/// <summary>Used in the OnCollisionEnter/Stay methods to define the minimum velocity magnitude to check against.</summary>
		/// <remarks>Default is 2f.  Would set very low for repulsors.</remarks>
		[KSPField]
		public float minVelocityMag = 2f;
		
		/// <summary>KSP path to the effect being used here.  Made into a field so that it can be customized in the future.</summary>
		/// <remarks>Default is "Effects/fx_smokeTrail_light"</remarks>
		[KSPField]
		public string dustEffectObject = "Effects/fx_smokeTrail_light";
		
		/// <summary>Added or subracted from the individual color parameters (rgb) to create a variance in the dust colors.</summary>
		/// <remarks>Should be a number between "0f" and "1f" where "0f" effectively disables the variance.</remarks>
		[KSPField]
		public float colorVariance = 0.24f;
		
		GameObject kfdustFx;
		ParticleAnimator dustAnimator;
		Color colorPrevious, colorBiome, colorAverage, colorCam, colorWater;
		Queue<Color> colorFIFO = new Queue<Color>();
		
		bool isColorOverrideActive, isReady, isPaused;
		
		float sizeFactor = 1f;
		
		GameObject _kfRepLight;
		Light _repLight;
		
		/// <summary>Part Info that will be displayed when part details are shown.</summary>
		/// <remarks>Can be overridden in the module config on a per-part basis.</remarks>
		[KSPField]
		public string strPartInfo = "This part will throw up dust when rolling over the terrain.";
		
		// Basic override for the info class.  Already has its own XML documentation.
		public override string GetInfo()
		{
			return strPartInfo;
		}
		
		/// <summary>Standard startup routine.</summary>
		/// <param name="state">State of the game.</param>
		public override void OnStart(StartState state)
		{
			if (!KFPersistenceManager.isDustEnabled)
				return;
			
			_repModule = part.gameObject.GetComponent<KFRepulsor>();
			
			if (HighLogic.LoadedSceneIsFlight)
			{
				_ModuleCameraShot = vessel.rootPart.gameObject.GetComponent<KFModuleCameraShot>();
				if (Equals(_ModuleCameraShot, null))
				{
					_ModuleCameraShot = vessel.rootPart.gameObject.AddComponent<KFModuleCameraShot>();
					_ModuleCameraShot.StartUp();
				}
				if (KFPersistenceManager.isDustEnabled)
					SetupParticles(isRepulsor);
			}
			
			GameEvents.onGamePause.Add(OnPause);
			GameEvents.onGameUnpause.Add(OnUnpause);
			isReady = true;
		}
		
		/// <summary>Updates stuff.</summary>
		public void Update()
		{
			if (!isReady || isPaused)
				return;
			sizeFactor = tweakScaleFactor * partSizeFactor * KFPersistenceManager.dustAmount;
		}
		
		/// <summary>Defines the particle effects used in this module.</summary>
		public void SetupParticles(bool repulsor)
		{
			if (!isRepulsor && repulsor)
				isRepulsor = true;
			if (isRepulsor && !repulsor)
				repulsor = true;
			
			kfdustFx = (GameObject)GameObject.Instantiate(Resources.Load(dustEffectObject));
			kfdustFx.transform.position = part.transform.position;
			kfdustFx.particleEmitter.localVelocity = Vector3.zero;
			kfdustFx.particleEmitter.useWorldSpace = true;
			kfdustFx.particleEmitter.emit = false;
			kfdustFx.particleEmitter.minEnergy = minDustEnergy;
			kfdustFx.particleEmitter.minEmission = minDustEmission;
			kfdustFx.particleEmitter.minSize = minDustSize;
			dustAnimator = kfdustFx.particleEmitter.GetComponent<ParticleAnimator>();
			if (repulsor)
				SetupRepulsorLights();
		}
		
		void SetupRepulsorLights()
		{
			_kfRepLight = new GameObject("Rep Light");
			_kfRepLight.transform.parent = part.transform;
			_kfRepLight.transform.position = Vector3.zero;
			
			_repLight = _kfRepLight.AddComponent<Light>();
			_repLight.type = LightType.Point;
			_repLight.renderMode = LightRenderMode.ForceVertex;
			_repLight.shadows = LightShadows.None;
			_repLight.range = 0.0f; // Starts out with zero range.
			_repLight.color = Color.clear; // Starts out clear, just in case it gets rendered mistakenly.
			_repLight.intensity = 0.0f; // Starts out with zero intensity.
			_repLight.enabled = true;
		}
		
		/// <summary>Called when the part is scraping over a surface.</summary>
		/// <param name="col">The collider being referenced.</param>
		/// <param name="position">The position of the scape.</param>
		public void WheelEmit(Vector3 position, Collider col)
		{
			if ((!isReady || isPaused || Equals(part, null)) || Equals(part.rigidbody, null))
				return;
			float fMagnitude = part.rigidbody.velocity.magnitude;
			DustParticles(fMagnitude, position + (part.rigidbody.velocity * Time.deltaTime), col);
		}
		
		/// <summary>Contains information about what to do when the part stays in the collided state over a period of time.</summary>
		/// <param name="hitPoint">Point at which the collision takes place.</param>
		/// <param name="col">The collider being referenced.</param>
		/// <param name="force">Force of the hit.</param>
		/// <param name="normal">I got nothing here.</param>
		/// <param name="direction">Emission direction..</param>
		public void RepulsorEmit(Vector3 hitPoint, Collider col, float force, Vector3 normal, Vector3 direction)
		{
			if (!isReady || isPaused)
				return;
			isColorOverrideActive |= string.Equals("ModuleWaterSlider.Collider", col.gameObject.name);
			DustParticles(force, hitPoint + (part.rigidbody.velocity * Time.deltaTime), col, normal, direction);
		}
		
		/// <summary>Sets up and/or updates the repulsor lights.</summary>
		/// <param name="squish">How squishy are we?</param>
		public void RepulsorLight(float squish)
		{
			if (!isReady || isPaused)
				return;
			if (isRepulsor && KFPersistenceManager.isRepLightEnabled)
			{
				if (_repModule.fRideHeight > 0f)
				{
					_kfRepLight.transform.localPosition = Vector3.zero;
					_repLight.intensity = Mathf.Clamp(squish, 0.4f, 0.8f);
					_repLight.range = Mathf.Clamp((squish * 2f), 1f, 4f);
					_repLight.color = Color.blue;
				}
				else
				{
					_repLight.intensity = 0.0f; // Zero intensity.
					_repLight.range = 0.0f; // Zero range.
					_repLight.color = Color.clear; // Make the light color go away completely.
				}
			}
			else // Implied: isRepLightEnabled = false and/or isRepulsor = false
			{
				_repLight.intensity = 0.0f;
				_repLight.range = 0.0f;
				_repLight.color = Color.clear;
			}
		}
		
		/// <summary>This creates and maintains the dust particles and their body/biome specific colors.</summary>
		/// <param name="speed">Speed of the part which is scraping.</param>
		/// <param name="contactPoint">The point at which the collider and the scraped surface make contact.</param>
		/// <param name="col">The collider being referenced.</param>
		public void DustParticles(float speed, Vector3 contactPoint, Collider col)
		{
			// disable ConvertToConstant.Local
			
			if (speed < minScrapeSpeed || Equals(dustAnimator, null) || !KFPersistenceManager.isDustEnabled || !isReady || isPaused)
				return;
			
			bool cameraEnabled = KFPersistenceManager.isDustCameraEnabled;
			colorWater = new Color(0.65f, 0.65f, 0.65f, 0.025f);
			
			if (Equals(sizeFactor, 0f) || sizeFactor < 0f)
				sizeFactor = 1f;
			
			colorBiome = KFDustFXUtils.GetDustColor(vessel.mainBody, col, vessel.latitude, vessel.longitude);
			
			cameraEnabled |= Equals(colorBiome, null);
			if (cameraEnabled)
				colorCam = _ModuleCameraShot._averageColour;
			
			Color colorTemp = Color.black;
			
			if (isColorOverrideActive)
				colorTemp = colorWater;
			else if (!cameraEnabled && KFPersistenceManager.dustConfigsPresent)
				colorTemp = colorBiome;
			else if (cameraEnabled && !KFPersistenceManager.dustConfigsPresent)
			{
				colorTemp = colorCam / 2f;
				colorTemp.a = 0.014f;
			}
			else // Implied: camera is enabled, and dust configs are present, and colorOverride is not active.
			{
				colorTemp = (colorCam / 2f) + (colorBiome / 4f); // Make the camera colour dominant.
				colorTemp.a = colorBiome.a;
			}
			
			colorFIFO.Enqueue(colorTemp);
			
			if (colorFIFO.Count > 100)
				colorFIFO.Dequeue();
			
			Color[] colorArray = colorFIFO.ToArray();
			
			float colorRed = 0f;
			float colorGreen = 0f;
			float colorBlue = 0f;
			float colorAlpha = 0f;
			
			const float COLORFULL = 100f;
			const float COLORHALF = 50f;
			
			for (int i = 0; i < colorArray.Count(); i++)
			{
				colorRed += colorArray[i].r;
				colorGreen += colorArray[i].g;
				colorBlue += colorArray[i].b;
				colorAlpha += colorArray[i].a;
			}
			
			colorAverage = new Color(colorRed / COLORFULL, colorGreen / COLORFULL, colorBlue / COLORFULL, colorAlpha / COLORHALF);
			
			if (speed >= minScrapeSpeed)
			{
				if (!Equals(colorAverage, colorPrevious))
				{
					Color[] colors = dustAnimator.colorAnimation;
					for (int i = 0; i < 5; i++)
						colors[i] = colorAverage;
					
					dustAnimator.colorAnimation = colors;
					colorPrevious = colorAverage;
				}
				kfdustFx.transform.position = contactPoint;
				kfdustFx.particleEmitter.maxEnergy = Mathf.Clamp(((speed / maxDustEnergyDiv) * sizeFactor), minDustEnergy, maxDustEnergy * sizeFactor);
				// Energy is the thickness of the particles.
				kfdustFx.particleEmitter.maxEmission = Mathf.Clamp((speed * (maxDustEmissionMult * sizeFactor)), minDustEmission, (maxDustEmission * sizeFactor));
				// Emission is the number of particles emitted per second.
				kfdustFx.particleEmitter.maxSize = Mathf.Clamp((speed * sizeFactor), minDustSize, maxDustSize * sizeFactor);
				// Size is self explanatory.  For wheels, I suggest values between 0.1 and 2.
				kfdustFx.particleEmitter.Emit();
				// I wonder if we'd get better performance if we set the boolean "emit" to true instead of explicitly calling "Emit()" each frame.
			}
		}
		
		/// <summary>Repsulsor overload of DustPartciles.</summary>
		/// <remarks>Adds direction and orientation.</remarks>
		/// <param name="force">Force of the repulsion field's collision with the surface.</param>
		/// <param name="contactPoint">Point of contact.</param>
		/// <param name="col">Collider making contact.</param>
		/// <param name="normal">No idea what this is.</param>
		/// <param name="direction">A direction to emit this stuff.</param>
		void DustParticles(float force, Vector3 contactPoint, Collider col, Vector3 normal, Vector3 direction)
		{
			if (force < minScrapeSpeed || Equals(dustAnimator, null) || !isReady || isPaused)
				return;
			kfdustFx.transform.rotation = Quaternion.Euler(normal);
			kfdustFx.particleEmitter.localVelocity = direction;
			DustParticles(force, contactPoint, col);
			return;
		}
		
		/// <summary>Called when the game enters the "paused" state.</summary>
		void OnPause()
		{
			if (!isPaused && isReady)
			{
				isPaused = true;
				if (kfdustFx.particleEmitter.enabled)
					kfdustFx.particleEmitter.enabled = false;
				if (isRepulsor && _repLight.enabled)
					_repLight.enabled = false;
			}
		}
		
		/// <summary>Called when the game leaves the "paused" state.</summary>
		void OnUnpause()
		{
			if (isPaused && isReady)
			{
				isPaused = false;
				kfdustFx.particleEmitter.enabled |= KFPersistenceManager.isDustEnabled;
				if (isRepulsor)
					_repLight.enabled |= KFPersistenceManager.isRepLightEnabled;
			}
		}
		
		/// <summary>Called when the object being referenced is destroyed, or when the module instance is deactivated.</summary>
		void OnDestroy()
		{
			if (isRepulsor && _repLight.enabled)
				_repLight.enabled = false;
			isReady = false;
			GameEvents.onGamePause.Remove(OnPause);
			GameEvents.onGameUnpause.Remove(OnUnpause);
		}
	}
}
