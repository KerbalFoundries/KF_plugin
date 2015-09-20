using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>Control module for the Auxilary Power Unit.</summary>
	[KSPModule("KFAPUController")]
	public class KFAPUController : PartModule
	{
		/// <summary>Uses ModuleEnginesFX specifically for the APU.</summary>
		public ModuleEnginesFX _moduleEnginesFX;
		
		/// <summary>Current throttle setting.</summary>
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Throttle"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 5f)]
		public float throttleSetting = 50f;
		
		/// <summary>Whether or not auto-throttle controls are enabled.</summary>
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Mode"), UI_Toggle(disabledText = "Manual", enabledText = "Auto")]
		public bool autoThrottle = true;
		
		/// <summary>The previously set ratio.</summary>
		/// <remarks>Not a field, used by the code only.</remarks>
		public float lastRatio = .5f;
		
		/// <summary>How fast we react to changes.</summary>
		[KSPField]
		public float reactionSpeed = 5f;
		
		/// <summary>Percentage of charge we are targetting as our "low threshold" level.</summary>
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Target Charge %"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 5f)]
		public float targetBatteryRatio = 75f;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Ratio Adjustment", guiFormat = "F8")]
		public float ratioAdjustment;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Usage Adjustment", guiFormat = "F8")]
		public float usageAdjustment;
		
		/// <summary>Auto-throttle level readout.</summary>
		[KSPField(isPersistant = false, guiActive = true, guiName = "Auto Throttle", guiFormat = "F8")]
		public float autoThrottleSetting = .5f;
		
		/// <summary>The relative charge in the batteries to keep track of.</summary>
		[KSPField(isPersistant = false, guiActive = true, guiName = "Battery Ratio", guiFormat = "F8")]
		public float batteryRatio = .5f;
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFAPUController");

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			
			#if DEBUG
			KFLog.Log(string.Format("{0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version));
			#endif
			
			FindEngine();
			if (HighLogic.LoadedSceneIsFlight)
				part.force_activate();
		}

		public override void OnFixedUpdate()
		{ 
			base.OnFixedUpdate();
			if (autoThrottle)
			{
				batteryRatio = part.GetBattery();
				usageAdjustment = (lastRatio - batteryRatio) * reactionSpeed;
				ratioAdjustment = Mathf.Clamp(((targetBatteryRatio / 100f) - batteryRatio), -0.001f, 0.001f);
				float tempThrottle = Mathf.Clamp(autoThrottleSetting + ratioAdjustment + usageAdjustment, 0.01f, 1f);
				autoThrottleSetting = tempThrottle;
				_moduleEnginesFX.currentThrottle = autoThrottleSetting;
				lastRatio = batteryRatio;
			}
			else
				_moduleEnginesFX.currentThrottle = Mathf.Lerp((throttleSetting / 100f), _moduleEnginesFX.currentThrottle, Time.deltaTime * 40f);
		}

		public void FindEngine()
		{
			foreach (ModuleEnginesFX engineFound in part.GetComponentsInChildren<ModuleEnginesFX>())
			{
				#if DEBUG
				KFLog.Log("Found an engine module.");
				#endif
				
				_moduleEnginesFX = engineFound;
			}
		}

		[KSPAction("APU + output")]
		public void IncreaseAPU(KSPActionParam param)
		{
			if (throttleSetting < 100f)
			{
				throttleSetting += 5f;
				
				#if DEBUG
				KFLog.Log("Increasing APU Output.");
				#endif
			}
		}

		[KSPAction("APU - output")]
		public void DecreaseAPU(KSPActionParam param)
		{
			if (throttleSetting > 0f)
			{
				throttleSetting -= 5f;
				
				#if DEBUG
				KFLog.Log("Decreasing APU Output.");
				#endif
			}
		}

		[KSPAction("APU Shutdown")]
		public void ShutdownAPU(KSPActionParam param)
		{
			throttleSetting = 0f;
			
			#if DEBUG
			KFLog.Log("Shutting down APU.");
			#endif
		}

		[KSPAction("APU Automatic")]
		public void AutoAPU(KSPActionParam param)
		{
			autoThrottle = true;
			
			#if DEBUG
			KFLog.Log("APU set to Automatic.");
			#endif
		}

		[KSPAction("APU Manual")]
		public void ManualAPU(KSPActionParam param)
		{
			autoThrottle = false;
			
			#if DEBUG
			KFLog.Log("APU set to Manual.");
			#endif
		}
	}
}
