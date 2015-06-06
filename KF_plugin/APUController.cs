using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    [KSPModule("APUController")]
    class APUController : PartModule
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Throttle"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 5f)]
		public float throttleSetting = 50;
        public ModuleEngines thisEngine;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Mode"), UI_Toggle(disabledText = "Manual", enabledText = "Auto")]
        public bool autoThrottle = true;
        public float lastRatio = .5f;
        [KSPField]
        public float reactionSpeed = 5; 
        [KSPField]
        public float targetBatteryRatio = 0.75f;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Ratio Adjustment", guiFormat = "F8")]
        public float ratioAdjustment;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Usage Adjustment", guiFormat = "F8")]
        public float usageAdjustment;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Auto Throttle", guiFormat = "F8")]
        public float autoThrottleSetting = .5f;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Battery Ratio", guiFormat = "F8")]
        public float batteryRatio = .5f;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            print(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            FindEngine(); 
            if (HighLogic.LoadedSceneIsFlight)
                this.part.force_activate();
            }

        public override void OnFixedUpdate()
        { 
            base.OnFixedUpdate();
            if (autoThrottle)
            {
                batteryRatio = Extensions.GetBattery(this.part);
                usageAdjustment = (lastRatio - batteryRatio) * reactionSpeed;
                ratioAdjustment = Mathf.Clamp((targetBatteryRatio - batteryRatio), -0.001f, 0.001f);
				float tempThrottle = Mathf.Clamp(autoThrottleSetting + ratioAdjustment + usageAdjustment, 0.01f, 1);
                autoThrottleSetting = tempThrottle;
                thisEngine.currentThrottle = autoThrottleSetting;
                lastRatio = batteryRatio;
            }
            else
                thisEngine.currentThrottle = Mathf.Lerp((throttleSetting / 100), thisEngine.currentThrottle, Time.deltaTime * 40f);
            }

        public void FindEngine()
        {
            foreach (ModuleEngines me in this.part.GetComponentsInChildren<ModuleEngines>())
            {
                print("Found an engine");
                thisEngine = me;
            }
        }

        [KSPAction("APU + output")]
        public void IncreaseAPU(KSPActionParam param)
        {
            if (throttleSetting < 100)
            {
                throttleSetting += 5f;
                print("Increasing APU Output");
            }
		}
		//End Retract

        [KSPAction("APU - output")]
        public void DecreaseAPU(KSPActionParam param)
        {
            if (throttleSetting > 0)
            {
                throttleSetting -= 5f;
                print("Decreasing APU Output");
            }
		}
		//End Retract

        [KSPAction("APU Shutdown")]
        public void ShutdownAPU(KSPActionParam param)
        {
            throttleSetting = 0f;
            print("Shutting down APU");
		}
		//End Retract

        [KSPAction("APU Automatic")]
        public void AutoAPU(KSPActionParam param)
        {
            autoThrottle = true;
            print("APU Automatic");
        }

        [KSPAction("APU Manual")]
        public void ManualAPU(KSPActionParam param)
        {
            autoThrottle = false;
            print("APU Manual");
        }
    }
}
