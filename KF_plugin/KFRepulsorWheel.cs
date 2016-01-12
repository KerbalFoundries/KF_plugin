/*
 * KSP [0.23.5] Anti-Grav Repulsor plugin by Lo-Fi
 * Much inspiration and a couple of code snippets for deployment taken from BahamutoD's Critter Crawler mod. Huge respect, it's a fantastic mod :)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>Control module for the convertible repulsor-wheel system.</summary>
	/// <remarks>Long overdue for a refactor and revival.</remarks>
	public class KFRepulsorWheel : PartModule
	{
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Repulsor Height %"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 5)]
		public float fRepulsorHeight = 50;
		
		const float REPULSORHEIGHTMULT = 5f;
		
		[KSPField(isPersistant = true)]
		public bool repulsorMode;
		
		[KSPField]
		public string resourceName = "ElectricCharge";
		
		[KSPField]
		public float resourceConsumptionRate = 1f;
		
		float effectPower, effectPowerMax;
		bool lowResource;
		
		List<WheelCollider> wcList = new List<WheelCollider>();
		List<float> wfForwardList = new List<float>();
		List<float> susDistList = new List<float>();
		List<float> wfSideList = new List<float>();
		
		KFModuleWheel _moduleWheel;
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFRepulsorWheel");
		
		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
            
			if (HighLogic.LoadedSceneIsEditor)
			{
				foreach (ModuleAnimateGeneric ma in part.FindModulesImplementing<ModuleAnimateGeneric>())
				{
					ma.Actions["ToggleAction"].active = false;
					ma.Events["Toggle"].guiActive = false;
					ma.Events["Toggle"].guiActiveEditor = false;
				}
			}
			
			if (HighLogic.LoadedSceneIsFlight)
			{
				#if DEBUG
				KFLog.Log("Repulsor Wheel started");
				#endif
				
				foreach (ModuleAnimateGeneric ma in part.FindModulesImplementing<ModuleAnimateGeneric>())
					ma.Events["Toggle"].guiActive = false;
				
				_moduleWheel = part.GetComponentInChildren<KFModuleWheel>();
				_moduleWheel.Events["ApplySettings"].guiActive = false;
                
				foreach (WheelCollider wc in part.GetComponentsInChildren<WheelCollider>())
					wcList.Add(wc);
				
				for (int i = 0; i < wcList.Count(); i++)
				{
					wfForwardList.Add(wcList[i].forwardFriction.stiffness);
					wfSideList.Add(wcList[i].sidewaysFriction.stiffness);
					susDistList.Add(wcList[i].suspensionDistance);
				}
				
				if (repulsorMode)
                    UpdateColliders("repulsor");
				if (!repulsorMode)
					UpdateColliders("wheel");
				effectPowerMax = resourceConsumptionRate * Time.deltaTime;
				KFLog.Log(string.Format("\"effectPowerMax\" = {0}", effectPowerMax));
			}
		}
		
		public override void OnUpdate()
		{
			base.OnUpdate();
			
			float fResourceConsumption, fRequestedResource;
			
			if (repulsorMode)
			{
				fResourceConsumption = (fRepulsorHeight / 2) * (1 + _moduleWheel.fSpringRate) * Time.deltaTime * resourceConsumptionRate;
				effectPower = fResourceConsumption / effectPowerMax;
				KFLog.Log(string.Format("\"effectPower\" = {0}", effectPower));
				
				fRequestedResource = part.RequestResource(resourceName, fResourceConsumption);
				if (fRequestedResource < (fResourceConsumption * 0.9f))
				{
					#if DEBUG
					KFLog.Warning(string.Format("Retracting due to low \"{0}.\"", resourceName));
					#endif
					
					lowResource = true;
					fRepulsorHeight = 0;
					UpdateColliders("wheel");
					_moduleWheel.status = Equals(resourceName, "ElectricCharge") ? "Low Charge" : _moduleWheel.statusLowResource;
				}
				else
				{
					lowResource = false;
					_moduleWheel.status = _moduleWheel.statusNominal;
				}
			}
			else
				effectPower = 0;
			
			RepulsorSound();
			effectPower = 0;
		}
		
		public void RepulsorSound()
		{
			part.Effect("RepulsorEffect", effectPower);
		}
		
		public void UpdateColliders(string mode)
		{
			switch (mode)
			{
				case "repulsor":
					if (lowResource)
						return;
					_moduleWheel.RetractDeploy("retract");
					_moduleWheel.fCurrentTravel = fRepulsorHeight * REPULSORHEIGHTMULT;
					repulsorMode = true;
					for (int i = 0; i < wcList.Count(); i++)
					{
						WheelFrictionCurve wf = wcList[i].forwardFriction;
						wf.stiffness = 0;
						wcList[i].forwardFriction = wf;
						wf = wcList[i].sidewaysFriction;
						wf.stiffness = 0;
						wcList[i].sidewaysFriction = wf;
					}
					break;
				case "wheel":
					_moduleWheel.fCurrentTravel = _moduleWheel.fRideHeight;
					_moduleWheel.fSmoothedTravel = _moduleWheel.fCurrentTravel;
					_moduleWheel.RetractDeploy("deploy");
					repulsorMode = false;
					for (int i = 0; i < wcList.Count(); i++)
					{
						WheelFrictionCurve wf = wcList[i].forwardFriction;
						wf.stiffness = wfForwardList[i];
						wcList[i].forwardFriction = wf;
						wf = wcList[i].sidewaysFriction;
						wf.stiffness = wfSideList[i];
						wcList[i].sidewaysFriction = wf;
					}
					break;
			}
		}
		
		[KSPEvent(guiActive = true, guiName = "Apply Repulsor Settings", active = true)]
		public void ApplySettings()
		{
			foreach (KFModuleWheel mt in vessel.FindPartModulesImplementing<KFModuleWheel>())
			{
				if (!Equals(_moduleWheel.fGroupNumber, 0) && Equals(_moduleWheel.fGroupNumber, mt.fGroupNumber) && repulsorMode)
				{
					_moduleWheel.fCurrentTravel = fRepulsorHeight * REPULSORHEIGHTMULT;
					mt.fCurrentTravel = fRepulsorHeight * REPULSORHEIGHTMULT;
				}
			}
			foreach (KFRepulsorWheel rw in vessel.FindPartModulesImplementing<KFRepulsorWheel>())
				rw.fRepulsorHeight = fRepulsorHeight;
		}
		
		[KSPAction("Toggle Modes")]
		public void AGToggleDeployed(KSPActionParam param)
		{
			if (repulsorMode)
				toWheel(param);
			else
				toRepulsor(param);
		}
		
		public void PlayAnimation()
		{
			ModuleAnimateGeneric myAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault();
			if (!myAnimation)
				return;
			myAnimation.Toggle();
		}
		
		[KSPAction("Wheel Mode")]
		public void toWheel(KSPActionParam param)
		{
			if (repulsorMode)
			{
				PlayAnimation();
				repulsorMode = false;
				UpdateColliders("wheel");
			}
		}
		
		[KSPAction("Repulsor Mode")]
		public void toRepulsor(KSPActionParam param)
		{
			if (lowResource)
				return;
			if (!repulsorMode)
			{
				PlayAnimation();
				repulsorMode = true;
				UpdateColliders("repulsor");
			}
		}
	}
}
