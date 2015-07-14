/* DustFX is a derivative work based on CollisionFX by Pizzaoverhead.
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
 * 
 * This specific file is a copy of the original DustFX code which will be modified to handle the
 * enabled state of the repulsor field from the KFRepulsor module so that dust is only emitted
 * when the repulsor is actively repulsing.  Possibility to alter dust by the field strength is
 * on the wishlist... scratch that, now it's considered WIP.  "appliedRideheight" is now defined
 * as "Rideheight" divided by two and is used as a multiplier in the min/max dust emission calculations
 * and is clamped to 1 so that we do not actually lower the dust emission below the minimum and over-
 * work the clamp method in the dust emission calculations.
 */

using System;
using UnityEngine;
using KerbalFoundries;

namespace KerbalFoundries
{
	/// <summary>DustFX class which is based on, but heavily modified from, CollisionFX by pizzaoverload.  This is the repulsor version.</summary>
	public class KFRepulsorDustFX : PartModule
	{
		/// <summary>Local definition for the KFRepulsor class.</summary>
		KFRepulsor _KFRepulsor;
		
		/// <summary>Local copy of the Rideheight parameter in the KFRepulsor module.</summary>
		/// <remarks>Maximum value this will ever be is 8, which is the constant maximum for the parameter in the repulsor module.</remarks>
		public float rideHeight;
		
		// Class-wide disabled warnings in SharpDevelop
		// disable AccessToStaticMemberViaDerivedType
		// disable RedundantDefaultFieldInitializer
		
		/// <summary>Mostly unnecessary, since there is no other purpose to having the module active.</summary>
		/// <remarks>Set to false in order to temporarily disable the effect on a specific part.  Default is "true"</remarks>
		[KSPField]
		public bool dustEffects = true;
		
		/// <summary>The camera object we're using to get color info directly from the terrain.</summary>
		ModuleCameraShot _ModuleCameraShot;
		
		/// <summary>Minimum size value of the dust particles.</summary>
		/// <remarks>Default is 0.1.  Represents the size of the particles themselves.</remarks>
		[KSPField]
		public float minDustSize = 0.1f;
		
		/// <summary>Maximum size value of the dust particles.</summary>
		/// <remarks>Default is 2.  Represents the size of the particles themselves.</remarks>
		[KSPField]
		public float maxDustSize = 2f;
		/// <summary>Minimum scrape speed.</summary>
		/// <remarks>Default is 0</remarks>
		[KSPField]
		public float minScrapeSpeed = 0f;
		
		/// <summary>Minimum dust energy value.</summary>
		/// <remarks>Default is 0.1.  Represents the minimum thickness of the particles.</remarks>
		[KSPField]
		public float minDustEnergy = 0.1f;
		
		/// <summary>Minimum dust energy value.</summary>
		/// <remarks>Default is 1.  Represents the maximum thickness of the particles.</remarks>
		[KSPField]
		public float maxDustEnergy = 1f;
		
		/// <summary>Maximum emission energy divisor.</summary>
		/// <remarks>Default is 2</remarks>
		[KSPField]
		public float maxDustEnergyDiv = 2f;
		
		/// <summary>Maximum emission multiplier.</summary>
		/// <remarks>Default is 2</remarks>
		[KSPField]
		public float maxDustEmissionMult = 2f;
		
		/// <summary>Minimum emission value of the dust particles.</summary>
		/// <remarks>Default is 0.1.  This is the number of particles to emit per second.</remarks>
		[KSPField]
		public float minDustEmission = 0.1f;
		
		/// <summary>Maximum emission value of the dust particles.</summary>
		/// <remarks>Default is 20</remarks>
		[KSPField]
		public float maxDustEmission = 20f;
		
		/// <summary>Used in the OnCollisionEnter/Stay methods to define the minimum velocity magnitude to check against.</summary>
		/// <remarks>Default is 0.5</remarks>
		[KSPField]
		public float minVelocityMag = 0.5f;
		
