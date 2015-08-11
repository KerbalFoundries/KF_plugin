using System;
using System.Linq;

namespace KerbalFoundries
{
	class ModulePropeller : PartModule
	{
		KFModuleWheel master;

		[KSPField]
		public float propellerForce = 5;

		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("ModulePropeller");
		
		public override void OnStart(PartModule.StartState state)
		{
			KFLog.Log("ModulePropeller called");
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
