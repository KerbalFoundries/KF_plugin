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
 */

using System;
using UnityEngine;
using KerbalFoundries;
using System.Collections.Generic;

namespace KerbalFoundries
{
	/// <summary>DustFX class which is based on, but heavily modified from, CollisionFX by pizzaoverload.</summary>
	public class KFDustFX : PartModule
	{
		/// <summary>Local definition of the KFModuleWheel class.</summary>
		KFModuleWheel _KFModuleWheel;
		
		/// <summary>Local copy of the tweakScaleCorrector parameter in the KFModuleWheel module.</summary>
		private float TSWheelCorrector;

        private int colCount;
		
		// Class-wide disabled warnings in SharpDevelop
		// disable AccessToStaticMemberViaDerivedType
		// disable RedundantDefaultFieldInitializer
		
		/// <summary>Mostly unnecessary, since there is no other purpose to having the module active.</summary>
		/// <remarks>Default is "true"</remarks>
		[KSPField]
		public bool dustEffects = true;
		
		/// <summary>Used to enable the impact audio effects.</summary>
		/// <remarks>Default is "false"</remarks>
		[KSPField]
		public bool wheelImpact;
		
		/// <summary>Minimum scrape speed.</summary>
		/// <remarks>Default is 1.  Repulsors should have this extremely low.</remarks>
		[KSPField]
		public float minScrapeSpeed = 1f;
		
		/// <summary>Minimum dust energy value.</summary>
		/// <remarks>Default is 0</remarks>
		[KSPField]
		public float minDustEnergy = 0f;
		
		/// <summary>Minimum emission value of the dust particles.</summary>
		/// <remarks>Default is 0</remarks>
		[KSPField]
		public float minDustEmission = 0f;
		
		/// <summary>Maximum emission energy divisor.</summary>
		/// <remarks>Default is 10</remarks>
		[KSPField]
		public float maxDustEnergyDiv = 10f;
		
		/// <summary>Maximum emission multiplier.</summary>
		/// <remarks>Default is 2</remarks>
		[KSPField]
		public float maxDustEmissionMult = 2f;
		
		/// <summary>Maximum emission value of the dust particles.</summary>
		/// <remarks>Default is 35</remarks>
		[KSPField]
		public float maxDustEmission = 35f;
		
		/// <summary>Path to the sound file that is to be used for impacts.</summary>
		/// <remarks>Default is Empty.</remarks>
		[KSPField]
		public string wheelImpactSound = string.Empty;
		
		/// <summary>Used in the OnCollisionEnter/Stay methods to define the minimum velocity magnitude to check against.</summary>
		/// <remarks>Default is 3.  Would set very low for repulsors.</remarks>
		[KSPField]
		public float minVelocityMag = 3f;
		
		/// <summary>Pitch-range that is used to vary the sound pitch.</summary>
		/// <remarks>Default is 0.3</remarks>
		[KSPField]
		public float pitchRange = 0.3f;
		
		/// <summary>Audio level for the doppler effect used in various audio calculations.</summary>
		/// <remarks>Default is 0</remarks>
		[KSPField]
		public float dopplerEffectLevel = 0f;
		
		/// <summary>KSP path to the effect being used here.  Made into a field so that it can be customized in the future.</summary>
		/// <remarks>Default is "Effects/fx_smokeTrail_light"</remarks>
		[KSPField]
		public const string dustEffectObject = "Effects/fx_smokeTrail_light";
		
		/// <summary>Part Info that will be displayed when part details are shown.</summary>
		/// <remarks>Can be overridden in the module config on a per-part basis.</remarks>
		[KSPField]
		public string partInfoString = "This part will throw up dust when rolling over the terrain.";
		
		/// <summary>FXGroup for the wheel impact sound effect.</summary>
		FXGroup WheelImpactSound;
		
		/// <summary>Prefix the logs with this to identify it.</summary>
		public string logprefix = "[DustFX - Main]: ";
		
		bool paused;
		GameObject kfdustFx;
		ParticleAnimator dustAnimator;
		Color dustColor;
		Color BiomeColor;

		/// <summary>CollisionInfo class for the DustFX module.</summary>
		public class CollisionInfo
		{
			public KFDustFX KFDustFX;
			public CollisionInfo (KFDustFX kfdustFX)
			{
				KFDustFX = kfdustFX;
			}
		}
		
		public override string GetInfo ()
		{
			return partInfoString;
		}
		
		public override void OnStart ( StartState state )
		{
			const string locallog = "OnStart(): ";
			_KFModuleWheel = part.GetComponentInChildren<KFModuleWheel>();
            colCount = _KFModuleWheel.wcList.Count;
			TSWheelCorrector = _KFModuleWheel.tweakScaleCorrector;
			if (!Equals(TSWheelCorrector, 0))
				Debug.Log(string.Format("{0}{1}TSWheelCorrector = {2}", logprefix, locallog, TSWheelCorrector));
			
			if (Equals(state, StartState.Editor) || Equals(state, StartState.None))
				return;
			if (dustEffects)
				SetupParticles();
			if (wheelImpact && !string.IsNullOrEmpty(wheelImpactSound))
				DustAudio();
			GameEvents.onGamePause.Add(OnPause);
			GameEvents.onGameUnpause.Add(OnUnpause);
		}
		
