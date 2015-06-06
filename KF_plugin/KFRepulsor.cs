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

        public JointSpring userspring;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Repulsor Settings")]
        public string settings = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string status = "Nominal";
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Group Number"), UI_FloatRange(minValue = 0, maxValue = 10f, stepIncrement = 1f)]
        public float groupNumber = 1;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Height"), UI_FloatRange(minValue = 0, maxValue = 100f, stepIncrement = 5f)]
        public float Rideheight = 25;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Strength"), UI_FloatRange(minValue = 0, maxValue = 6.00f, stepIncrement = 0.2f)]
        public float SpringRate;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Damping"), UI_FloatRange(minValue = 0, maxValue = 0.3f, stepIncrement = 0.05f)]
        public float DamperRate;
        [KSPField]
        public bool deployed;
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
        float smoothedRideHeight;
        float currentRideHeight;
        const float maxRepulsorHeight = 8;

        public float repulsorCount = 0;
        [KSPField]
        public float chargeConsumptionRate = 1f;
        //begin start
        public List<WheelCollider> wcList = new List<WheelCollider>();
        //public List<float> susDistList = new List<float>();
        ModuleWaterSlider _MWS;

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
            // degub only: print("onstart");
            base.OnStart(state);
            print(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            //this.part.AddModule("ModuleWaterSlider");
            if (HighLogic.LoadedSceneIsGame || HighLogic.LoadedSceneIsEditor)
            {
            	// do absolutely nothing.
            }
            if (HighLogic.LoadedSceneIsFlight)
            {
                _grid = transform.Search(gridName);
                _gridScale = _grid.transform.localScale;
                _gimbal = transform.Search(gimbalName);

                foreach (WheelCollider b in this.part.GetComponentsInChildren<WheelCollider>())
                {
                    repulsorCount += 1;
                    userspring = b.suspensionSpring;
                    userspring.spring = SpringRate;
                    userspring.damper = DamperRate;
                    b.suspensionSpring = userspring;
                    b.suspensionDistance = Rideheight;
                    wcList.Add(b);
                }
                
				this.part.force_activate(); // Force the part active or OnFixedUpate is not called.
                currentRideHeight = Rideheight;
                UpdateHeight();

                foreach (ModuleWaterSlider mws in this.vessel.FindPartModulesImplementing<ModuleWaterSlider>())
                    _MWS = mws;
				
                //print("water slider height is" + _MWS.colliderHeight);
                if (pointDown && Equals(this.vessel, FlightGlobals.ActiveVessel))
                {
                    StopAllCoroutines();
                    StartCoroutine("LookAt");
                }
            }
            DestroyBounds();
            effectPowerMax = 1 * repulsorCount * chargeConsumptionRate * Time.deltaTime;
            //print("max effect power");
            //print(effectPowerMax);
		}
		// End start

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
            Debug.LogWarning("Finished shrinking"); // Unreachable code warning here.
        }

        public void DestroyBounds()
        {
            Transform bounds = transform.Search("Bounds");
			if (!Equals(bounds, null))
            {
				UnityEngine.Object.Destroy(bounds.gameObject);
				//boundsDestroyed = true; // Remove the bounds object to let the wheel colliders take over
                print("destroying Bounds");
            }
        }

        public void RepulsorSound()
        {
            part.Effect("RepulsorEffect", effectPower);
        }

        public override void OnFixedUpdate()
        {
            smoothedRideHeight = Mathf.Lerp(smoothedRideHeight, currentRideHeight, Time.deltaTime * 2);
            appliedRideHeight = smoothedRideHeight / 100;

            //Vector3d oceanNormal = this.part.vessel.mainBody.GetSurfaceNVector(vessel.latitude, vessel.longitude);

            for (int i = 0; i < wcList.Count(); i++)
                wcList[i].suspensionDistance = maxRepulsorHeight * appliedRideHeight;

            if (deployed)
            {
				_MWS.colliderHeight = -2.5f; // Reset the height of the water collider that slips away every frame.
				float chargeConsumption = appliedRideHeight * (1 + SpringRate) * repulsorCount * Time.deltaTime * chargeConsumptionRate / 4;
                effectPower = chargeConsumption / effectPowerMax;

                float electricCharge = part.RequestResource("ElectricCharge", chargeConsumption);
                //print(electricCharge);
                // = Extensions.GetBattery(this.part);

                if (electricCharge < (chargeConsumption * 0.5f))
                {
                    print("Retracting due to low Electric Charge");
                    lowEnergy = true;
                    Rideheight = 0;
                    UpdateHeight();
                    status = "Low Charge";
                }
                else
                {
                    lowEnergy = false;
                    status = "Nominal";
                }

                if (Equals(appliedRideHeight, 0) || lowEnergy) //disable the colliders if there is not enough energy or height slips below the threshold
                {
                    deployed = false;
                    DisableColliders();
                    //print(appliedRideHeight);
                }
            }
            else
                effectPower = 0;
			
            RepulsorSound();
            effectPower = 0;    //reset to make sure it doesn't play when it shouldn't.
            //print(effectPower);
        }

        public void EnableColliders()
        {
            //print(wcList.Count());
            for (int i = 0; i < wcList.Count(); i++)
            {
                wcList[i].enabled = true;
                deployed = true;
            }
            if (retractEffect)
            {
                //StopCoroutine("Shrink");
                StartCoroutine("Grow");
            }
        }

        public void DisableColliders()
        {
            for (int i = 0; i < wcList.Count(); i++)
            {
                wcList[i].enabled = false;
                deployed = false;
            }
            if (retractEffect)
            {
                //StopCoroutine("Grow");
                StartCoroutine("Shrink");
            }
        }

        public void UpdateHeight()
        {
            currentRideHeight = Rideheight;
            EnableColliders();
        }

        [KSPAction("Retract")]
        public void Retract(KSPActionParam param)
        {
            if (Rideheight > 0)
            {
                Rideheight -= 5f;
                print("Retracting");
                UpdateHeight();
            }
		}

        [KSPAction("Extend")]
        public void Extend(KSPActionParam param)
        {
            if (Rideheight < 100)
            {
                Rideheight += 5f;
                print("Extending");
                UpdateHeight();
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
            foreach (KFRepulsor mt in this.vessel.FindPartModulesImplementing<KFRepulsor>())
            {
                if (!Equals(groupNumber, 0) && Equals(groupNumber, mt.groupNumber))
                {
                    mt.Rideheight = Rideheight;
                    currentRideHeight = Rideheight;
                    mt.currentRideHeight = Rideheight;
                    mt.UpdateHeight();
                }
            }
            UpdateHeight();
        }
	}
	// End class
}
// End namespace
