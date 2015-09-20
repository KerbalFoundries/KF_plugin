/*
 * KSP [0.23.5] Anti-Grav Repulsor plugin by Lo-Fi
 * Much inspiration and a couple of code snippets for deployment taken from BahamutoD's Critter Crawler mod. Huge respect, it's a fantastic mod :)
 * (Update): Fully compatible with KSP [1.0.4].
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KerbalFoundries.DustFX;

namespace KerbalFoundries
{
	/// <summary>Control module for the Anti-Gravity Repulsor.</summary>
	public class KFRepulsor : PartModule
	{
		public JointSpring userspring;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Repulsor Settings")]
		public string settings = string.Empty;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
		public string status = "Nominal";
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Group Number"), UI_FloatRange(minValue = 0, maxValue = 10f, stepIncrement = 1f)]
		public float groupNumber = 1f;
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Height"), UI_FloatRange(minValue = 0, maxValue = 100f, stepIncrement = 5f)]
		public float rideHeight = 25f;
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

		// disable ConvertToConstant.Local
		// disable RedundantDefaultFieldInitializer
		float effectPowerMax = 50f;
		float appliedRideHeight;
		float currentRideHeight;
		float repulsorCount = 0f;
		float compression = 0f;
		float squish;

		KFDustFX _dustFX;
		float dir;

		public List<WheelCollider> wcList = new List<WheelCollider>();
		public bool deployed;
		KFModuleWaterSlider _waterSlider;

		/// <summary>Defines the rate at which the specified resource is consumed.</summary>
		/// <remarks>Special Case: set this to 0 to disable resource consumption.</remarks>
		[KSPField]
		public float resourceConsumptionRate = 1f;

		/// <summary>defines the name of the resource to consume.</summary>
		/// <remarks>Special Case: set this to "none" to disable resource consumption.</remarks>
		[KSPField]
		public string resourceName = "ElectricCharge";
		
		/// <summary>Defines whether or not we use resources at all.</summary>
		/// <remarks>Default is "true"</remarks>
		[KSPField]
		public bool usesResources = true;
		
		/// <summary>Suspension increment/decrement value for height controls.</summary>
		public float susInc;

		/// <summary>This is the info string that will display when the part info is shown.</summary>
		/// <remarks>This can be overridden in the part config for this module due to its KSPField status.</remarks>
		[KSPField]
		public string strPartInfo = "This part allows the craft to hover above the ground.  Steering mechanism not included.";
		public override string GetInfo()
		{
			return UpdateInfoText(strPartInfo, resourceName, resourceConsumptionRate, usesResources);
		}

		/// <summary>Updates the part info strings.</summary>
		/// <param name="inputPartInfo">The base string for the part module info.</param>
		/// <param name="strResourceName">The resource being requested for use.</param>
		/// <param name="consumptionRate">The rate at which this part initially comsumes the resource.</param>
		/// <param name="enabled">Whether or not resource consumption is even enabled.</param>
		/// <returns>The completed part module info string.</returns>
		static string UpdateInfoText(string inputPartInfo, string strResourceName, float consumptionRate, bool enabled)
		{
			string infoString = inputPartInfo;
			infoString += enabled ? string.Format("\n\n<b><color=#99ff00ff>Requires:</color></b>\n- {0}: {1}/sec @ max height.", strResourceName, consumptionRate.ToString("0.00")) : "\n\n<b><color=#99ff00ff>No Resource Usage!</color></b>";
			return infoString;
		}
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFRepulsor");
		
		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			deployed = true;
			
			if ((Equals(resourceName, "none") || Equals(resourceConsumptionRate, 0f)) && usesResources) // Implied: usesResources has been set to true.
				usesResources = false;
			
			susInc = KFPersistenceManager.suspensionIncrement;
			
			if (HighLogic.LoadedSceneIsFlight && (!Equals(vessel.vesselType, VesselType.Debris) && !Equals(vessel.vesselType, VesselType.EVA)))
			{
				_grid = transform.Search(gridName);
				_gridScale = _grid.transform.localScale;
				_gimbal = transform.Search(gimbalName);
				
				foreach (WheelCollider foundCollider in part.GetComponentsInChildren<WheelCollider>())
				{
					repulsorCount++;
					userspring = foundCollider.suspensionSpring;
					userspring.spring = SpringRate;
					userspring.damper = DamperRate;
					foundCollider.suspensionSpring = userspring;
					foundCollider.suspensionDistance = 2.5f;
					wcList.Add(foundCollider);
				}
				
				#if DEBUG
				KFLog.Log(string.Format("Repulsor Count: {0}", repulsorCount));
				#endif
				
				if (pointDown)
				{
					StopAllCoroutines();
					StartCoroutine("LookAt");
				}
				
				appliedRideHeight = rideHeight;
				StartCoroutine("UpdateHeight");
				
				SetupDust(state);
				SetupWaterSlider();
				isReady = true;
			}
			DestroyBounds();
		}
		
		/// <summary>Detects the DustFX component, or adds it if not detected on the part.</summary>
		/// <param name="state">Current state of the simulation.</param>
		void SetupDust(PartModule.StartState state)
		{
			_dustFX = part.GetComponent<KFDustFX>();
			if (Equals(_dustFX, null))
			{
				_dustFX = part.gameObject.AddComponent<KFDustFX>();
				_dustFX.isRepulsor = true;
				_dustFX.OnStart(state);
			}
		}
		
		/// <summary>Detects the water slider component, or adds it if not found on the vessel.</summary>
		void SetupWaterSlider()
		{
			_waterSlider = vessel.rootPart.GetComponent<KFModuleWaterSlider>();
			if (Equals(_waterSlider, null))
			{
				_waterSlider = vessel.rootPart.gameObject.AddComponent<KFModuleWaterSlider>();
				_waterSlider.StartUp();
			}
		}

		/// <summary>A "Shrink" coroutine for an animation.</summary>
		IEnumerator Shrink()
		{
			while (_grid.transform.localScale.x > 0.2f && _grid.transform.localScale.y > 0.2f && _grid.transform.localScale.z > 0.2f)
			{
				_grid.transform.localScale -= (_gridScale / 50f);
				yield return null;
			}
			_grid.localScale = new Vector3(0.01f, 0.01f, 0.01f);
			deployed = false;
		}

		/// <summary>A "grow" coroutine for an animation.</summary>
		IEnumerator Grow()
		{
			while (_grid.transform.localScale.x < _gridScale.x && _grid.transform.localScale.y < _gridScale.y && _grid.transform.localScale.z < _gridScale.z)
			{
				_grid.transform.localScale += (_gridScale / 50f);
				yield return null;
			}
			_grid.transform.localScale = _gridScale;
		}
		
		// disable once FunctionNeverReturns
		/// <summary>A "LookAt" coroutine for repulsor gimbals.</summary>
		IEnumerator LookAt()
		{
			while (true)
			{
				_gimbal.transform.LookAt(FlightGlobals.ActiveVessel.mainBody.transform.position);
				yield return null;
			}
		}

		/// <summary>Destroys the "bounds" object if detected.  We don't need it anymore.</summary>
		public void DestroyBounds()
		{
			Transform bounds = transform.Search("Bounds");
			if (!Equals(bounds, null))
				UnityEngine.Object.Destroy(bounds.gameObject);
		}
		
		/// <summary>Maintains the sound effects.</summary>
		/// <param name="power">Power level for the effect.</param>
		public void RepulsorSound(float power)
		{
			part.Effect("RepulsorEffect", power / effectPowerMax);
		}
		
		/// <summary>Maintains the water slider.</summary>
		public void UpdateWaterSlider()
		{
			_waterSlider.colliderHeight = -2f;
		}
		
		/// <summary>Resource consumption handler.</summary>
		public void ResourceConsumption()
		{
			float resRequest;
			float resDrain;
			if (usesResources)
			{
				resRequest = resourceConsumptionRate * Time.deltaTime * appliedRideHeight / 100f;
				resDrain = part.RequestResource(resourceName, resRequest);
				lowEnergy = resDrain < resRequest;
			}
			else
			{
				resRequest = 0f;
				resDrain = 0f;
				lowEnergy = false;
			}
		}
		
		/// <summary>Fixed interval update process.</summary>
		public void FixedUpdate()
		{
			if (!isReady)
				return;
			
			if (dir > 360f)
				dir = 0f;
			
			float sin = (float)Math.Sin(Mathf.Deg2Rad * dir);
			float cos = (float)Math.Cos(Mathf.Deg2Rad * dir);
			var emitDirection = new Vector3(0f, sin * 10f, cos * 10f);
			float hitForce = 0f;
			
			if (deployed)
			{
				// Reset the height of the water collider that slips away every frame.
				UpdateWaterSlider();
				if (usesResources)
					ResourceConsumption();
				bool anyGrounded = false;
				float frameCompression = 0f;
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
					float normalisedComp = compression / 8f;
					squish = normalisedComp / (appliedRideHeight / 100f);
				}
				else if (squish > 0.1f)
					squish /= 2f;
				
				if (lowEnergy)
				{
					#if DEBUG
					KFLog.Log(string.Format("Retracting due to low \"{0}\"", resourceName));
					#endif
					
					appliedRideHeight = 0f;
					rideHeight = 0f;
					StartCoroutine("UpdateHeight");
					status = !Equals(resourceName, "ElectricCharge") ? string.Format("Low {0}", resourceName) : "Low Charge";
					deployed = false;
				}
				else
					status = "Nominal";
			}
			else
				status = lowEnergy ? "Low Charge" : "Off";
			
			RepulsorSound(hitForce);
			if (deployed && KFPersistenceManager.isRepLightEnabled && rideHeight > 0f)
				_dustFX.RepulsorLight(squish);
			
			dir += UnityEngine.Random.Range(20, 60);
			susInc = KFPersistenceManager.suspensionIncrement;
		}

		/// <summary>Updates the height of the repulsion field.</summary>
		/// <returns>Nothing.</returns>
		IEnumerator UpdateHeight()
		{
			if (appliedRideHeight > 0f)
			{
				for (int i = 0; i < wcList.Count(); i++)
					wcList[i].enabled = true;
				deployed = true;
				StopCoroutine("Shrink");
				StartCoroutine("Grow");
			}
			while (!Equals(Mathf.Round(currentRideHeight), appliedRideHeight))
			{
				currentRideHeight = Mathf.Lerp(currentRideHeight, appliedRideHeight, Time.deltaTime * 2f);
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
		}
		
		/// <summary>Decrements the height by the predefined interval, clamped between 0 and 100.</summary>
		/// <param name="param">Unused.</param>
		[KSPAction("Dec. Height")]
		public void Retract(KSPActionParam param)
		{
			if (rideHeight > 0f)
			{
				rideHeight -= Mathf.Clamp(susInc, 0f, 100f);
				ApplySettingsAction();
			}
		}
		
		/// <summary>Increments the height by the predefined interval, clamped between 0 and 100.</summary>
		/// <param name="param">Unused.</param>
		[KSPAction("Inc. Height")]
		public void Extend(KSPActionParam param)
		{
			if (rideHeight < 100f)
			{
				rideHeight += Mathf.Clamp(susInc, 0f, 100f);
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
		
		/// <summary>"Apply Settings" action routine.</summary>
		public void ApplySettingsAction()
		{
			appliedRideHeight = rideHeight;
			StartCoroutine("UpdateHeight");
		}
		
		/// <summary>"Apply Settings" event routine.</summary>
		[KSPEvent(guiActive = true, guiName = "Apply Settings", active = true)]
		public void ApplySettings()
		{
			//appliedRideHeight = rideHeight;
			foreach (KFRepulsor mt in vessel.FindPartModulesImplementing<KFRepulsor>())
			{
				if (!Equals(groupNumber, 0f) && Equals(groupNumber, mt.groupNumber))
				{
					mt.rideHeight = rideHeight;
					mt.appliedRideHeight = rideHeight;
					mt.StartCoroutine("UpdateHeight");
				}
			}
		}
	}
}