		/// <summary>Defines the particle effects used in this module.</summary>
		void SetupParticles ()
		{
			const string locallog = "SetupParticles(): ";
			if (!dustEffects)
				return;
			kfdustFx = (GameObject)GameObject.Instantiate(Resources.Load(dustEffectObject));
			kfdustFx.transform.parent = part.transform;
			kfdustFx.transform.position = part.transform.position;
			kfdustFx.particleEmitter.localVelocity = Vector3.zero;
			kfdustFx.particleEmitter.useWorldSpace = true;
			kfdustFx.particleEmitter.emit = false;
			kfdustFx.particleEmitter.minEnergy = minDustEnergy;
			kfdustFx.particleEmitter.minEmission = minDustEmission;
			dustAnimator = kfdustFx.particleEmitter.GetComponent<ParticleAnimator>();
			Debug.Log(string.Format("{0}{1}Particles have been set up.", logprefix, locallog));
		}
		
		/// <summary>Contains information about what to do when the part enters a collided state.</summary>
		/// <param name="col">The collider being referenced.</param>
        /// 
        
		public void OnCollisionEnter ( Collision col )
		{
			if (col.relativeVelocity.magnitude >= minVelocityMag)
			{
				if (Equals(col.contacts.Length, 0))
					return;
                int collisionCount = 0;
                Vector3 collisionAverage = new Vector3(0, 0, 0);

                for (int i = 0; i < colCount; i++)
                {
                    WheelHit hit;
                    bool grounded = _KFModuleWheel.wcList[i].GetGroundHit(out hit);
                    if (grounded)
                    {
                        collisionAverage += hit.point;
                        collisionCount++;
                    }
                }
                collisionAverage /= collisionCount;
                CollisionInfo cInfo = GetClosestChild(part, collisionAverage + (part.rigidbody.velocity * Time.deltaTime));
				if (!Equals(cInfo.KFDustFX, null))
					cInfo.KFDustFX.DustImpact();
			}
		}
       
		
		/// <summary>Contains information about what to do when the part stays in the collided state over a period of time.</summary>
		/// <param name="col">The collider being referenced.</param>
		public void OnCollisionStay ( Collision col )
		{
			if (paused || Equals(col.contacts.Length, 0))
				return;

            int collisionCount = 0;
            Vector3 collisionAverage = new Vector3(0,0,0); 

            for (int i = 0; i < colCount; i++)
            {
                WheelHit hit;
                bool grounded = _KFModuleWheel.wcList[i].GetGroundHit(out hit);
                if (grounded)
                {
                    collisionAverage += hit.point;
                    collisionCount++;
                }
            }
            collisionAverage /= collisionCount;


			//CollisionInfo cInfo = KFDustFX.GetClosestChild(part, col.contacts[0].point + part.rigidbody.velocity * Time.deltaTime);
            /*
            var collisions = new Vector3(0, 0, 0);
            var collisionCount = 0;

            foreach (ContactPoint cols in col.contacts)
            {
                collisions += cols.point;
                collisionCount++;
            }
            Vector3 averageCollision = collisions / collisionCount;
            CollisionInfo cInfo = GetClosestChild(part, averageCollision + (part.rigidbody.velocity * Time.deltaTime));
			if (!Equals(cInfo.KFDustFX, null))
				cInfo.KFDustFX.Scrape(col, averageCollision);
             * */


            Scrape(col, collisionAverage);
		}
		
		/// <summary>Searches child parts for the nearest instance of this class to the given point.</summary>
		/// <remarks>
		/// Parts with "physicsSignificance = 1" have their collisions detected by the parent part.
		/// To identify which part is the source of a collision, check which part the collision is closest to.
		/// </remarks>
		/// <param name="parent">The parent part whose children should be tested.</param>
		/// <param name="point">The point to test the distance from.</param>
		/// <returns>The nearest child part with a DustFX module, or null if the parent part is nearest.</returns>
		static CollisionInfo GetClosestChild ( Part parent, Vector3 point )
		{
			float parentDistance = Vector3.Distance(parent.transform.position, point);
			float minDistance = parentDistance;
			KFDustFX closestChild = null;
			foreach (Part child in parent.children)
			{
				if (!Equals(child, null) && !Equals(child.collider, null) && (Equals(child.physicalSignificance, Part.PhysicalSignificance.NONE)))
				{
					float childDistance = Vector3.Distance(child.transform.position, point);
					var cfx = child.GetComponent<KFDustFX>();
					if (!Equals(cfx, null) && childDistance < minDistance)
					{
						minDistance = childDistance;
						closestChild = cfx;
					}
				}
			}
			return new CollisionInfo (closestChild);
		}
		