		/// <summary>KSP path to the effect being used here.  Made into a field so that it can be customized in the future.</summary>
		/// <remarks>Default is "Effects/fx_smokeTrail_light"</remarks>
		[KSPField]
		public const string dustEffectsObject = "Effects/fx_smokeTrail_light";
		
		/// <summary>Part Info that will be displayed when part details are shown.</summary>
		/// <remarks>Can be overridden in the module config on a per-part basis.</remarks>
		[KSPField]
		public string partInfoString = "This part will throw up dust when the repulsion field is actively repulsing.";
		
		readonly KFLogUtil KFLog = new KFLogUtil();
		
		/// <summary>Prefix the logs with this to identify it.</summary>
		public string strClassName = "KFRepulsorDustFX";
		
		bool isPaused;
		bool isColorOverrideActive;
		bool isDustCameraEnabled;
		GameObject _kfRepDustFx;
		GameObject _kfRepLight;
		public Light _repLight;
		ParticleAnimator dustAnimator;
		Color colorDust;
		Color colorBiome;
		Color colorAverage;
		Color colorCam;
		
		/// <summary>Loaded from the KFConfigManager class.</summary>
		/// <remarks>Persistent field.</remarks>
		[Persistent]
		public bool isDustEnabledGlobally = true;
		
		/// <summary>Local dust disabler.</summary>
		/// <remarks>Is Persistent and active in all appropriate scenes by default.</remarks>
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Dust Effects"), UI_Toggle(disabledText = "Disabled", enabledText = "Enabled")]
		public bool isDustEnabledLocally = true;

		/// <summary>CollisionInfo class for the KFRepulsorDustFX module.</summary>
		public class CollisionInfo
		{
			public KFRepulsorDustFX KFRepDustFX;
			public CollisionInfo(KFRepulsorDustFX kfrepdustFX)
			{
				KFRepDustFX = kfrepdustFX;
			}
		}
		
		// Has it's own XML documentation, no need to add to it here.
		public override string GetInfo()
		{
			return partInfoString;
		}
		
		public override void OnStart(StartState state)
		{
			_KFRepulsor = part.GetComponentInChildren<KFRepulsor>();
			// This allows me to get the parameter value from the current active part.
			rideHeight = _KFRepulsor.rideHeight;
			// Public variable is set to the value of the remote variable here.

			isDustEnabledGlobally = KFPersistenceManager.isDustEnabled;
			isDustCameraEnabled = KFPersistenceManager.isDustCameraEnabled;
			
			if (!isDustEnabledGlobally && isDustEnabledLocally)
			{
				isDustEnabledLocally = isDustEnabledGlobally;
				Fields["localDisabledDust"].guiActive = false;
				Fields["localdisabledDust"].guiActiveEditor = false;
				return;
			}
			
			if (isDustEnabledGlobally && !isDustEnabledLocally)
				return;
			
			if (!HighLogic.LoadedSceneIsFlight)
				return;
			if (HighLogic.LoadedSceneIsFlight)
			{
				_ModuleCameraShot = vessel.GetComponent<ModuleCameraShot>();
				if (dustEffects)
					SetupParticles();
			}
			
			// Adding events to start and stop the emission on pause states.
			GameEvents.onGamePause.Add(OnPause);
			GameEvents.onGameUnpause.Add(OnUnpause);
		}
		
