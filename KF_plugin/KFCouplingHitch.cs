using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	public class KFCouplingHitch : PartModule
	{
		bool isReady;
		
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
		private bool beginWarp = true;

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
		Vector3 trailerOffset = Vector3.zero;
        
		GameObject _hitchObject;
		GameObject _coupledObject;
		GameObject _Link;
        
		ConfigurableJoint _LinkJoint;
		ConfigurableJoint _HitchJoint;
        
		Rigidbody _rbLink;
		Vector3 _LinkRotation;
		
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
			if (Equals(vessel, this.vessel))
			{
                
				KFLog.Error(string.Format("Hitch state is {0}", isHitched));
				//_targetPart.rigidbody.isKinematic = true;
				//_targetPart.transform.parent = this.part.transform;
			}
		}

		public void VesselUnPack(Vessel vessel)
		{
			KFLog.Warning("FlightChecker started.");
			if (Equals(vessel, this.vessel))
				StartCoroutine("WaitAndAttach");
		}

		IEnumerator WaitAndAttach() //Part partToAttach, Vector3 position, Quaternion rotation, Part toPart = null
		{

			KFLog.Log("Wait for FixedUpdate...");
			yield return new WaitForFixedUpdate();
			KFLog.Error(string.Format("Saved hitch state: {0}", savedHitchState));

			if (!Equals(_Link.GetComponent<Rigidbody>(), null))
				KFLog.Warning("Link has rigidbody.");
			if (Equals(_Link.GetComponent<Rigidbody>(), null))
				KFLog.Error("Link has no Rigidbody.");
            
			if (savedHitchState && Equals(_Link.GetComponent<Rigidbody>(), null))
			{
				KFLog.Warning("Was previously hitched at last save.");
				print(string.Format("Taget flightID {0}", _flightID));
				KFLog.Log(string.Format("Compare ID with flight ID {0}", uint.Parse(_flightID)));
				foreach (Part pa in FindObjectsOfType(typeof(Part)) as Part[])
				{
					KFLog.Log(string.Format("found part with flight ID {0}", pa.flightID));
                    
					if (pa.flightID.Equals(uint.Parse(_flightID)))
					{
						//UnHitch(); //just in case, by some miracle, we're hitched already
						_targetPart = pa;

						KFLog.Warning("Found part from persistence.");
						_targetObject = pa.transform.Search(_targetObjectName).gameObject;
						_rb = pa.rigidbody;

						KFLog.Warning("Found hitchObject from persistence.");
						if (Vector3.Distance(_targetObject.transform.position, _hitchObject.transform.position) > 0.1f)
						{
							// Empty!
						}
						_targetObject.transform.position = _hitchObject.transform.position;
						KFLog.Warning("Put objects in correct position.");
						//RayCast(0.3f);
						pa.vessel.rigidbody.velocity = this.vessel.rigidbody.velocity;
						Hitch();
						KFLog.Error("Hitched!.");
					}
				}
			}
			else // String is nullorempty 
               KFLog.Error("Not previously hitched or hitch already in place..");
			isReady = true;
		}

      
		//[KSPEvent(guiActive = true, guiName = "Hitch", active = true)]
		void Hitch()
		{
			if (!Equals(_targetObject, null))
			{
				isHitched = true;
				savedHitchState = true;
				KFLog.Warning("Start of method....");
				_coupledObject = _targetObject;
				_targetObjectName = _targetObject.name.ToString();

				_targetPart = _targetObject.GetComponentInParent<Part>() as Part;
				//_couplingEye = _targetObject.GetComponentInParent<KFCouplingEye>() as KFCouplingEye;
				//_couplingEye.isHitched = true;
				//_couplingEye._hitchObject = _Link.transform;
				_flightID = _targetPart.flightID.ToString();
				KFLog.Log(string.Format("Target flight ID {0}", _flightID));
				//print(_targetPart.launchID);
				//print(_targetPart.name);

				_targetVessel = _targetPart.vessel;
                
				KFLog.Log(string.Format("Vessel ID {0}", _targetVessel.GetInstanceID()));

				KFLog.Warning("Set up vessel and part stuff.");

				//_hitchObject.transform.rotation = _couplingObject.transform.rotation;
				_rbLink = _Link.gameObject.AddComponent<Rigidbody>();
				_rbLink.mass = 0.01f;
				_rbLink.useGravity = false;
				_HitchJoint = this.part.gameObject.AddComponent<ConfigurableJoint>();
				_LinkJoint = _Link.gameObject.AddComponent<ConfigurableJoint>();

				#if Debug
				KFLog.Warning("Created joint...");
				#endif
				_LinkJoint.xMotion = ConfigurableJointMotion.Locked;
				_LinkJoint.yMotion = ConfigurableJointMotion.Locked;
				_LinkJoint.zMotion = ConfigurableJointMotion.Locked;

				_HitchJoint.xMotion = ConfigurableJointMotion.Locked;
				_HitchJoint.yMotion = ConfigurableJointMotion.Locked;
				_HitchJoint.zMotion = ConfigurableJointMotion.Locked;

				#if Debug
				KFLog.Warning("Configured linear...");
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
                KFLog.Warning("Configured linear...");
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
                KFLog.Warning("Configured joint...");
				#endif
				_HitchJoint.anchor = new Vector3(0, 0.4f, 0); //this seems to make a springy joint

				// Set correct axis
				//_HitchJoint.axis = new Vector3(1, 0, 0);
				//_HitchJoint.secondaryAxis = new Vector3(0, 0, 1);
				#if Debug
                KFLog.Warning("Configured axis...");
				#endif
				_HitchJoint.connectedBody = _rbLink;

				_Link.transform.rotation = _coupledObject.transform.rotation;
				#if Debug
                KFLog.Warning("Connected joint...");
				#endif
                
				KFLog.Log(string.Format("Target object is {0}.", _coupledObject.name));
				_coupledObject.transform.position = _hitchObject.transform.position;
				_LinkJoint.connectedBody = _rb;
				KFLog.Log("Setting target parent...");
				_coupledObject.transform.parent = this.part.transform;
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
				UnityEngine.Object.Destroy(_LinkJoint);
				UnityEngine.Object.Destroy(_rbLink);
				UnityEngine.Object.Destroy(_HitchJoint);
				_Link.transform.localEulerAngles = _LinkRotation;
				isHitched = false;
				//_couplingEye.isHitched = false;
				savedHitchState = false;
				hitchCooloff = true;
				_coupledObject = null;
				_targetVessel = null;
				_targetPart = null;
				_flightID = string.Empty;
				StartCoroutine("HitchCooloffTimer");
			}
			else
				KFLog.Warning("Not hitched!");
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
				KFLog.Error("No joint to update!");
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

			GameEvents.onVesselGoOffRails.Add(VesselUnPack);
			GameEvents.onVesselGoOnRails.Add(VesselPack);

			//GameEvents.onTimeWarpRateChanged.Add(OnWarpChange);
            
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


		// than staying in fixed world coordinates)
		//line.transform.localPosition = Vector3.zero;
		//line.transform.localEulerAngles = Vector3.zero;
		// Make it render a red to yellow triangle, 1 meter wide and 2 meters long

		public void Update()
		{
			if (isHitched)
			{
				WarpMover();
			}
		}


		public void FixedUpdate()
		{
			RaycastHit hitUpdate;
			if (!isReady || hitchCooloff)
				return;
            
			bool brakesOn = FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.Brakes];

			if (isHitched)
			{
				_targetVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, brakesOn);
				return;
			}
			else
			{
				hitUpdate = RayCast(rayDistance);
			}
                
			if (!Equals(_targetObject, null) && !isHitched)
			{

				if (_targetObject.name.Equals("EyeTarget", StringComparison.Ordinal) || _targetObject.name.Equals("EyePoint", StringComparison.Ordinal))
				{
					_targetPart = Part.FromGO(hitUpdate.rigidbody.gameObject);
					_rb = (hitUpdate.rigidbody);
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
		}

		//[KSPEvent(guiActive = true, guiName = "Fire Ray", active = true)]
		RaycastHit RayCast(float rayLength)
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
				//try
				//{
                    
				//if(p.vessel != this.vessel)
				//{
				_targetObject = hit.collider.gameObject;
				//print("Hit " + hit.collider.gameObject.name);
				//}
				//}
				//catch (NullReferenceException)
				//{
				//}
			}
			else
			{
				ravInfo = "Nothing";
				_targetObject = null;
			}
			return hit;
		}
	}
}