		/// <summary>Called when the part is scraping over a surface.</summary>
		/// <param name="col">The collider being referenced.</param>
		public void Scrape ( Collision col, Vector3 position )
		{
			if ((paused || Equals(part, null)) || Equals(part.rigidbody, null) || Equals(col.contacts.Length, 0))
				return;
			float m = col.relativeVelocity.magnitude;
			DustParticles(m, position + (part.rigidbody.velocity * Time.deltaTime), col.collider);
		}
		
		/// <summary>This creates and maintains the dust particles and their body/biome specific colors.</summary>
		/// <param name="speed">Speed of the part which is scraping.</param>
		/// <param name="contactPoint">The point at which the collider and the scraped surface make contact.</param>
		/// <param name="col">The collider being referenced.</param>
		void DustParticles ( float speed, Vector3 contactPoint, Collider col )
		{
			const string locallog = "DustParticles(): ";
			if (!dustEffects || speed < minScrapeSpeed || Equals(dustAnimator, null))
				return;
			float correctionvalue = TSWheelCorrector;
			if (Equals(TSWheelCorrector, 0) || TSWheelCorrector < 0)
				correctionvalue = 1f;
			BiomeColor = KFDustFXController.DustColors.GetDustColor(vessel.mainBody, col, vessel.latitude, vessel.longitude);
			if (Equals(BiomeColor, null))
				Debug.Log(string.Format("{0}{1}Color \"BiomeColor\" is null!", logprefix, locallog)); 
			if (speed >= minScrapeSpeed)
			{
				if (!Equals(BiomeColor, dustColor))
				{
					Color [] colors = dustAnimator.colorAnimation; 
					colors[0] = BiomeColor;
					colors[1] = BiomeColor;
					colors[2] = BiomeColor;
					colors[3] = BiomeColor;
					colors[4] = BiomeColor;
					dustAnimator.colorAnimation = colors;
					dustColor = BiomeColor;
				}
				kfdustFx.transform.position = contactPoint;
				kfdustFx.particleEmitter.maxEnergy = speed / (maxDustEnergyDiv * correctionvalue);
				kfdustFx.particleEmitter.maxEmission = Mathf.Clamp((speed * (maxDustEmissionMult * correctionvalue)), minDustEmission, (maxDustEmission * correctionvalue));
				kfdustFx.particleEmitter.Emit();
			}
			return;
		}
		
		/// <summary>Called when the game enters a "paused" state.</summary>
		void OnPause ()
		{
			paused = true;
			kfdustFx.particleEmitter.enabled = false;
			if (!Equals(WheelImpactSound, null) && !Equals(WheelImpactSound.audio, null))
				WheelImpactSound.audio.Stop();
		}
		
		/// <summary>Called when the game leaves a "paused" state.</summary>
		void OnUnpause ()
		{
			paused = false;
			kfdustFx.particleEmitter.enabled = true;
		}
		
		/// <summary>Called when the object being referenced is destroyed, or when the module instance is deactivated.</summary>
		void OnDestroy ()
		{
			if (!Equals(WheelImpactSound, null) && !Equals(WheelImpactSound.audio, null))
				WheelImpactSound.audio.Stop();
			GameEvents.onGamePause.Remove(OnPause);
			GameEvents.onGameUnpause.Remove(OnUnpause);
		}

		/// <summary>Gets the current volume setting for Ship sounds.</summary>
		/// <returns>The volume value as a float.</returns>
		static float GetShipVolume ()
		{
			return GameSettings.SHIP_VOLUME;
		}

		/// <summary>Sets up and maintains the audio effect which is, currently, not widely used.</summary>
		void DustAudio ()
		{
			if (Equals(wheelImpactSound, string.Empty))
				return;
			WheelImpactSound = new FXGroup ("WheelImpactSound");
			part.fxGroups.Add(WheelImpactSound);
			WheelImpactSound.audio = gameObject.AddComponent<AudioSource>();
			WheelImpactSound.audio.clip = GameDatabase.Instance.GetAudioClip(wheelImpactSound);
			WheelImpactSound.audio.dopplerLevel = dopplerEffectLevel;
			WheelImpactSound.audio.rolloffMode = AudioRolloffMode.Logarithmic;
			WheelImpactSound.audio.Stop();
			WheelImpactSound.audio.loop = false;
			WheelImpactSound.audio.volume = GetShipVolume();
		}
		
		/// <summary>Called when the part impacts with a surface with enough magnitude to be audible.</summary>
		public void DustImpact ()
		{
			if (Equals(WheelImpactSound, null) || Equals(wheelImpactSound, string.Empty))
			{
				WheelImpactSound.audio.Stop();
				return;
			}
			WheelImpactSound.audio.pitch = UnityEngine.Random.Range(1 - pitchRange, 1 + pitchRange);
			WheelImpactSound.audio.Play();
		}
	}
}
