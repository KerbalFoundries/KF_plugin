using System;
using System.Linq;

namespace KerbalFoundries
{
	/// <summary>Controls the ability for certain tracks and the Screw drive to propel the vessel through the water.</summary>
	public class KFModulePropeller : PartModule
	{
		KFModuleWheel _KFModuleWheel;
		
		[KSPField]
		public float propellerForce = 5f;
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFModulePropeller");
		
		public override void OnStart(PartModule.StartState state)
		{
			if (!HighLogic.LoadedSceneIsFlight || (Equals(vessel.vesselType, VesselType.Debris) || Equals(vessel.vesselType, VesselType.EVA)))
				return;
			
			#if DEBUG
			KFLog.Log("ModulePropeller called.");
			#endif
			
			_KFModuleWheel = part.GetComponentInChildren<KFModuleWheel>();
			base.OnStart(state);
		}
		
		public override void OnUpdate()
		{
			base.OnUpdate();
			if (part.Splashed)
			{
				float forwardPropellorForce = _KFModuleWheel.directionCorrector * propellerForce * vessel.ctrlState.wheelThrottle;
				float turningPropellorForce = (propellerForce / 3f) * vessel.ctrlState.wheelSteer;
				part.rigidbody.AddForce(part.GetReferenceTransform().forward * (forwardPropellorForce - turningPropellorForce));
			}
		}
	}
}