		/// <summary>Defines the particle effects used in this module.</summary>
		void SetupParticles()
		{
			if (!dustEffects)
				return;
			_kfRepDustFx = (GameObject)GameObject.Instantiate(Resources.Load(dustEffectsObject));
			_kfRepDustFx.transform.parent = part.transform;
			_kfRepDustFx.transform.position = part.transform.position;
			_kfRepDustFx.particleEmitter.localVelocity = Vector3.zero;
			_kfRepDustFx.particleEmitter.useWorldSpace = false;
			_kfRepDustFx.particleEmitter.emit = false;
            
			_kfRepDustFx.particleEmitter.minEnergy = minDustEnergy;
			_kfRepDustFx.particleEmitter.minEmission = minDustEmission;
			_kfRepDustFx.particleEmitter.minSize = minDustSize;
			dustAnimator = _kfRepDustFx.particleEmitter.GetComponent<ParticleAnimator>();
			KFLog.Log("Particles have been set up.", strClassName);
			_kfRepLight = new GameObject("Rep Light");
			_kfRepLight.transform.parent = _kfRepDustFx.transform;
			_kfRepLight.transform.position = Vector3.zero;
			_repLight = _kfRepLight.AddComponent<Light>();
			_repLight.type = LightType.Point;
			_repLight.range = 2.0f;
			_repLight.color = Color.blue;
			_repLight.intensity = 0.0f;


		}
		
		/// <summary>Contains information about what to do when the part enters a collided state.</summary>
		/// <param name="col">The collider being referenced.</param>
		public void OldCollisionEnter(Collision col)
		{
			CollisionInfo cInfo;
			if (col.relativeVelocity.magnitude >= minVelocityMag)
			{
				if (Equals(col.contacts.Length, 0))
					return;
				cInfo = GetClosestChild(part, col.contacts[0].point + (part.rigidbody.velocity * Time.deltaTime));
			}
		}
		
		/// <summary>Contains information about what to do when the part stays in the collided state over a period of time.</summary>
		/// <param name="hitPoint">Point at which the collision takes place.</param>
		/// <param name="col">The collider being referenced.</param>
		/// <param name="force">Force of the hit.</param>
		/// <param name="normal">No Clue.</param>
		/// <param name="direction">No Clue.</param>
		public void RepulsorEmit(Vector3 hitPoint, Collider col, float force, Vector3 normal, Vector3 direction)
		{
			CollisionInfo cInfo;
			isColorOverrideActive |= string.Equals("ModuleWaterSlider.Collider", col.gameObject.name);
			if (isPaused || Equals(rideHeight, 0))
				return;
			cInfo = KFRepulsorDustFX.GetClosestChild(part, hitPoint + part.rigidbody.velocity * Time.deltaTime);
			if (!Equals(cInfo.KFRepDustFX, null))
				cInfo.KFRepDustFX.Scrape(hitPoint, col, force, normal, direction);
			Scrape(hitPoint, col, force, normal, direction);
		}
		
		/// <summary>Searches child parts for the nearest instance of this class to the given point.</summary>
		/// <remarks>
		/// Parts with "physicsSignificance = 1" have their collisions detected by the parent part.
		/// To identify which part is the source of a collision, check which part the collision is closest to.
		/// </remarks>
		/// <param name="parent">The parent part whose children should be tested.</param>
		/// <param name="point">The point to test the distance from.</param>
		/// <returns>The nearest child part with a DustFX module, or null if the parent part is nearest.</returns>
		static CollisionInfo GetClosestChild(Part parent, Vector3 point)
		{
			float parentDistance = Vector3.Distance(parent.transform.position, point);
			float minDistance = parentDistance;
			KFRepulsorDustFX closestChild = null;
			foreach (Part child in parent.children)
			{
				if (!Equals(child, null) && !Equals(child.collider, null) && (Equals(child.physicalSignificance, Part.PhysicalSignificance.NONE)))
				{
					float childDistance = Vector3.Distance(child.transform.position, point);
					var cfx = child.GetComponent<KFRepulsorDustFX>();
					if (!Equals(cfx, null) && childDistance < minDistance)
					{
						minDistance = childDistance;
						closestChild = cfx;
					}
				}
			}
			return new CollisionInfo(closestChild);
		}
		
