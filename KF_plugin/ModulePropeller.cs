using System;
using System.Linq;

namespace KerbalFoundries
{
    class ModulePropeller : PartModule
    {
        KFModuleWheel master;

        [KSPField]
        public float propellerForce = 5;

        public override void OnStart(PartModule.StartState state)
        {
            print("ModulePropeller called");
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight) 
				master = part.GetComponentInChildren<KFModuleWheel>();
            }

        public override void OnUpdate()
        {
            base.OnUpdate();
			if (part.Splashed)
            {
				float forwardPropellorForce = master.directionCorrector * propellerForce * vessel.ctrlState.wheelThrottle;
				float turningPropellorForce = (propellerForce / 3) * vessel.ctrlState.wheelSteer;
				part.rigidbody.AddForce(part.GetReferenceTransform().forward * (forwardPropellorForce - turningPropellorForce));
            }
        }
    }
} 
