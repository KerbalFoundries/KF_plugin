using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class ArmMagnet : PartModule
    {
        [KSPField]
        public string fixedName;
        [KSPField]
        public string armName;
        [KSPField]
        public int layerMask = 0;
        [KSPField(guiName = "Ray", guiActive = true)]
        public string ravInfo;

        GameObject _base;
        GameObject _arm;
		Rigidbody _rb;
		// Reports that it is never used.
		Rigidbody _targetRb;
		// Reports "Field 'KerbalFoundries.ArmMagnet._targetRb' is never assigned to, and will always have its default value null.
        ConfigurableJoint _joint;
        bool isReady;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight)
            {
                Debug.LogError(fixedName);
                _base = transform.Search(fixedName).gameObject;
                _arm = transform.Search(armName).gameObject;
                isReady = true;
                Debug.LogError("Test Arm started");
            }
        }

        public void FixedUpdate()
        {
            if (!isReady)
                return;
        }

        //[KSPEvent(guiActive = true, guiName = "Grab", active = true, guiActiveUnfocused = true, unfocusedRange = 40f)]
        public void CreateJoint()
        {
			if (!Equals(_arm, null) && !Equals(_base, null))
            {
                //_rb = _base.AddComponent<Rigidbody>();
                //_rb.isKinematic = true;
                //_rb.mass = 0.1f;
                //_rb.useGravity = false;
                //_rb.constraints = RigidbodyConstraints.FreezeAll;

                _joint = this.part.gameObject.AddComponent<ConfigurableJoint>();

                _joint.xMotion = ConfigurableJointMotion.Locked;
                _joint.yMotion = ConfigurableJointMotion.Locked;
                _joint.zMotion = ConfigurableJointMotion.Locked;

                _joint.angularXMotion = ConfigurableJointMotion.Free;
                _joint.angularYMotion = ConfigurableJointMotion.Free;
                _joint.angularZMotion = ConfigurableJointMotion.Free;

                _joint.connectedBody = _targetRb;
            }
            else
                Debug.LogError("GO not found ");
        }
    }
}

//Debug.Log("running Fixedupdate");
/*
Ray ray = new Ray(_base.transform.position, _base.transform.forward);
RaycastHit hit;

int tempLayerMask = ~layerMask;

if (Physics.Raycast(ray, out hit, 0.5f, tempLayerMask))
{
    ravInfo = hit.collider.gameObject.name.ToString();
    try
    {
        _arm = hit.collider.gameObject;
        _targetRb = (hit.rigidbody);
    }
    catch (NullReferenceException) { }
}
else
{
    ravInfo = "Nothing";
    _arm = null;
    //_rb = null;
}
 * */