		/// <summary>Called when the part is scraping over a surface.</summary>
		/// <param name="position">Position of the scrape.</param>
		/// <param name="col">The collider being referenced.</param>
		/// <param name="force">Scrape force.</param>
		/// <param name="normal">No Clue.</param>
		/// <param name="direction">No Clue.</param>
		public void Scrape(Vector3 position, Collider col, float force, Vector3 normal, Vector3 direction)
		{
			if ((isPaused || Equals(part, null)) || Equals(part.rigidbody, null))
				return;
			//float fMagnitude = this.part.rigidbody.velocity.magnitude;
			DustParticles(force, position + (part.rigidbody.velocity * Time.deltaTime), col, normal, direction);
		}
		
		/// <summary>This creates and maintains the dust particles and their body/biome specific colors.</summary>
		/// <param name="force">Force of the part which is scraping.</param>
		/// <param name="contactPoint">The point at which the collider and the scraped surface make contact.</param>
		/// <param name="col">The collider being referenced.</param>
		/// <param name="normal">No Clue.</param>
		/// <param name="direction">No Clue.</param>
		void DustParticles(float force, Vector3 contactPoint, Collider col, Vector3 normal, Vector3 direction)
		{
			var WaterColor = new Color(0.65f, 0.65f, 0.65f, 0.025f);
			if (!dustEffects || force < minScrapeSpeed || Equals(dustAnimator, null) || Equals(rideHeight, 0))
				return;
			float appliedRideHeight = Mathf.Clamp((rideHeight / 2), 1, 4);
			colorBiome = !isColorOverrideActive ? KFDustFXUtils.GetDustColor(vessel.mainBody, col, vessel.latitude, vessel.longitude) : WaterColor;
			colorCam = _ModuleCameraShot._averageColour;
			colorAverage = !isDustCameraEnabled ? colorBiome : (colorCam + colorBiome) / 2;
			
			if (Equals(colorBiome, null))
			{
				KFLog.Error("Color \"BiomeColor\" is null!", strClassName);
				return;
			}
			if (force >= minScrapeSpeed)
			{
				if (!Equals(colorAverage, colorDust))
				{
					Color[] colors = dustAnimator.colorAnimation;
					colors[0] = colorAverage;
					colors[1] = colorAverage;
					colors[2] = colorAverage;
					colors[3] = colorAverage;
					colors[4] = colorAverage;
					dustAnimator.colorAnimation = colors;
					colorDust = colorAverage;
				}
				_kfRepDustFx.transform.position = contactPoint;
				_kfRepDustFx.transform.rotation = Quaternion.Euler(normal);
				_kfRepDustFx.particleEmitter.localVelocity = direction;
				_kfRepDustFx.particleEmitter.worldVelocity = Vector3.zero;
				_kfRepDustFx.particleEmitter.maxEnergy = Mathf.Clamp((force / maxDustEnergyDiv), minDustEnergy, maxDustEnergy);
				_kfRepDustFx.particleEmitter.maxEmission = Mathf.Clamp((force * maxDustEmissionMult), (minDustEmission * appliedRideHeight), (maxDustEmission * appliedRideHeight));
				_kfRepDustFx.particleEmitter.maxSize = Mathf.Clamp((force / appliedRideHeight), minDustSize, maxDustSize);
				_kfRepDustFx.particleEmitter.Emit();
				_kfRepLight.transform.localPosition = Vector3.zero;
				_repLight.intensity = 0.5f;
				_repLight.enabled = true;
			}
			return;
		}
		
		/// <summary>Called when the game enters a "paused" state.</summary>
		void OnPause()
		{
			isPaused = true;
			_kfRepDustFx.particleEmitter.enabled = false;
		}
		
		/// <summary>Called when the game leaves a "paused" state.</summary>
		void OnUnpause()
		{
			isPaused = false;
			_kfRepDustFx.particleEmitter.enabled = true;
		}
		
		/// <summary>Called when the object being referenced is destroyed, or when the module instance is deactivated.</summary>
		void OnDestroy()
		{
			GameEvents.onGamePause.Remove(OnPause);
			GameEvents.onGameUnpause.Remove(OnUnpause);
		}
	}
}
