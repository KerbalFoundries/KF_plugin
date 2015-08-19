using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFSteering : PartModule
    {
        [KSPField]
        public string steeringObject;
        [KSPField]
        public string steeringAxis = "Y";

        int steeringIndex = 1;
        Transform _steering;
        Vector3 initialSteeringAngle;

        KFModuleWheel _KFModuleWheel;

        public override void OnStart(PartModule.StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight && !Equals(vessel.vesselType, VesselType.Debris) && vessel.parts.Count > 1)
            {
                _KFModuleWheel = part.GetComponentInChildren<KFModuleWheel>();
                _steering = part.FindModelTransform(steeringObject);
                initialSteeringAngle = _steering.transform.localEulerAngles;
                steeringIndex = Extensions.SetAxisIndex(steeringAxis);

                base.OnStart(state);
                StartCoroutine(Steering());
            }
        }

        // disable FunctionNeverReturns
		IEnumerator Steering() // Coroutine for steering
        {
            while (true)
            {
                Vector3 newSteeringAngle = initialSteeringAngle;
                newSteeringAngle[steeringIndex] += _KFModuleWheel.steeringAngle;
                _steering.transform.localEulerAngles = newSteeringAngle;
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
