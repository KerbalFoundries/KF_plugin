using System;

namespace KerbalFoundries
{
	/// <summary>Control module for a "precision mode" which basically multiplies the smoothSpeed parameter and the resourceConsumptionRate parameter by a specified factor.</summary>
	[KSPModule("KFPrecisionMode")]
	public class KFPrecisionMode : PartModule
	{
		bool bPrecisionModeActive;
		KFModuleWheel _KFModuleWheel;
		
		/// <summary>Multiplier applied to the smooth speed value.</summary>
		[KSPField]
		public float smoothSpeedMult = 2f;
		
		/// <summary>Multiplier applied to the resource consumption rate.</summary>
		[KSPField]
		public float ResourceConsumptionMult = 2f;
		
		[KSPField(isPersistant = false, guiActive = true, guiName = "Precision")]
        public string status = "Off";
        
        [KSPField(isPersistant = false, guiActive = true, guiName = "Smooth Speed", guiFormat = "F1")]
		public float currentSmoothSpeed;
		
		float originalSmoothSpeed;
		float appliedSmoothSpeed;
		float originalResourceConsumption;
		float appliedResourceConsumption;
		bool overrideActive;
		bool doOnce;
		
		public string statusOn = "On";
		public string statusOff = "Off";
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFPrecisionMode");
		
		/// <summary>This is the info string that will display when the part info is shown.</summary>
		/// <remarks>This can be overridden in the config for this module in the part file.</remarks>
		[KSPField]
		public string strPartInfo = "This part comes with a precision control mode which can be activated via an action group.  This enabled quicker turning speed for a greater resource consumption rate.";
		public override string GetInfo()
		{
			return strPartInfo;
		}
		
		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			bPrecisionModeActive = false;
			
			_KFModuleWheel = part.GetComponent<KFModuleWheel>();
			originalSmoothSpeed = _KFModuleWheel.smoothSpeed;
			
			appliedSmoothSpeed = originalSmoothSpeed;
			
			originalResourceConsumption = _KFModuleWheel.resourceConsumptionRate;
			appliedResourceConsumption = originalResourceConsumption;
			
			#if DEBUG
			KFLog.Log(string.Format("Original Smooth Speed = {0}", originalSmoothSpeed));
			KFLog.Log(string.Format("Boosted Smooth Speed = {0}", (originalSmoothSpeed * smoothSpeedMult)));
			#endif
		}

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();
			//IsPrecisionModeActive();
			if (doOnce)
				ApplyPrecisionMode(bPrecisionModeActive);
			status = bPrecisionModeActive ? statusOn : statusOff;
			currentSmoothSpeed = appliedSmoothSpeed;
		}
		
		/// <summary>Gets the current state of the precision control system.</summary>
		public void IsPrecisionModeActive() // Needs work, doesn't currently function.
		{
			if (GameSettings.PRECISION_CTRL.GetKeyDown() && !overrideActive)
			{
				bPrecisionModeActive = !bPrecisionModeActive;
				doOnce = true;
			}
		}
		
		/// <summary>Modifies the requisite variables.</summary>
		/// <param name="isActive">If true, apply the change, else reset to default.</param>
		void ApplyPrecisionMode(bool isActive)
		{
			if (isActive)
			{
				appliedSmoothSpeed *= smoothSpeedMult;
				appliedResourceConsumption *= ResourceConsumptionMult; 
			}
			else
			{
				appliedSmoothSpeed = originalSmoothSpeed;
				appliedResourceConsumption = originalResourceConsumption;
			}
			doOnce = false;
			UpdateWheels();
		}
		
		// disable once UnusedParameter
		[KSPAction("Toggle Precision")]
		public void OverridePrecisionMode(KSPActionParam param)
		{
			overrideActive = !overrideActive;
			bPrecisionModeActive = !bPrecisionModeActive;
			doOnce = true;
		}
		
		/// <summary>Iterates through all the applicable parts and syncs the variables.</summary>
		void UpdateWheels()
		{
			foreach (KFModuleWheel wheelsOnVessel in vessel.FindPartModulesImplementing<KFModuleWheel>())
			{
				wheelsOnVessel.smoothSpeed = appliedSmoothSpeed;
				wheelsOnVessel.resourceConsumptionRate = appliedResourceConsumption;
			}
		}
	}
}