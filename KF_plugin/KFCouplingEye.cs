using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFCouplingEyeOLD : PartModule
    {
        public bool isHitched;
        public bool isPacked;
        public Transform _hitchObject;
        public Transform _relativePostion;
        public Vector3 _relativeRotation;
        
        [KSPField]
        public string eyeObjectName;

        Transform _eyeObject;

        public void VesselUnPack(Vessel vessel)
        {
			if (Equals(vessel, this.vessel))
            {
                Debug.LogWarning("Unpacked and this vessel");
                isPacked = false;
            }
        }

        public void VesselPack(Vessel vessel)
        {
			if (Equals(vessel, this.vessel))
            {
                Debug.LogWarning("Packed and this vessel");
                isPacked = true;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            GameEvents.onVesselGoOffRails.Add(VesselUnPack);
            GameEvents.onVesselGoOnRails.Add(VesselPack);
            _eyeObject = this.part.transform.Search(eyeObjectName);
        }

        public void FixedUpdate()
        {
            // Nothing here?
        }
    }
}
