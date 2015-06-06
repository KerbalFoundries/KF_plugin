using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFCouplingHitch : PartModule
    {
        bool isReady;
		bool sentOnRails;
        //[KSPField(isPersistant = true)]
        public bool isHitched;
        [KSPField]
        public string hitchObjectName;
        
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Coupling Force"), UI_FloatRange(minValue = 0, maxValue = 10f, stepIncrement = 1f)]
		const float forceMultiplier = 1;
        
        [KSPField]
        public string hitchLinkName;
        [KSPField]
        public float maxForce = 7.5f;
        [KSPField]
        public float xLimitHigh = 20;
        [KSPField]
        public float xLimitLow = 20;
        [KSPField]
        public float yLimit = 20;
        [KSPField]
        public float zLimit = 20; 
        [KSPField(guiActive = true, guiUnits = "deg", guiName = "Hitch Angle", guiFormat = "F0")]
        public Vector3 hitchRotation;
        [KSPField(guiActive = true, guiUnits = "deg", guiName = "Warp Rate", guiFormat = "F6")]
        public float warpRate;
        [KSPField(guiActive = true, guiUnits = "deg", guiName = "Warp index", guiFormat = "F0")]
        public int warpIndex;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Debug"), UI_Toggle(disabledText = "Enabled", enabledText = "Disabled")]
        public bool isDebug = false;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Joint Damper"), UI_FloatRange(minValue = 0, maxValue = 1f, stepIncrement = 0.1f)]
        public float jointDamper = 1;
        [KSPField(isPersistant = true)]
        public string _flightID;
        [KSPField(isPersistant = true)]
        public bool savedHitchState;
        public bool hitchCooloff;
        public bool inWarp = true;

        [KSPField(isPersistant = true)]
		public Vector3 _trailerPosition = new Vector3(0, 0, 0);

        GameObject _targetObject;
		[KSPField(isPersistant = true, guiActive = true, guiName = "Target Object Name")]
        public string _targetObjectName;
        [KSPField]
        public float moveSpeed = 1f;
        Rigidbody _rb;
        Part _targetPart;
        //KFCouplingEye _couplingEye;
        Vessel _targetVessel;
        
        GameObject _hitchObject;
        GameObject _coupledObject;
        GameObject _Link;
        GameObject _trailerFix;
        ConfigurableJoint _LinkJoint;
        ConfigurableJoint _HitchJoint;
        FixedJoint _StaticJoint;
        Rigidbody _rbLink;
        Vector3 _LinkRotation;
		Vector3 tempPosition;
		// Reports that it is never used.
        [KSPField]
        public int layerMask = 0;
        [KSPField(guiName = "Ray", guiActive = true)]
        public string ravInfo;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Distance"), UI_FloatRange(minValue = 0, maxValue = 5f, stepIncrement = 0.2f)]
        public float rayDistance = 1;

        //private LineRenderer lineX = null;

        IEnumerator HitchCooloffTimer()  
        {
            //print(Time.time);
            yield return new WaitForSeconds(10);
            print("Hitch Active");
            hitchCooloff = false;
        }

        public void VesselPack(Vessel vessel)
        {
			if (Equals(vessel, this.vessel))
            {
                sentOnRails = true;
				Debug.LogError(string.Format("hitch state is {0}", isHitched));
                //_targetPart.rigidbody.isKinematic = true;
                //_targetPart.transform.parent = this.part.transform;
            }
        }

        public void VesselUnPack(Vessel vessel)
        {
            Debug.LogWarning("FlightChecker started");
			if (Equals(vessel, this.vessel))
                StartCoroutine("WaitAndAttach");
        }

		IEnumerator WaitAndAttach() //Part partToAttach, Vector3 position, Quaternion rotation, Part toPart = null
        {

            Debug.Log("Wait for FixedUpdate");
            yield return new WaitForFixedUpdate();
			Debug.LogError(string.Format("saved hitch state{0}", savedHitchState));

			if (!Equals(_Link.GetComponent<Rigidbody>(), null))
                Debug.LogWarning("Link has rigidbody");
			if (Equals(_Link.GetComponent<Rigidbody>(), null))
                Debug.LogError("Link has no Rigidbosy");
            
			if (savedHitchState && Equals(_Link.GetComponent<Rigidbody>(), null))
            {
                Debug.LogWarning("Was previously hitched at last save");
				print(string.Format("Taget flightID {0}", _flightID));
				print(string.Format("Compare ID with flight ID {0}", uint.Parse(_flightID)));
                foreach (Part pa in FindObjectsOfType(typeof(Part)) as Part[])
                {
					print(string.Format("found part with flight ID {0}", pa.flightID));
                    
                    if (pa.flightID.Equals(uint.Parse(_flightID)))
                    {
                        //UnHitch(); //just in case, by some miracle, we're hitched already
                        _targetPart = pa;

                        Debug.LogWarning("Found part from persistence");
                        _targetObject = pa.transform.Search(_targetObjectName).gameObject;
                        _rb = pa.rigidbody;

                        Debug.LogWarning("Found hitchObject from persistence");
						if (Vector3.Distance(_targetObject.transform.position, _hitchObject.transform.position) > 0.1f)
                        {
                            // Empty!
                        }
                        _targetObject.transform.position = _hitchObject.transform.position;
                        Debug.LogWarning("Put objects in correct position");
                        //RayCast(0.3f);
                        pa.vessel.rigidbody.velocity = this.vessel.rigidbody.velocity;
                        Hitch();
                        Debug.LogError("Hitched");
                    }
                }
            }
			else // String is nullorempty 
                Debug.LogError("Not previously hitched or hitch already in place");
            isReady = true;
        }

        public void CreateStaticJoint(Part coupledPart)
        {
            UnityEngine.Object.Destroy(_LinkJoint);
            UnityEngine.Object.Destroy(_HitchJoint);
            UnityEngine.Object.Destroy(_rbLink);
            _StaticJoint = coupledPart.gameObject.AddComponent<FixedJoint>();
            _StaticJoint.breakForce = float.PositiveInfinity;
            _StaticJoint.breakTorque = float.PositiveInfinity;
            _StaticJoint.connectedBody = this.part.Rigidbody;
        }

        public void DestroyStaticJoint()
        {
            UnityEngine.Object.Destroy(_StaticJoint);
        }

        public void OnWarpChange()
        {
            warpRate = TimeWarp.CurrentRate;
            warpIndex = TimeWarp.CurrentRateIndex;
			if (!Equals(TimeWarp.CurrentRateIndex, 0))
            {/*
                print("warp rate changed and greater than one");
                //UnHitch();
                Part coupledPart = _coupledObject.GetComponentInParent<Part>();
                print("Refound target part");
                this.part.attachMethod = AttachNodeMethod.FIXED_JOINT;
                Vessel vessel = this.vessel;

                coupledPart.Couple(this.part);

                FlightGlobals.ForceSetActiveVessel(vessel);

                vessel.MakeActive(); 

                CreateStaticJoint(coupledPart);
                print("created static joint");
              */
                //_targetVessel.flightIntegrator.enabled = false;

                //FlightGlobals.overrideOrbit = true;

                if (isHitched)
                {
                    _trailerFix = new GameObject("_trailerFix");
                    _trailerFix.transform.position = _targetVessel.transform.position;
                    _trailerFix.transform.parent = this.part.transform;
                    FlightGlobals.overrideOrbit = true;
                    //_trailerPosition = _targetVessel.transform.position;
					//Debug.LogError("Relative position " + _trailerPosition);
                    DebugLine(_trailerFix.transform.position, _targetVessel.transform.right);
                }
                inWarp = true;
            }
            else if (Equals(TimeWarp.CurrentRateIndex, 0))
            {
                print("warp rate changed back to one");
                inWarp = false;
                FlightGlobals.overrideOrbit = false;
                //DestroyStaticJoint();
                //Hitch();
            }
        }

        //[KSPEvent(guiActive = true, guiName = "Hitch", active = true)]
        void Hitch()
        {
			if (!Equals(_targetObject, null))
            {
                isHitched = true;
                savedHitchState = true;
                Debug.LogWarning("Start of method...");
                _coupledObject = _targetObject;
                _targetObjectName = _targetObject.name.ToString();

                _targetPart = _targetObject.GetComponentInParent<Part>() as Part;
                //_couplingEye = _targetObject.GetComponentInParent<KFCouplingEye>() as KFCouplingEye;
                //_couplingEye.isHitched = true;
                //_couplingEye._hitchObject = _Link.transform;
                _flightID = _targetPart.flightID.ToString();
				print(string.Format("Target flight ID {0}", _flightID));
                //print(_targetPart.launchID);
                //print(_targetPart.name);

                _targetVessel = _targetPart.vessel;
                
				print(string.Format("Vessel ID {0}", _targetVessel.GetInstanceID()));

                Debug.LogWarning("Set up vessel and part stuff");

                //_hitchObject.transform.rotation = _couplingObject.transform.rotation;
                _rbLink = _Link.gameObject.AddComponent<Rigidbody>();
                _rbLink.mass = 0.01f;
                _rbLink.useGravity = false;
                _HitchJoint = this.part.gameObject.AddComponent<ConfigurableJoint>();
                _LinkJoint = _Link.gameObject.AddComponent<ConfigurableJoint>();

				#if Debug
                Debug.LogWarning("Created joint...");
				#endif
                _LinkJoint.xMotion = ConfigurableJointMotion.Locked;
                _LinkJoint.yMotion = ConfigurableJointMotion.Locked;
                _LinkJoint.zMotion = ConfigurableJointMotion.Locked;

                _HitchJoint.xMotion = ConfigurableJointMotion.Locked;
                _HitchJoint.yMotion = ConfigurableJointMotion.Locked;
                _HitchJoint.zMotion = ConfigurableJointMotion.Locked;

				#if Debug
                Debug.LogWarning("Configured linear...");
				#endif // Set up X limits
                SoftJointLimit sjl;
                sjl = _HitchJoint.highAngularXLimit;
                sjl.limit = xLimitHigh;
                _HitchJoint.highAngularXLimit = sjl;
                sjl = _HitchJoint.lowAngularXLimit;
                sjl.limit = -xLimitLow;
                _HitchJoint.lowAngularXLimit = sjl;

				// Set up Y limits
                sjl = _HitchJoint.angularYLimit;
                sjl.limit = yLimit;
                _HitchJoint.angularYLimit = sjl;
				// Set up Z limitssssssa
                sjl = _HitchJoint.angularZLimit;
                sjl.limit = zLimit;
                _HitchJoint.angularZLimit = sjl;

				#if Debug
                Debug.LogWarning("Configured linear...");
				#endif    

                _HitchJoint.angularXMotion = ConfigurableJointMotion.Limited;
                _HitchJoint.angularYMotion = ConfigurableJointMotion.Limited;
                _HitchJoint.angularZMotion = ConfigurableJointMotion.Limited;
                _HitchJoint.projectionMode = JointProjectionMode.PositionOnly;
                _HitchJoint.projectionDistance = 0.05f;

                _LinkJoint.angularXMotion = ConfigurableJointMotion.Locked;
                _LinkJoint.angularYMotion = ConfigurableJointMotion.Locked;
                _LinkJoint.angularZMotion = ConfigurableJointMotion.Locked;
                _LinkJoint.projectionMode = JointProjectionMode.PositionOnly;
                _LinkJoint.projectionDistance = 0.05f;

                SetJointDamper();

				#if Debug
                Debug.LogWarning("Configured joint...");
				#endif
                _HitchJoint.anchor = new Vector3(0, 0.4f, 0); //this seems to make a springy joint

                // Set correct axis
                //_HitchJoint.axis = new Vector3(1, 0, 0);
                //_HitchJoint.secondaryAxis = new Vector3(0, 0, 1);
				#if Debug
                Debug.LogWarning("Configured axis...");
                #endif
                _HitchJoint.connectedBody = _rbLink;

                _Link.transform.rotation = _coupledObject.transform.rotation;
				#if Debug
                Debug.LogWarning("Connected joint...");
				#endif
                
				print(string.Format("Target object is {0}", _coupledObject.name));
                _coupledObject.transform.position = _hitchObject.transform.position;
                _LinkJoint.connectedBody = _rb;
                print("Setting target parent");
                _coupledObject.transform.parent = this.part.transform;
            }
            else
                Debug.LogWarning("No target");
        }

        [KSPEvent(guiActive = true, guiName = "Update Damper", active = true, guiActiveUnfocused = true, unfocusedRange = 40f)]
        void SetJointDamper()
        {
			if (!Equals(_HitchJoint, null))
            {
                _HitchJoint.rotationDriveMode = RotationDriveMode.XYAndZ;
                JointDrive X = _HitchJoint.angularXDrive;
                X.mode = JointDriveMode.Position;
                X.positionDamper = jointDamper;
                _HitchJoint.angularXDrive = X;
                JointDrive YZ = _HitchJoint.angularYZDrive;
                YZ.mode = JointDriveMode.Position;
                YZ.positionDamper = jointDamper;
                _HitchJoint.angularYZDrive = YZ;
            }
            else
                Debug.LogError("No joint to update!");
            }

        [KSPEvent(guiActive = true, guiName = "Un-Hitch", active = true, guiActiveUnfocused = true, unfocusedRange = 40f)]
        void UnHitch()
        {
			if (!Equals(_LinkJoint, null))
            {
                Debug.LogWarning("Unhitching...");
                //_joint.connectedBody = this.part.rigidbody;
				UnityEngine.Object.Destroy(_LinkJoint);
				UnityEngine.Object.Destroy(_rbLink);
				UnityEngine.Object.Destroy(_HitchJoint);
                _Link.transform.localEulerAngles = _LinkRotation;
                isHitched = false;
                //_couplingEye.isHitched = false;
                savedHitchState = false;
                hitchCooloff = true;
                _coupledObject = null;
                _flightID = string.Empty;
                StartCoroutine("HitchCooloffTimer");
            }
            else
                Debug.LogWarning("Not hitched!!!!");
        }

        //[KSPEvent(guiActive = true, guiName = "Show rotation", active = true)]
        public static Vector3 JointRotation(Transform hitch, Transform coupling, bool signed)
        {
            //this projects vectors onto chosen 2D planes. planes are defined by their normals, in this case hitchObject.transform.forward.
            Vector3 hitchProjectX = hitch.transform.right - (hitch.transform.forward) * Vector3.Dot(hitch.transform.right, hitch.transform.forward);
            Vector3 attachProjectX = coupling.transform.right - (hitch.transform.forward) * Vector3.Dot(coupling.transform.right, hitch.transform.forward);

            Vector3 hitchProjectY = hitch.transform.up - (hitch.transform.right) * Vector3.Dot(hitch.transform.up, hitch.transform.right);
            Vector3 attachProjectY = coupling.transform.up - (hitch.transform.right) * Vector3.Dot(coupling.transform.up, hitch.transform.right);

            Vector3 hitchProjectZ = hitch.transform.forward - (hitch.transform.up) * Vector3.Dot(hitch.transform.forward, hitch.transform.up);
            Vector3 attachProjectZ = coupling.transform.forward - (hitch.transform.up) * Vector3.Dot(coupling.transform.forward, hitch.transform.up);

            float angleX = Mathf.Acos(Vector3.Dot(hitchProjectX, attachProjectX) / Mathf.Sqrt(Mathf.Pow(hitchProjectX.magnitude, 2) * Mathf.Pow(attachProjectX.magnitude, 2))) * Mathf.Rad2Deg;
            float angleY = Mathf.Acos(Vector3.Dot(hitchProjectY, attachProjectY) / Mathf.Sqrt(Mathf.Pow(hitchProjectY.magnitude, 2) * Mathf.Pow(attachProjectY.magnitude, 2))) * Mathf.Rad2Deg;
            float angleZ = Mathf.Acos(Vector3.Dot(hitchProjectZ, attachProjectZ) / Mathf.Sqrt(Mathf.Pow(hitchProjectZ.magnitude, 2) * Mathf.Pow(attachProjectZ.magnitude, 2))) * Mathf.Rad2Deg;

            if (signed)
            {
                Vector3 normalvectorX = Vector3.Cross(hitchProjectX, attachProjectX);
                if (normalvectorX[2] < 0.0f)
                    angleX *= -1;
                Vector3 normalvectorY = Vector3.Cross(hitchProjectY, attachProjectY);
                if (normalvectorY[1] < 0.0f)
                    angleY *= -1;
                Vector3 normalvectorZ = Vector3.Cross(hitchProjectZ, attachProjectZ);
                if (normalvectorZ[0] < 0.0f)
                    angleZ *= -1;
            }

			#if Debug
            if (isDebug)
            {
				print(string.Format("normalVector{0}", normalvectorX));
				print(string.Format("normalVector{0}", normalvectorY));
				print(string.Format("normalVector{0}", normalvectorZ));
				Debug.LogWarning(string.Format("Rotate {0}", hitchRotation));
            }
			#endif
            var couplingRotation = new Vector3(angleX, angleY, angleZ);

            return couplingRotation;
        }

        void OnDestroy()
        {
            Debug.LogError("OnDestroy");
            GameEvents.onVesselGoOffRails.Remove(VesselUnPack);
            GameEvents.onVesselGoOnRails.Remove(VesselPack);
            Debug.LogError("Hitch State destroy" + isHitched);
            //savedHitchState = isHitched;

            //Debug.LogError("Vessel Pack");
            if (isHitched)
            {
				// Do absolutely nothing, and do it right!
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            GameEvents.onVesselGoOffRails.Add(VesselUnPack);
            GameEvents.onVesselGoOnRails.Add(VesselPack);

            GameEvents.onTimeWarpRateChanged.Add(OnWarpChange);
            
            _hitchObject = transform.Search(hitchObjectName).gameObject;
            _Link = transform.Search(hitchLinkName).gameObject;
            _LinkRotation = _Link.transform.localEulerAngles;

            if (HighLogic.LoadedSceneIsFlight)
            {
                // First of all, create a GameObject to which LineRenderer will be attached.
                //GameObject obj = _hitchObject.gameObject;
                // Then create renderer itself...
                //DebugLine(_hitchObject.gameObject, _hitchObject.transform.forward, Color.black);
                //StartCoroutine("FlightChecker");
            }
        }

        void DebugLine(Vector3 position, Vector3 rotation)
        {
            var lineDebugX = new GameObject("lineDebug");
            var lineDebugY = new GameObject("lineDebug");
            var lineDebugZ = new GameObject("lineDebug");
            lineDebugX.transform.position = position;
            lineDebugY.transform.position = position;
            lineDebugZ.transform.position = position;
            LineRenderer lineX = lineDebugX.AddComponent<LineRenderer>();
            LineRenderer lineY = lineDebugY.AddComponent<LineRenderer>();
            LineRenderer lineZ = lineDebugZ.AddComponent<LineRenderer>();
            //lineX.transform.parent = transform; // ...child to our part...
            lineX.useWorldSpace = false; // ...and moving along with it (rather 
            lineX.material = new Material(Shader.Find("Particles/Additive"));
            lineX.SetColors(Color.red, Color.white);
            lineX.SetWidth(0.1f, 0.1f);
            lineX.SetVertexCount(2);
            lineX.SetPosition(0, Vector3.zero);
            lineX.SetPosition(1, Vector3.right * 10);

            lineY.useWorldSpace = false; // ...and moving along with it (rather 
            lineY.material = new Material(Shader.Find("Particles/Additive"));
            lineY.SetColors(Color.green, Color.white);
            lineY.SetWidth(0.1f, 0.1f);
            lineY.SetVertexCount(2);
            lineY.SetPosition(0, Vector3.zero);
            lineY.SetPosition(1, Vector3.up * 10);

            lineZ.useWorldSpace = false; // ...and moving along with it (rather 
            lineZ.material = new Material(Shader.Find("Particles/Additive"));
            lineZ.SetColors(Color.blue, Color.white);
            lineZ.SetWidth(0.1f, 0.1f);
            lineZ.SetVertexCount(2);
            lineZ.SetPosition(0, Vector3.zero);
            lineZ.SetPosition(1, Vector3.forward * 10);
        }
        // than staying in fixed world coordinates)
        //line.transform.localPosition = Vector3.zero;
        //line.transform.localEulerAngles = Vector3.zero;
        // Make it render a red to yellow triangle, 1 meter wide and 2 meters long

        public void FixedUpdate()
        {
            if (!isReady || hitchCooloff)
                return;
            RayCast(rayDistance);
            bool brakesOn = FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.Brakes];

			if (isHitched)
            {
                _targetVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, brakesOn);
                _trailerPosition = _targetVessel.transform.position;
                if (inWarp)
                    _targetVessel.transform.position = _trailerFix.transform.position;
                }
                
            if (!Equals(_targetObject, null) && !isHitched)
            {
                if (_targetObject.name.Equals("EyeTarget", StringComparison.Ordinal) || _targetObject.name.Equals("EyePoint", StringComparison.Ordinal))
                {
                    Vector3 forceVector = -(_targetObject.transform.position - _hitchObject.transform.position).normalized;
                    Vector3 forcePlane = forceVector - (_hitchObject.transform.forward) * Vector3.Dot(forceVector, _hitchObject.transform.forward);
                    Vector3 force = forcePlane * forceMultiplier * Mathf.Clamp((1 / (_targetObject.transform.position - _hitchObject.transform.position).magnitude), -maxForce, maxForce); //(1 / (_targetObject.transform.position - _hitchObject.transform.position).magnitude) *
                    _rb.rigidbody.AddForceAtPosition(force, _targetObject.transform.position);
                    this.part.rigidbody.AddForceAtPosition(-force, _hitchObject.transform.position);
                }

                if (_targetObject.name.Equals("EyePoint", StringComparison.Ordinal))
                {
                    if (Vector3.Distance(_targetObject.transform.position, _hitchObject.transform.position) < 0.1f)
                    {
                        Vector3 jointLimitCheck = JointRotation(_hitchObject.transform, _targetObject.transform, false);
                        bool rotationCorrect = false;
                        if (jointLimitCheck[0] < xLimitHigh && jointLimitCheck[1] < yLimit && jointLimitCheck[2] < zLimit)
                            rotationCorrect = true;
                        else
                            Debug.Log("Joint outside rotation limit");

                        if (rotationCorrect)
                        {
                            Debug.Log("Rotation within limits, hitching");
                            Hitch();
                        }
                    }
                }
            }
            //print(_joint.
        }

        //[KSPEvent(guiActive = true, guiName = "Fire Ray", active = true)]
        void RayCast(float rayLength)
        {
            var ray = new Ray(_hitchObject.transform.position, _hitchObject.transform.forward);
            RaycastHit hit;
            int tempLayerMask = ~layerMask;
            //Debug.DrawRay(_hitchObject.transform.position, _hitchObject.transform.up);
            //lineX.transform.rotation = _hitchObject.transform.rotation;
            //lineX.transform.position = _hitchObject.transform.position;
            if (Physics.Raycast(ray, out hit, rayLength, tempLayerMask))
            {
                //targetObject = hit.collider.gameObject;
                ravInfo = hit.collider.gameObject.name.ToString();
                try
                {
                    _targetPart = Part.FromGO(hit.rigidbody.gameObject);
                    _rb = (hit.rigidbody);
                    //if(p.vessel != this.vessel)
                    //{
                    _targetObject = hit.collider.gameObject;
                    //print("Hit " + hit.collider.gameObject.name);
                    //}
                }
				catch (NullReferenceException)
				{
				}
            }
            else
            {
                ravInfo = "Nothing";
                _targetObject = null;
            }
        }
    }
}
