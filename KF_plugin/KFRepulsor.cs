﻿/*
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
		public float fGroupNumber = 1f;
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Height"), UI_FloatRange(minValue = 0, maxValue = 100f, stepIncrement = 5f)]
		public float fRideHeight = 25f;
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Strength"), UI_FloatRange(minValue = 0, maxValue = 6.00f, stepIncrement = 0.2f)]
		public float springRate;
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Damping"), UI_FloatRange(minValue = 0, maxValue = 0.3f, stepIncrement = 0.05f)]
		public float damperRate;
		
		//[KSPField]
		public bool lowEnergy;
		[KSPField]
		public bool retractEffect;
		[KSPField]
		public bool pointDown;
		[KSPField]
		public string gridName;
		[KSPField]
		public string gimbalName;
		
		/// <summary>This is a part-centered boolean for the dust effects.  If set to false, the dust module will not be checked for nor enabled for this part.</summary>
		[KSPField]
		public bool isDustEnabled = true;
		
		bool isReady, isPaused;
		public Transform _grid;
		Transform _gimbal;
		
		Vector3 _gridScale;
		
		const float EFFECTPOWERMAX = 50f;
		float fAppliedRideHeight, fCurrentRideHeight, fSquish, fRepulsorCount, fCompression, fDir;
		
		KFDustFX _dustFX;
		
		public List<WheelCollider> wcList = new List<WheelCollider>();
		public bool isDeployed;
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
		public float fSusInc;
		
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
			isDeployed = true;
			
			if ((Equals(resourceName, "none") || Equals(resourceConsumptionRate, 0f)) && usesResources) // Implied: usesResources has been set to true.
				usesResources = false;
			
			fSusInc = KFPersistenceManager.suspensionIncrement;
			
			if (HighLogic.LoadedSceneIsFlight && (!Equals(vessel.vesselType, VesselType.Debris) && !Equals(vessel.vesselType, VesselType.EVA)))
			{
				_grid = transform.Search(gridName);
				_gridScale = _grid.transform.localScale;
				_gimbal = transform.Search(gimbalName);
				
				foreach (WheelCollider foundCollider in part.GetComponentsInChildren<WheelCollider>())
				{
					fRepulsorCount++;
					userspring = foundCollider.suspensionSpring;
					userspring.spring = springRate;
					userspring.damper = damperRate;
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
				
				fAppliedRideHeight = fRideHeight;
				StartCoroutine("UpdateHeight");
				
				SetupDust(state);
				SetupWaterSlider();
				isReady = true;
			}
			DestroyBounds();
			
			GameEvents.onGamePause.Add(OnPause);
			GameEvents.onGameUnpause.Add(OnUnpause);
		}
		
		/// <summary>Detects the DustFX component, or adds it if not detected on the part.</summary>
		/// <param name="state">Current state of the simulation.</param>
		void SetupDust(PartModule.StartState state)
		{
			if (isDustEnabled)
			{
				_dustFX = part.GetComponent<KFDustFX>();
				if (Equals(_dustFX, null))
				{
					_dustFX = part.gameObject.AddComponent<KFDustFX>();
					_dustFX.isRepulsor = true;
					_dustFX.OnStart(state);
				}
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
				_waterSlider.fColliderHeight = -2f;
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
			isDeployed = false;
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
			part.Effect("RepulsorEffect", power / EFFECTPOWERMAX);
		}
		
		/// <summary>Maintains the water slider.</summary>
		public void UpdateWaterSlider()
		{
			if (isPaused)
				return;
			_waterSlider.fColliderHeight = -2f;
		}
		
		/// <summary>Resource consumption handler.</summary>
		public void ResourceConsumption()
		{
			if (isPaused)
				return;
			float resRequest, resDrain;
			if (usesResources)
			{
				resRequest = resourceConsumptionRate * Time.deltaTime * fAppliedRideHeight / 100f;
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
			if (!isReady || isPaused)
				return;
			
			float sin, cos, hitForce, frameCompression, normalisedComp;
			bool anyGrounded, grounded;
			
			if (fDir > 360f)
				fDir = 0f;
			
			sin = (float)Math.Sin(Mathf.Deg2Rad * fDir);
			cos = (float)Math.Cos(Mathf.Deg2Rad * fDir);
			hitForce = 0f;
			
			var emitDirection = new Vector3(0f, sin * 10f, cos * 10f);
			
			if (isDeployed)
			{
				// Reset the height of the water collider that slips away every frame.
				UpdateWaterSlider();
				if (usesResources)
					ResourceConsumption();
				anyGrounded = false;
				frameCompression = 0f;
				for (int i = 0; i < wcList.Count(); i++)
				{
					WheelHit hit;
					grounded = wcList[i].GetGroundHit(out hit);
					if (grounded)
					{
						anyGrounded |= grounded;
						hitForce += hit.force;
						if (isDustEnabled && KFPersistenceManager.isDustEnabled)
							_dustFX.RepulsorEmit(hit.point, hit.collider, hit.force, hit.normal, emitDirection);
						frameCompression += -wcList[i].transform.InverseTransformPoint(hit.point).y - wcList[i].radius;
					}
					fCompression = frameCompression;
				}
				if (anyGrounded)
				{
					fCompression /= (wcList.Count() + 1);
					normalisedComp = fCompression / 8f;
					fSquish = normalisedComp / (fAppliedRideHeight / 100f);
				}
				else if (fSquish > 0.1f)
					fSquish /= 2f;
				
				if (lowEnergy)
				{
					#if DEBUG
					KFLog.Log(string.Format("Retracting due to low \"{0}\"", resourceName));
					#endif
					
					fAppliedRideHeight = 0f;
					fRideHeight = 0f;
					StartCoroutine("UpdateHeight");
					status = !Equals(resourceName, "ElectricCharge") ? string.Format("Low {0}", resourceName) : "Low Charge";
					isDeployed = false;
				}
				else
					status = "Nominal";
			}
			else
				status = lowEnergy ? "Low Charge" : "Off";
			
			RepulsorSound(hitForce);
			if (isDeployed && KFPersistenceManager.isRepLightEnabled && fRideHeight > 0f)
				_dustFX.RepulsorLight(fSquish);
			
			fDir += UnityEngine.Random.Range(20, 60);
			fSusInc = KFPersistenceManager.suspensionIncrement;
		}
		
		/// <summary>Updates the height of the repulsion field.</summary>
		/// <returns>Nothing.</returns>
		IEnumerator UpdateHeight()
		{
			if (fAppliedRideHeight > 0f)
			{
				for (int i = 0; i < wcList.Count(); i++)
					wcList[i].enabled = true;
				isDeployed = true;
				StopCoroutine("Shrink");
				StartCoroutine("Grow");
			}
			while (!Equals(Mathf.Round(fCurrentRideHeight), fAppliedRideHeight))
			{
				fCurrentRideHeight = Mathf.Lerp(fCurrentRideHeight, fAppliedRideHeight, Time.deltaTime * 2f);
				for (int i = 0; i < wcList.Count(); i++)
					wcList[i].suspensionDistance = fAppliedRideHeight / 20f;
				yield return new WaitForFixedUpdate();
			}
			if (fCurrentRideHeight < 1)
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
			if (fRideHeight > 0f)
			{
				fRideHeight -= Mathf.Clamp(fSusInc, 0f, 100f);
				ApplySettingsAction();
			}
		}
		
		/// <summary>Increments the height by the predefined interval, clamped between 0 and 100.</summary>
		/// <param name="param">Unused.</param>
		[KSPAction("Inc. Height")]
		public void Extend(KSPActionParam param)
		{
			if (fRideHeight < 100f)
			{
				fRideHeight += Mathf.Clamp(fSusInc, 0f, 100f);
				ApplySettingsAction();
			}
		}
		
		#region Presets
		/// <summary>Handles preset rideHeight values.</summary>
		/// <param name="value">The height being requested. (0-100 float)</param>
		void Presetter(float value)
		{
			fRideHeight = Mathf.Clamp(value, 0f, 100f);
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
			fAppliedRideHeight = fRideHeight;
			StartCoroutine("UpdateHeight");
		}
		
		/// <summary>"Apply Settings" event routine.</summary>
		[KSPEvent(guiActive = true, guiName = "Apply Settings", active = true)]
		public void ApplySettings()
		{
			//appliedRideHeight = rideHeight;
			foreach (KFRepulsor mt in vessel.FindPartModulesImplementing<KFRepulsor>())
			{
				if (!Equals(fGroupNumber, 0f) && Equals(fGroupNumber, mt.fGroupNumber))
				{
					mt.fRideHeight = fRideHeight;
					mt.fAppliedRideHeight = fRideHeight;
					mt.StartCoroutine("UpdateHeight");
				}
			}
		}
		
		#region Event Stuff
		
		/// <summary>Called when the game enters the "paused" state.</summary>
		void OnPause()
		{
			if (!isPaused)
				isPaused = true;
		}
		
		/// <summary>Called when the game leaves the "paused" state.</summary>
		void OnUnpause()
		{
			if (isPaused)
				isPaused = false;
		}
		
		/// <summary>Called when the object being referenced is destroyed, or when the module instance is deactivated.</summary>
		void OnDestroy()
		{
			isPaused = false;
			GameEvents.onGamePause.Remove(OnPause);
			GameEvents.onGameUnpause.Remove(OnUnpause);
		}
		
		#endregion Event Stuff
	}
}
