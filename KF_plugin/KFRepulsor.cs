/*
 * KSP [0.23.5] Anti-Grav Repulsor plugin by Lo-Fi
 * Much inspiration and a couple of code snippets for deployment taken from BahamutoD's Critter Crawler mod. Huge respect, it's a fantastic mod :)
 * 
 */

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFRepulsor : PartModule
    {
        // disable RedundantDefaultFieldInitializer
		// disable RedundantThisQualifier

        public JointSpring userspring;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Repulsor Settings")]
        public string settings = string.Empty;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string status = "Nominal";
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Group Number"), UI_FloatRange(minValue = 0, maxValue = 10f, stepIncrement = 1f)]
        public float groupNumber = 1;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Height"), UI_FloatRange(minValue = 0, maxValue = 100f, stepIncrement = 5f)]
        public float rideHeight = 25;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Strength"), UI_FloatRange(minValue = 0, maxValue = 6.00f, stepIncrement = 0.2f)]
        public float SpringRate;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Damping"), UI_FloatRange(minValue = 0, maxValue = 0.3f, stepIncrement = 0.05f)]
        public float DamperRate;
        [KSPField]
        public bool deployed = true;
        [KSPField]
        public bool lowEnergy;
        [KSPField]
        public bool retractEffect;
        [KSPField]
        public bool pointDown;
        [KSPField]
        public string gridName;
        [KSPField]
        public string gimbalName;
        bool isReady;
        Transform _grid;
        Transform _gimbal;

        Vector3 _gridScale;
        
        float effectPower; 
        float effectPowerMax;
        float appliedRideHeight;
        //float smoothedRideHeight;
        float currentRideHeight;
        const float maxRepulsorHeight = 100;

        public float repulsorCount = 0;

        KFRepulsorDustFX _dustFX;
        float dir;


        //begin start
        public List<WheelCollider> wcList = new List<WheelCollider>();
        //public List<float> susDistList = new List<float>();
        ModuleWaterSlider _MWS;

		/// <summary>Defines the rate at which the specified resource is consumed.</summary>
        [KSPField]
        public float resourceConsumptionRate = 1f;

		/// <summary>The name of the resource to consume.</summary>
        [KSPField]
		public string resourceName = "ElectricCharge";
        
        /// <summary>This is the info string that will display when the part info is shown.</summary>
		/// <remarks>This can be overridden in the part config for this module.</remarks>
		[KSPField]
		public string strInfo = "This part allows the craft to hover above the ground.  Steering mechanism not included.";
        public override string GetInfo()
		{
			return strInfo;
		}
        
        public override void OnStart(PartModule.StartState state)  //when started
        {
			// debug only: print("onstart");
			if (!isReady)
				isReady = true; // Now it won't complain about not being used or initialized anywhere.
            base.OnStart(state);
            print(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            effectPowerMax = repulsorCount * resourceConsumptionRate * Time.fixedDeltaTime; // Previously it had "1 * blahblahblah" in it, which is kinda stupid since 1x of any value is equal to that value.  So I nuked the "1 *" part. - Gaalidas
			print(string.Format("Max effect power is {0}.", effectPowerMax));

            _dustFX = this.part.GetComponent<KFRepulsorDustFX>(); //see if it's been added by MM
            if (Equals(_dustFX, null)) //add if not... sets some defaults.
            {
                this.part.AddModule("KFRepulsorDustFX");
                _dustFX = this.part.GetComponent<KFRepulsorDustFX>();
                //_dustFX.wheelImpact = true;
                //_dustFX.wheelImpactSound = "KerbalFoundries/Sounds/TyreSqueal";
                _dustFX.maxDustEmission = 28;
                _dustFX.OnStart(state);
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                _grid = transform.Search(gridName);
                _gridScale = _grid.transform.localScale;
                _gimbal = transform.Search(gimbalName);
                SetupWaterSlider();

                foreach (WheelCollider b in this.part.GetComponentsInChildren<WheelCollider>())
                {
                    repulsorCount += 1;
                    userspring = b.suspensionSpring;
                    userspring.spring = SpringRate;
                    userspring.damper = DamperRate;
                    b.suspensionSpring = userspring;
                    b.suspensionDistance = 1f; //default to low setting to save stupid shenanigans on takeoff
                    wcList.Add(b);
                }
				this.part.force_activate(); // Force the part active or OnFixedUpate is not called.
				
                //print("water slider height is" + _MWS.colliderHeight);
                if (pointDown && Equals(this.vessel, FlightGlobals.ActiveVessel))
                {
                    StopAllCoroutines();
                    StartCoroutine("LookAt");
                }
                appliedRideHeight = rideHeight;
                StartCoroutine("UpdateHeight"); //start updating to height set before launch
            }
            DestroyBounds();
		}
		// End start

		void SetupWaterSlider()
		{
			_MWS = this.vessel.GetComponent<ModuleWaterSlider>();
			if (!Equals(_MWS, null))
				Debug.LogError("Found MWS.");
			else
				Debug.LogError("Did not find MWS.");
		}

        /// <summary>A "Shrink" coroutine for steering.</summary>
        IEnumerator Shrink()
        {
            while (_grid.transform.localScale.x > 0.2f && _grid.transform.localScale.y > 0.2f && _grid.transform.localScale.z > 0.2f)
            {
                _grid.transform.localScale -= (_gridScale / 50);
                yield return null;
            }
            _grid.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            Debug.LogWarning("Finished shrinking");
        }

        /// <summary>A "grow" coroutine for steering.</summary>
        IEnumerator Grow()
        {
            while (_grid.transform.localScale.x < _gridScale.x && _grid.transform.localScale.y < _gridScale.y && _grid.transform.localScale.z < _gridScale.z)
            {
                _grid.transform.localScale += (_gridScale / 50);
                yield return null;
            }
            print(_gridScale);
            _grid.transform.localScale = _gridScale;
            Debug.LogWarning("Finished growing");
        }

        // disable once FunctionNeverReturns
        /// <summary>A "LookAt" coroutine for steering.</summary>
        IEnumerator LookAt()
        {
            while (true)
            {
                _gimbal.transform.LookAt(FlightGlobals.ActiveVessel.mainBody.transform.position);
                yield return null;
            }
        }

        public void DestroyBounds()
        {
            Transform bounds = transform.Search("Bounds");
			if (!Equals(bounds, null))
            {
				UnityEngine.Object.Destroy(bounds.gameObject);
				print("Destroying Bounds.");
            }
        }

        public void RepulsorSound()
        {
            part.Effect("RepulsorEffect", effectPower/ effectPowerMax);
        }

        public void UpdateWaterSlider()
        {
            _MWS.colliderHeight = -2.5f;
        }

        public float ResourceConsumption()
        {
            float resourceConsumption = (appliedRideHeight / 100) * (1 + SpringRate) * repulsorCount * Time.deltaTime * resourceConsumptionRate / 4;
            float requestResource = resourceConsumption / effectPowerMax;
            float electricCharge = part.RequestResource(resourceName, requestResource);
            return electricCharge;
        }

        public override void OnFixedUpdate()
        {
            //smoothedRideHeight = Mathf.Lerp(smoothedRideHeight, currentRideHeight, Time.deltaTime * 2);
            //appliedRideHeight = smoothedRideHeight / 100;
                        
            //for (int i = 0; i < wcList.Count(); i++)
               // wcList[i].suspensionDistance = maxRepulsorHeight * rideHeight;
            if (dir > 360)
                dir = 0;
            float sin = (float)Math.Sin(Mathf.Deg2Rad * dir);
            float cos = (float)Math.Cos(Mathf.Deg2Rad * dir);
            Vector3 emitDirection = new Vector3(0, sin * 10, cos * 10);
           
            float hitForce = 0;

            if (deployed)
            {
				 // Reset the height of the water collider that slips away every frame.
                UpdateWaterSlider();
                float requestRecource = ResourceConsumption();


                for (int i = 0; i < wcList.Count(); i++)
                {
                    WheelHit hit;
                    bool grounded = wcList[i].GetGroundHit(out hit);
                    if (grounded)
                    {
                        hitForce += hit.force;
                        _dustFX.RepulsorEmit(hit.point, hit.collider, hit.force, hit.normal, emitDirection);
                    }
                }

                
                //print(electricCharge);
                // = Extensions.GetBattery(this.part);
                /*
                if (electricCharge < (chargeConsumption * 0.5f))
                {
                    print("Retracting due to low Electric Charge");
                    lowEnergy = true;
                    rideHeight = 0;
                    UpdateHeight();
                    status = "Low Charge";
                }
                else
                {
                    lowEnergy = false;
                    status = "Nominal";
                }

                if (Equals(rideHeight, 0) || lowEnergy) //disable the colliders if there is not enough energy or height slips below the threshold
                {
                    deployed = false;
                    DisableColliders();
                    //print(appliedRideHeight);
                }
                 * */
            }
            else
                effectPower = 0;
			
            //RepulsorSound();
            effectPower = 0;    //reset to make sure it doesn't play when it shouldn't.
            //print(effectPower);

            dir += UnityEngine.Random.Range(20,60);
        }


        IEnumerator UpdateHeight()
        {
            if (appliedRideHeight > 0)
            {
                for (int i = 0; i < wcList.Count(); i++)
                {
                    wcList[i].enabled = true;
                }
                StartCoroutine("Grow");
            }
            //Debug.LogWarning("UpdateHeight Couroutine Start.");
			while (!Equals(Mathf.Round(currentRideHeight), appliedRideHeight))
			{
				currentRideHeight = Mathf.Lerp(currentRideHeight, appliedRideHeight, Time.deltaTime * 2);
				//Debug.LogWarning(currentRideHeight);
				for (int i = 0; i < wcList.Count(); i++)
					wcList[i].suspensionDistance = appliedRideHeight / 20f;
				yield return new WaitForFixedUpdate();
			}
            if (currentRideHeight < 1)
            {
                //Debug.LogWarning("Disabling Colliders.");
				for (int i = 0; i < wcList.Count(); i++)
					wcList[i].enabled = false;
                StartCoroutine("Shrink");
            }
            //Debug.LogWarning("Finished height update.");
        }

        [KSPAction("Retract")]
        public void Retract(KSPActionParam param)
        {
            if (rideHeight > 0)
            {
                rideHeight -= 5f;
                //print("Retracting...");
                StartCoroutine("UpdateHeight");
            }
		}

        [KSPAction("Extend")]
        public void Extend(KSPActionParam param)
        {
            if (rideHeight < 100)
            {
                rideHeight += 5f;
                //print("Extending...");
                StartCoroutine("UpdateHeight");
            }
		}

		[KSPAction("Apply Settings")]
		public void ApplySettingsAction(KSPActionParam param)
		{
			ApplySettings();
		}
		
        [KSPEvent(guiActive = true, guiName = "Apply Settings", active = true)]
        public void ApplySettings()
        {
            //appliedRideHeight = rideHeight;
            foreach (KFRepulsor mt in this.vessel.FindPartModulesImplementing<KFRepulsor>())
            {
                if (!Equals(groupNumber, 0) && Equals(groupNumber, mt.groupNumber))
                {
                    mt.rideHeight = rideHeight;
                    mt.appliedRideHeight = rideHeight;
                    mt.StartCoroutine("UpdateHeight"); 
                }
            }
            //StartCoroutine("UpdateHeight"); 
        }
	}
	// End class
}
// End namespace
