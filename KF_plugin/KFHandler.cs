using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFHandler : PartModule
	{
		bool isReady;
		
		//[KSPField(isPersistant = true)]
		public bool isHitched;
		[KSPField]
		public string rayObjectName;
        
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Coupling Force"), UI_FloatRange(minValue = 0, maxValue = 10f, stepIncrement = 1f)]
		const float forceMultiplier = 1;
        
		[KSPField]
		public string effectorName;

        [KSPField]
        public float moveSpeed = 0.02f;
        [KSPField]
        public float rotateSpeed = 0.75f;

        [KSPField]
		public string mountName;
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
		private bool beginWarp = true;
        bool grabActive;

		[KSPField(isPersistant = true)]
		public Vector3 _trailerPosition = new Vector3(0, 0, 0);

		GameObject _targetObject;
		[KSPField(isPersistant = true, guiActive = true, guiName = "Target Object Name")]
		public string _targetObjectName;
		//[KSPField]
		//public float moveSpeed = 1f;
		Part _targetPart;
        Vessel _targetVessel;

		Vector3 trailerOffset = Vector3.zero;

        GameObject _rayObject;

		GameObject _effector;

        Vector3 jointRotation = new Vector3(0, 0, 0);
        Vector3 jointPosition = new Vector3(0, 0, 0);


       
		ConfigurableJoint _LinkJoint;
		ConfigurableJoint _HitchJoint;


		Vector3 _moverInitialRotation;
		
		// Reports that it is never used.
		[KSPField]
		public int layerMask = 0;
		[KSPField(guiName = "Ray", guiActive = true)]
		public string ravInfo;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Distance"), UI_FloatRange(minValue = 0, maxValue = 5f, stepIncrement = 0.2f)]
		public float rayDistance = 1;

		//private LineRenderer lineX = null;

		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFCouplingHitch");
		
		IEnumerator HitchCooloffTimer()
		{
			//print(Time.time);
			yield return new WaitForSeconds(10);
			KFLog.Log("Hitch Active.");
			hitchCooloff = false;
		}

		void WarpMover()
		{
			if (TimeWarp.CurrentRate > 1 && TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
			{
				KFLog.Warning("Time rate greater than one and warpmode is high.");
				if (beginWarp)
				{
					KFLog.Warning("BeginWarp is true");
					beginWarp = false;

					trailerOffset = _targetVessel.transform.position - this.vessel.transform.position;
					KFLog.Log(string.Format("\"trailerOffset\" = {0}", trailerOffset));
				}

				_targetVessel.SetPosition(this.vessel.transform.position + trailerOffset);
				_targetVessel.obt_velocity = this.vessel.obt_velocity;

			}
			else
			{
				if (!beginWarp)
				{
					KFLog.Error("BeginWarp is false.");

					Vector3d newPosition = this.vessel.transform.position + trailerOffset;

					_targetVessel.SetPosition(this.vessel.transform.position + trailerOffset);
					_targetVessel.obt_velocity = this.vessel.obt_velocity;
					_targetVessel.SetWorldVelocity(this.vessel.obt_velocity);

					_targetVessel.orbit.UpdateFromStateVectors(newPosition.xzy, vessel.obt_velocity.xzy, vessel.mainBody, Planetarium.GetUniversalTime());

				}
				beginWarp = true;
			}
		}

		public void VesselPack(Vessel vessel)
		{
            KFLog.Warning("Vessel Packed");
			if (Equals(vessel, this.vessel))
			{
                
				KFLog.Error(string.Format("Hitch state is {0}", isHitched));
			}
		}

		public void VesselUnPack(Vessel vessel)
		{
            KFLog.Warning("Vessel Unpacked");
			if (Equals(vessel, this.vessel))
				StartCoroutine("WaitAndAttach");
		}

		IEnumerator WaitAndAttach() //Part partToAttach, Vector3 position, Quaternion rotation, Part toPart = null
		{

			KFLog.Log("Wait for FixedUpdate...");
			yield return new WaitForFixedUpdate();
			KFLog.Error(string.Format("Saved hitch state: {0}", savedHitchState));

			isReady = true;
		}

        IEnumerator GrabTimer() //Part partToAttach, Vector3 position, Quaternion rotation, Part toPart = null
        {

            KFLog.Log("Magnets On");
            grabActive = true;
            yield return new WaitForSeconds(2);
            grabActive = false;
            KFLog.Log("Magnets Off");
        }

      
		//[KSPEvent(guiActive = true, guiName = "Hitch", active = true)]
		void Hitch(Part target, GameObject effector)
		{
			if (!Equals(_targetPart, null))
			{
				isHitched = true;
				savedHitchState = true;
				KFLog.Warning("Start of Hitch method....");

				_HitchJoint = effector.AddComponent<ConfigurableJoint>();

				#if Debug
				KFLog.Warning("Created joint...");
				#endif

				_HitchJoint.xMotion = ConfigurableJointMotion.Locked;
				_HitchJoint.yMotion = ConfigurableJointMotion.Locked;
				_HitchJoint.zMotion = ConfigurableJointMotion.Locked;

				_HitchJoint.angularXMotion = ConfigurableJointMotion.Locked;
				_HitchJoint.angularYMotion = ConfigurableJointMotion.Locked;
				_HitchJoint.angularZMotion = ConfigurableJointMotion.Locked;
				_HitchJoint.projectionMode = JointProjectionMode.PositionAndRotation;
				_HitchJoint.projectionDistance = 0.05f;
                _HitchJoint.projectionAngle = 0.1f;

				#if Debug
                KFLog.Warning("Configured joint...");
				#endif
				
				#if Debug
                KFLog.Warning("Configured axis...");
				#endif
				_HitchJoint.connectedBody = target.rigidbody;

				#if Debug
                KFLog.Warning("Connected joint...");
				#endif

			}
			else
				KFLog.Warning("No target.");
		}

		[KSPEvent(guiActive = true, guiName = "Un-Hitch", active = true, guiActiveUnfocused = true, unfocusedRange = 40f)]
		void UnHitch()
		{
			if (isHitched)
			{
				KFLog.Warning("Unhitching...");
				//_joint.connectedBody = this.part.rigidbody;
				UnityEngine.Object.Destroy(_HitchJoint);
				isHitched = false;
				savedHitchState = false;

				_targetPart = null;
			}
			else
				KFLog.Warning("Not hitched!");
		}



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
				KFLog.Log(string.Format("normalVector{0}", normalvectorX));
				KFLog.Log(string.Format("normalVector{0}", normalvectorY));
				KFLog.Log(string.Format("normalVector{0}", normalvectorZ));
				KFLog.Warning(string.Format("Rotate {0}", hitchRotation));
            }
			#endif
			var couplingRotation = new Vector3(angleX, angleY, angleZ);

			return couplingRotation;
		}

		void OnDestroy()
		{
			KFLog.Error("OnDestroy");
			GameEvents.onVesselGoOffRails.Remove(VesselUnPack);
			GameEvents.onVesselGoOnRails.Remove(VesselPack);
			KFLog.Error(string.Format("Hitch State destroy: {0}", isHitched));
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

			if (HighLogic.LoadedSceneIsFlight)
			{

                GameEvents.onVesselGoOffRails.Add(VesselUnPack);
                GameEvents.onVesselGoOnRails.Add(VesselPack);

                //this is for the ray to raycast from
                _rayObject = transform.Search(rayObjectName).gameObject;
                //this is for the movable object with the magnets
                _effector = transform.Search(effectorName).gameObject;
                //this is the object in the model to which it's attached.
                _moverInitialRotation = _effector.transform.localEulerAngles;

                Rigidbody rb = _effector.AddComponent<Rigidbody>();
                rb.mass = 0.01f;
                rb.useGravity = false;

                _LinkJoint = SetupConfigurableJoint(rb, 100000, 1000, 100000, true);
                //_LinkJoint = _mount.GetComponent<ConfigurableJoint>();
                
			}
		}

		public void Update()
		{
            
            if (!isReady)
                return;



            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.GetKey(KeyCode.Keypad4) && jointRotation.y < 20)
                    jointRotation.y += rotateSpeed;
                if (Input.GetKey(KeyCode.Keypad6) && jointRotation.y > -20)
                    jointRotation.y -= rotateSpeed;
                if (Input.GetKey(KeyCode.Keypad8) && jointRotation.x < 30)
                    jointRotation.x += rotateSpeed;
                if (Input.GetKey(KeyCode.Keypad2) && jointRotation.x > -30)
                    jointRotation.x -= rotateSpeed;
                if (Input.GetKey(KeyCode.Keypad1) && jointRotation.z < 20)
                    jointRotation.z += rotateSpeed;
                if (Input.GetKey(KeyCode.Keypad3) && jointRotation.z > -20)
                    jointRotation.z -= rotateSpeed;
            }
            else
            {
                if (Input.GetKey(KeyCode.Keypad4) && jointPosition.x < 0.7f)
                    jointPosition.x += moveSpeed;
                if (Input.GetKey(KeyCode.Keypad6) && jointPosition.x > -0.7f)
                    jointPosition.x -= moveSpeed;
                if (Input.GetKey(KeyCode.Keypad8) && jointPosition.y < 1.0f)
                    jointPosition.y += moveSpeed;
                if (Input.GetKey(KeyCode.Keypad2) && jointPosition.y > -0.5f)
                    jointPosition.y -= moveSpeed;
                if (Input.GetKey(KeyCode.Keypad9) && jointPosition.z < 0.5f)
                    jointPosition.z += moveSpeed;
                if (Input.GetKey(KeyCode.Keypad3) && jointPosition.z > -0.5f)
                    jointPosition.z -= moveSpeed;

            }
            if (Input.GetKeyUp(KeyCode.Keypad0))
            {
                StopCoroutine("GrabTimer");
                StartCoroutine("GrabTimer");
            }

		}


		public void FixedUpdate()
		{
            if (!isReady)
                return;

            _LinkJoint.targetPosition = jointPosition;
            _LinkJoint.targetRotation = Quaternion.Euler(jointRotation);

            if (grabActive)
            {
                RaycastHit hit;
                if (RayCastNew(0.2f, _rayObject, _rayObject.transform.forward, out hit))
                {
                    _targetPart = hit.collider.gameObject.GetComponentInParent<Part>() as Part;
                    if (!Equals(_targetPart, null))
                    {
                        KFLog.Warning("Grabbing Part " + _targetPart.partInfo.name);
                        grabActive = false; //stops us running through and trying to reattach

                        Hitch(_targetPart, _effector);
                    }
                }
            }
		}

        void SetupMoverJoint()
        {

        }

        ConfigurableJoint SetupConfigurableJoint(Rigidbody connectedBody, float spring, float damper, float force, bool swap)
        {
            KFLog.Warning("Setting Up ConfigurableJoint");

            ConfigurableJoint joint = this.part.gameObject.AddComponent<ConfigurableJoint>();
            Debug.LogWarning(this.part.gameObject.transform.InverseTransformPoint(connectedBody.transform.position));
            joint.anchor = this.part.gameObject.transform.InverseTransformPoint(connectedBody.transform.position);

#if Debug
				KFLog.Warning("Created joint...");
#endif

            SoftJointLimit sjl;
            // Set up X angular limits
            sjl = joint.highAngularXLimit;
            sjl.limit = xLimitHigh;
            joint.highAngularXLimit = sjl;
            sjl = joint.lowAngularXLimit;
            sjl.limit = -xLimitLow;
            joint.lowAngularXLimit = sjl;
            // Set up angular Y limits
            sjl = joint.angularYLimit;
            sjl.limit = yLimit;
            joint.angularYLimit = sjl;
            // Set up angular Z limits
            sjl = joint.angularZLimit;
            sjl.limit = zLimit;
            joint.angularZLimit = sjl;
            // Set up linear limits
            sjl = joint.linearLimit;
            sjl.limit = 1;
            sjl.bounciness = force;
            sjl.spring = spring;
            sjl.damper = damper;
            joint.linearLimit = sjl;

#if Debug
                KFLog.Warning("Configuring limits...");
#endif

            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.05f;
            joint.projectionAngle = 0.1f;

            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;


#if Debug
                KFLog.Warning("Configurng linear drives ...");
#endif
            JointDrive jd;
            jd = joint.xDrive;
            jd.mode = JointDriveMode.Position;
            jd.positionSpring = spring;
            jd.positionDamper = damper;
            jd.maximumForce = force;
            joint.xDrive = jd;

            jd = joint.yDrive;
            jd.mode = JointDriveMode.Position;
            jd.positionSpring = spring;
            jd.positionDamper = damper;
            jd.maximumForce = force;
            joint.yDrive = jd;

            jd = joint.zDrive;
            jd.mode = JointDriveMode.Position;
            jd.positionSpring = spring;
            jd.positionDamper = damper;
            jd.maximumForce = force;
            joint.zDrive = jd;
#if Debug
                KFLog.Warning("Configuring rotational drives...");
#endif
            jd = joint.angularXDrive;
            jd.mode = JointDriveMode.Position;
            jd.positionSpring = spring;
            jd.positionDamper = damper;
            jd.maximumForce = force;
            joint.angularXDrive = jd;

            jd = joint.angularYZDrive;
            jd.mode = JointDriveMode.Position;
            jd.positionSpring = spring;
            jd.positionDamper = damper;
            jd.maximumForce = force;
            joint.angularYZDrive = jd;

            joint.rotationDriveMode = RotationDriveMode.XYAndZ;

#if Debug
                KFLog.Warning("Connected joint...");
#endif
            joint.connectedBody = connectedBody;
            //joint.swapBodies = swap;
            

            return joint;
        }

        bool RayCastNew(float rayLength, GameObject rayObject, Vector3 direction, out RaycastHit hit)
        {
            var ray = new Ray(rayObject.transform.position, direction);
            int tempLayerMask = ~layerMask;
            return Physics.Raycast(ray, out hit, rayLength, tempLayerMask);
        }

		//[KSPEvent(guiActive = true, guiName = "Fire Ray", active = true)]
		
	}
}

/*
           RaycastHit hit;
           if (!isReady || hitchCooloff)
               return;
            
           bool brakesOn = FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.Brakes];

           if (isHitched)
           {
               return;
           }
           else
           {
               if (RayCastNew(rayDistance, _rayObject, _rayObject.transform.right, out hit))
               {
                   _targetObject = hit.collider.gameObject;
                   _rbGrabbedObject = hit.rigidbody;
                   print(hit.collider.name);
               }
           }
                
           if (!Equals(_targetObject, null) && !isHitched)
           {

               if (_targetObject.name.Equals("EyeTarget", StringComparison.Ordinal) || _targetObject.name.Equals("EyePoint", StringComparison.Ordinal))
               {
                   _targetPart = Part.FromGO(hit.rigidbody.gameObject);
                   _rb = (hit.rigidbody);
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
                           KFLog.Log("Joint outside rotation limit.");

                       if (rotationCorrect)
                       {
                           KFLog.Log("Rotation within limits, hitching.");
                           Hitch();
                       }
                   }
               }
           }
           //print(_joint.
            * */