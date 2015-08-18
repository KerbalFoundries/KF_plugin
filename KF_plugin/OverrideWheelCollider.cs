using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class OverrideWheelCollider : PartModule
    {
        [KSPField]
        public string colliderName = "wheelCollider";
        [KSPField]
        public string susTravName = "suspensionTraverse";
        [KSPField]
		public float mass;
        [KSPField]
		public float radius;
        [KSPField]
		public float suspensionDistance;
        [KSPField]
		public float spring;
        [KSPField]
		public float damper;
        [KSPField]
		public float targetPosition;
        
        [KSPField]
        public float forExtSlip = 1;
        [KSPField]
        public float forExtValue = 120;
        [KSPField]
        public float forAsySlip = 2;
        [KSPField]
        public float forAsyValue = 80;
        [KSPField]
        public float forStiffness = 1;

        [KSPField]
        public float sideExtSlip = 1;
        [KSPField]
        public float sideExtValue = 120;
        [KSPField]
        public float sideAsySlip = 2;
        [KSPField]
        public float sideAsyValue = 80;
        [KSPField]
        public float sideStiffness = 1;
        [KSPField]
        public bool moveCollider;
        [KSPField]
        public bool overrideFriction = true;
        [KSPField]
        public float moveColliderBy;
        [KSPField]
        public int susTravIndex = 1;

        WheelCollider _wheelCollider;

        /// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("OverrideWheelCollider");
        
        public override void OnStart(PartModule.StartState state)
        {
            GameObject _susTrav = part.FindModelTransform(susTravName).gameObject;
            foreach (var wc in part.GetComponentsInChildren<WheelCollider>())
            {
                if (wc.name.Equals(colliderName, StringComparison.Ordinal))
                    _wheelCollider = wc;
                else
                    KFLog.Error("Wheel collider not found.");
            }

			if (HighLogic.LoadedSceneIsFlight && moveCollider)
			{
				_wheelCollider.transform.Translate(0, moveColliderBy, 0, Space.Self);
				// SharpDevelop suggested we use "var" instead of reiterating "Vector3" in this definition. - Gaalidas
				var tempVector = new Vector3(0, 0, 0);
				//Vector3 tempVector = new Vector3(0, 0, 0);
				tempVector[susTravIndex] = moveColliderBy;
				_susTrav.transform.Translate(tempVector, Space.Self);
			}

			if (!Equals(_wheelCollider, null))
            {
				if (!Equals(mass, 0))
                    _wheelCollider.mass = mass;
				if (!Equals(radius, 0))
                    _wheelCollider.radius = radius;
				if (!Equals(suspensionDistance, 0))
                    _wheelCollider.suspensionDistance = suspensionDistance;
				if (!Equals(spring, 0))
                {
                    JointSpring js = _wheelCollider.suspensionSpring;
                    js.spring = spring;
                    _wheelCollider.suspensionSpring = js;
                }
				if (!Equals(damper, 0))
                {
                    JointSpring js = _wheelCollider.suspensionSpring;
                    js.damper = damper;
                    _wheelCollider.suspensionSpring = js;
                }
				if (!Equals(targetPosition, 0))
                { 
                    JointSpring js = _wheelCollider.suspensionSpring;
                    js.targetPosition = targetPosition;
                    _wheelCollider.suspensionSpring = js;
                }
                if (overrideFriction)
                {
                    WheelFrictionCurve _forwardFric = _wheelCollider.forwardFriction;
                    _forwardFric.extremumSlip = forExtSlip;
                    _forwardFric.extremumValue = forExtValue;
                    _forwardFric.asymptoteSlip = forAsySlip;
                    _forwardFric.asymptoteValue = forAsyValue;
                    _forwardFric.stiffness = forStiffness;
                    _wheelCollider.forwardFriction = _forwardFric;

                    WheelFrictionCurve _sideFric = _wheelCollider.sidewaysFriction;
                    _sideFric.extremumSlip = forExtSlip;
                    _sideFric.extremumValue = forExtValue;
                    _sideFric.asymptoteSlip = forAsySlip;
                    _sideFric.asymptoteValue = forAsyValue;
                    _sideFric.stiffness = forStiffness;
                    _wheelCollider.sidewaysFriction = _sideFric;
                }
            }
            base.OnStart(state);
        }
    }
}
