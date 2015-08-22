/*
 * KSP [0.23.5] Anti-Grav Repulsor plugin by Lo-Fi
 * Much inspiration and a couple of code snippets for deployment taken from BahamutoD's Critter Crawler mod. Huge respect, it's a fantastic mod :)
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFRepulsor : PartModule
    {
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
        public Transform _grid;
        Transform _gimbal;

        Vector3 _gridScale;

        //float effectPower;
        // disable ConvertToConstant.Local
        float effectPowerMax = 50f;
        float appliedRideHeight;
        float currentRideHeight;
        float repulsorCount = 0;
        float compression = 0;
        float squish;

        KFDustFX _dustFX;
        float dir;

        public List<WheelCollider> wcList = new List<WheelCollider>();
        public bool deployed = true;
        //public List<float> susDistList = new List<float>();
        ModuleWaterSlider _MWS;

		/// <summary>Defines the rate at which the specified resource is consumed.</summary>
        [KSPField]
        public float resourceConsumptionRate = 1f;

		/// <summary>The name of the resource to consume.</summary>
        [KSPField]
		public string resourceName = "ElectricCharge";
		
		public float susInc;

        /// <summary>This is the info string that will display when the part info is shown.</summary>
		/// <remarks>This can be overridden in the part config for this module due to its KSPField status.</remarks>
		[KSPField]
        public string strPartInfo = "This part allows the craft to hover above the ground.  Steering mechanism not included.\n\n<b><color=#99ff00ff>Requires:</color></b>\n- {ResourceName}: {ConsumptionRate}/sec @ max height";
        public override string GetInfo()
		{
            return UpdateInfoText(strPartInfo, resourceName, resourceConsumptionRate);
		}

        static string UpdateInfoText(string strPartInfo, string strResourceName, float consumptionRate)
        {
            return strPartInfo.Replace("{ResourceName}", strResourceName).Replace("{ConsumptionRate}", consumptionRate.ToString("0.00"));
        }

        /// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFRepulsor");

        //begin start
        public override void OnStart(PartModule.StartState state)  //when started
        {
			base.OnStart(state);
			
			susInc = KFPersistenceManager.suspensionIncrement;
			
			if (HighLogic.LoadedSceneIsFlight && !Equals(vessel.vesselType, VesselType.Debris)) // && vessel.parts.Count > 1) // Vessel could still be debris even with a part cound greater than one.
            {
                _grid = transform.Search(gridName);
                _gridScale = _grid.transform.localScale;
                _gimbal = transform.Search(gimbalName);

                foreach (WheelCollider b in part.GetComponentsInChildren<WheelCollider>())
                {
                    repulsorCount ++;
                    userspring = b.suspensionSpring;
                    userspring.spring = SpringRate;
                    userspring.damper = DamperRate;
                    b.suspensionSpring = userspring;
                    b.suspensionDistance = 2.5f; //default to low setting to save stupid shenanigans on takeoff
                    wcList.Add(b);
                }
                KFLog.Log(string.Format("Repulsor Count: {0}", repulsorCount));
				
                if (pointDown)
                {
                    StopAllCoroutines();
                    StartCoroutine("LookAt");
                }
                appliedRideHeight = rideHeight;
                StartCoroutine("UpdateHeight"); //start updating to height set before launch

				SetupDust(state);
				SetupWaterSlider();
				isReady = true;
            }
            DestroyBounds();
		}
		// End start
        void SetupDust(PartModule.StartState state)
        {
            _dustFX = part.GetComponent<KFDustFX>(); //see if it's been added by MM. MM deprecated in favor of adding the module manually. - Gaalidas
            if (!Equals(_dustFX, null)) //add if not... sets some defaults.
            {
                _dustFX = part.gameObject.AddComponent<KFDustFX>();
                _dustFX.isRepulsor = true;
                _dustFX.OnStart(state);
            }
        }

		void SetupWaterSlider()
		{
			_MWS = vessel.rootPart.GetComponent<ModuleWaterSlider>();
            if (Equals(_MWS, null)) //add if not... sets some defaults.
            {
                _MWS = vessel.rootPart.gameObject.AddComponent<ModuleWaterSlider>();
                 //vessel.rootPart.GetComponent<ModuleWaterSlider>();
                _MWS.StartUp();
            }
		}

        /// <summary>A "Shrink" coroutine for the animation.</summary>
        IEnumerator Shrink()
        {
            while (_grid.transform.localScale.x > 0.2f && _grid.transform.localScale.y > 0.2f && _grid.transform.localScale.z > 0.2f)
            {
                _grid.transform.localScale -= (_gridScale / 50);
                yield return null;
            }
            _grid.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            deployed = false;
            //KFLog.Warning("Finished shrinking");
        }

        /// <summary>A "grow" coroutine for the animation.</summary>
        IEnumerator Grow()
        {
            while (_grid.transform.localScale.x < _gridScale.x && _grid.transform.localScale.y < _gridScale.y && _grid.transform.localScale.z < _gridScale.z)
            {
                _grid.transform.localScale += (_gridScale / 50);
                yield return null;
            }
            //KFLog.Log(_gridScale);
            _grid.transform.localScale = _gridScale;
            //KFLog.Warning("Finished growing");
        }

        // disable once FunctionNeverReturns
        /// <summary>A "LookAt" coroutine for steering/orientation.</summary>
        IEnumerator LookAt()
        {
            while (true)
            {
                _gimbal.transform.LookAt(FlightGlobals.ActiveVessel.mainBody.transform.position);
                yield return null;
            }
        }

        // disable once MemberCanBeMadeStatic.Local
        public void DestroyBounds()
        {
            Transform bounds = transform.Search("Bounds");
			if (!Equals(bounds, null))
            {
				UnityEngine.Object.Destroy(bounds.gameObject);
				//KFLog.Log("Destroying Bounds.");
            }
        }

        public void RepulsorSound(float power)
        {
            part.Effect("RepulsorEffect", power / effectPowerMax);
        }

        public void UpdateWaterSlider()
        {
            _MWS.colliderHeight = -2f;
        }

        public void ResourceConsumption()
        {
            float resRequest = resourceConsumptionRate * Time.deltaTime * appliedRideHeight / 100f;
            float resDrain = part.RequestResource(resourceName, resRequest);
			lowEnergy = resDrain < resRequest ? true : false;
        }

        public void FixedUpdate()
        {
            if (!isReady)
                return;
            if (dir > 360)
                dir = 0;
            float sin = (float)Math.Sin(Mathf.Deg2Rad * dir);
            float cos = (float)Math.Cos(Mathf.Deg2Rad * dir);
            var emitDirection = new Vector3(0, sin * 10, cos * 10);

            float hitForce = 0;

            if (deployed)
            {
                // Reset the height of the water collider that slips away every frame.
                UpdateWaterSlider();
                ResourceConsumption();
                bool anyGrounded = false;
                float frameCompression = 0;
                for (int i = 0; i < wcList.Count(); i++)
                {
                    WheelHit hit;
                    bool grounded = wcList[i].GetGroundHit(out hit);
                    if (grounded)
                    {
                        anyGrounded |= grounded;
                        hitForce += hit.force;
                        if (KFPersistenceManager.isDustEnabled)
                        	_dustFX.RepulsorEmit(hit.point, hit.collider, hit.force, hit.normal, emitDirection);
                        frameCompression += -wcList[i].transform.InverseTransformPoint(hit.point).y - wcList[i].radius;
                    }
                    compression = frameCompression;
                }
                if (anyGrounded)
                {
                    compression /= (wcList.Count() + 1);

                    float normalisedComp = compression / 8;
                    squish = normalisedComp / (appliedRideHeight / 100);
                }
                else if (squish > 0.1)
					squish /= 2;
                //KFLog.Log("comp " + compression);
                //KFLog.Log("squish " + squish);

				if (lowEnergy)
				{
					KFLog.Log("Retracting due to low Electric Charge");
					appliedRideHeight = 0f;
					rideHeight = 0f;
					StartCoroutine("UpdateHeight");
					status = "Low Charge";
					deployed = false;
				}
				else
					status = "Nominal";
            }
            else
            {
                //effectPower = 0;
				status = lowEnergy ? "Low Charge" : "Off";
                //KFLog.Warning(string.Format("\"deployed\" = {0}", deployed));
            }
			
            RepulsorSound(hitForce);
			if (deployed && KFPersistenceManager.isRepLightEnabled && rideHeight > 0f)
				_dustFX.RepulsorLight(deployed, squish);
            //effectPower = 0;    //reset to make sure it doesn't play when it shouldn't.
            //KFLog.Log(effectPower);
			
            dir += UnityEngine.Random.Range(20,60);

            // Updates the suspention increment value for the action groups.
			susInc = KFPersistenceManager.suspensionIncrement;
        }

        IEnumerator UpdateHeight()
        {
            if (appliedRideHeight > 0)
            {
				for (int i = 0; i < wcList.Count(); i++)
					wcList[i].enabled = true;
                deployed = true; // catches redeploy after low charge retract
                StopCoroutine("Shrink");
                StartCoroutine("Grow");
            }
			while (!Equals(Mathf.Round(currentRideHeight), appliedRideHeight))
			{
				currentRideHeight = Mathf.Lerp(currentRideHeight, appliedRideHeight, Time.deltaTime * 2);
				for (int i = 0; i < wcList.Count(); i++)
					wcList[i].suspensionDistance = appliedRideHeight / 20f;
				yield return new WaitForFixedUpdate();
			}
            if (currentRideHeight < 1)
            {
				for (int i = 0; i < wcList.Count(); i++)
					wcList[i].enabled = false;

                StopCoroutine("Grow");
                StartCoroutine("Shrink");
            }
            //KFLog.Warning("Finished height update.");
        }

        [KSPAction("Dec. Height")]
        public void Retract(KSPActionParam param)
        {
            if (rideHeight > 0)
            {
                rideHeight -= Mathf.Clamp(susInc, 0f, 100f);
                //KFLog.Log("Retracting...");
				ApplySettingsAction();
            }
		}

        [KSPAction("Inc. Height")]
        public void Extend(KSPActionParam param)
        {
            if (rideHeight < 100)
            {
                rideHeight += Mathf.Clamp(susInc, 0f, 100f);
				//KFLog.Log("Extending...");
				ApplySettingsAction();
            }
		}

        #region Presets
		/// <summary>Handles preset rideHeight values.</summary>
		/// <param name="value">The height being requested. (0-100 float)</param>
		void Presetter(float value)
		{
			rideHeight = Mathf.Clamp(value, 0f, 100f);
			ApplySettingsAction();
		}
		[KSPAction("Repulsor Off")]
        public void HeightZero(KSPActionParam param)
        {
			Presetter(0);
        }
        [KSPAction("Height 10")]
        public void HeightMinimal(KSPActionParam param)
        {
			Presetter(10);
        }
        [KSPAction("Height 25")]
        public void HeightQuarter(KSPActionParam param)
        {
			Presetter(25);
        }
        [KSPAction("Height 50")]
        public void HeightHalf(KSPActionParam param)
        {
			Presetter(50);
        }
        [KSPAction("Height 75")]
        public void HeightThreeQuarter(KSPActionParam param)
        {
			Presetter(75);
        }
        [KSPAction("Height 100")]
        public void HeightFull(KSPActionParam param)
        {
			Presetter(100);
        }

        #endregion Presets

		public void ApplySettingsAction()
		{
			appliedRideHeight = rideHeight;
			StartCoroutine("UpdateHeight");
		}
		
        [KSPEvent(guiActive = true, guiName = "Apply Settings", active = true)]
        public void ApplySettings()
        {
            //appliedRideHeight = rideHeight;
            foreach (KFRepulsor mt in vessel.FindPartModulesImplementing<KFRepulsor>())
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
