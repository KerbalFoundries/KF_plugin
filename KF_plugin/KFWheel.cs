using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>A replacement for the stock "Wheel" module geared towards KF-related wheels.</summary>
	public class KFWheel : PartModule
	{
		[KSPField(isPersistant = false, guiActive = false, guiName = "Suspension travel")]
		public float susTravel;
		
		// Config fields
		[KSPField]
		public string wheelName;
		
		[KSPField]
		public string colliderName;
		
		[KSPField]
		public string sustravName;
		
		[KSPField]
		public string steeringName;
		
		[KSPField]
		public bool useDirectionCorrector;
		
		[KSPField]
		public bool isSprocket;
		
		[KSPField]
		public bool hasSuspension = true;
		
		[KSPField]
		public float rotationCorrection = 1f;
		
		[KSPField]
		public bool trackedWheel = true;
		
		/// <summary>Wheel rotation X axis.</summary>
		[KSPField]
		public float wheelRotationX = 1f;
		
		/// <summary>Wheel rotation Y axis.</summary>
		[KSPField]
		public float wheelRotationY;
		
		/// <summary>Wheel rotation Z axis.</summary>
		[KSPField]
		public float wheelRotationZ;
		
		/// <summary>Suspension traverse axis.</summary>
		[KSPField]
		public string susTravAxis = "Y";
		
		/// <summary>Steering axis.</summary>
		[KSPField]
		public string steeringAxis = "Y";
		
		[KSPField(isPersistant = true)]
		public float lastFrameTraverse;
		
		// Persistent fields. Not to be used for config
		[KSPField(isPersistant = true)]
		public float fSuspensionDistance;
		
		[KSPField(isPersistant = true)]
		public float fSuspensionSpring;
		
		[KSPField(isPersistant = true)]
		public float fSuspensionDamper;
		
		[KSPField(isPersistant = true)]
		public bool isConfigured;
		
		// Object types
		WheelCollider _wheelCollider;
		Transform _susTrav, _wheel, _trackSteering;
		KFModuleWheel _KFModuleWheel;
		
		// Gloabl variables
		Vector3 initialPosition, initialSteeringAngles, _wheelRotation;
        
		int susTravIndex = 1;
		int steeringIndex = 1;
		public int directionCorrector = 1;
        
		float fDegreesPerTick;
		bool couroutinesActive;
		
		/// <summary>Local reference for the tweakScaleCorrector parameter in KFModuleWheel.</summary>
		/// <remarks>This is completely unnecessary in this module now.  Will be removed once it's certain to not cause a problem.</remarks>
		public float tweakScaleCorrector;
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFWheel");
		
		public override void OnStart(PartModule.StartState state)
		{
			_KFModuleWheel = part.GetComponentInChildren<KFModuleWheel>();
			tweakScaleCorrector = _KFModuleWheel.tweakScaleCorrector;
            
			if (!isConfigured)
			{
				foreach (WheelCollider wc in part.GetComponentsInChildren<WheelCollider>())
				{
					if (wc.name.StartsWith(colliderName, StringComparison.Ordinal))
					{
						_wheelCollider = wc;
						fSuspensionDistance = wc.suspensionDistance;
						
						#if DEBUG
						KFLog.Log(string.Format("SuspensionDistance is: {0}.", suspensionDistance));
						#endif
						
						isConfigured = true;
					}
				}
			}
			
			if (HighLogic.LoadedSceneIsFlight && !Equals(vessel.vesselType, VesselType.Debris))
			{
				GameEvents.onGamePause.Add(new EventVoid.OnEvent(OnPause));
				GameEvents.onGameUnpause.Add(new EventVoid.OnEvent(OnUnPause));
				
				// Find named onjects in part.
				foreach (WheelCollider wc in part.GetComponentsInChildren<WheelCollider>())
				{
					if (wc.name.StartsWith(colliderName, StringComparison.Ordinal))
						_wheelCollider = wc;
				}
				
				foreach (Transform tr in part.GetComponentsInChildren<Transform>())
				{
					if (tr.name.StartsWith(wheelName, StringComparison.Ordinal))
						_wheel = tr;
					if (tr.name.StartsWith(steeringName, StringComparison.Ordinal))
						_trackSteering = tr;
					if (tr.name.StartsWith(sustravName, StringComparison.Ordinal))
						_susTrav = tr;
				}
				
				initialPosition = _susTrav.localPosition;
				susTravIndex = susTravAxis.SetAxisIndex();
				steeringIndex = steeringAxis.SetAxisIndex(); 
				
				if (_KFModuleWheel.hasSteering)
				{
					initialSteeringAngles = _trackSteering.transform.localEulerAngles;
					
					#if DEBUG
					KFLog.Log(string.Format("initial steering angles are \"{0}\"", initialSteeringAngles));
					#endif
				}
				
				directionCorrector = useDirectionCorrector ? _KFModuleWheel.directionCorrector : 1;
				_wheelRotation = new Vector3(wheelRotationX, wheelRotationY, wheelRotationZ);
				
				if (Equals(lastFrameTraverse, 0)) //check to see if we have a value in persistance
				{
					#if DEBUG
					KFLog.Log("Last frame = 0. Setting suspension distance.");
					#endif
					
					lastFrameTraverse = _wheelCollider.suspensionDistance;
				}
				
				#if DEBUG
				KFLog.Log(string.Format("Last frame = {0}", lastFrameTraverse));
				#endif
				
				couroutinesActive = true;
				
				MoveSuspension(susTravIndex, -lastFrameTraverse, _susTrav); //to get the initial stuff correct
				
				if (_KFModuleWheel.hasSteering)
				{
					StartCoroutine("Steering");
					
					#if DEBUG
					KFLog.Log("Starting steering coroutine.");
					#endif
				}
				if (trackedWheel)
					StartCoroutine("TrackedWheel");
				else
					StartCoroutine("IndividualWheel");
				
				if (hasSuspension)
				{
					KFLog.Warning("KFWheel suspension module is deprecated. Please use KFSuspension.");
					StartCoroutine("Suspension");
				}
				part.force_activate();
			}
			base.OnStart(state);
		}
		
		// disable FunctionNeverReturns
		IEnumerator Steering() //Coroutine for steering
		{
			Vector3 newSteeringAngle;
			
			while (true)
			{
				newSteeringAngle = initialSteeringAngles;
				newSteeringAngle[steeringIndex] += _KFModuleWheel.fSteeringAngle;
				_trackSteering.transform.localEulerAngles = newSteeringAngle;
				yield return null;
			}
		}
		
		/// <summary>Coroutine for tracked wheels (all rotate the same speed in the part).</summary>
		IEnumerator TrackedWheel()
		{
			while (couroutinesActive)
			{
				_wheel.transform.Rotate(_wheelRotation, _KFModuleWheel.fDegreesPerTick * directionCorrector * rotationCorrection); //rotate wheel
				yield return null;
			}
		}
		
		/// <summary>Coroutine for individual wheels.</summary>
		IEnumerator IndividualWheel()
		{
			while (couroutinesActive)
			{
				fDegreesPerTick = (_wheelCollider.rpm / 60) * Time.deltaTime * 360;
				_wheel.transform.Rotate(_wheelRotation, fDegreesPerTick * directionCorrector * rotationCorrection);
				yield return new WaitForFixedUpdate();
			}
		}
		
		/// <summary>Coroutine for wheels with suspension.</summary>
		/// <remarks>DEPRECATED!!!!!!!!!!!!!! Use KFSuspension instead!</remarks>
		IEnumerator Suspension()
		{
			float fFrameTraverse;
			bool grounded;
			
			WheelHit hit;
			
			while (true)
			{
				// Suspension movement
				fFrameTraverse = 0f;
				grounded = _wheelCollider.GetGroundHit(out hit);
				if (grounded)
				{
					fFrameTraverse = -_wheelCollider.transform.InverseTransformPoint(hit.point).y + _KFModuleWheel.raycastError - _wheelCollider.radius;
                    
					if (fFrameTraverse > (_wheelCollider.suspensionDistance + _KFModuleWheel.raycastError))
                        fFrameTraverse = _wheelCollider.suspensionDistance;
					else if (fFrameTraverse < -0.1f)
                        fFrameTraverse = 0f;
					
					lastFrameTraverse = fFrameTraverse;
				}
				else
					fFrameTraverse = lastFrameTraverse;
				
				susTravel = fFrameTraverse;
				_susTrav.localPosition = initialPosition;
				MoveSuspension(susTravIndex, -fFrameTraverse, _susTrav);
				yield return null; 
			}
		}
		
		public void OnPause()
		{
			couroutinesActive = false;
		}
		
		public void OnUnPause()
		{
			couroutinesActive = true;
			if (trackedWheel)
				StartCoroutine("TrackedWheel");
			else
				StartCoroutine("IndividualWheel");
		}
		
		public void MoveSuspension(int index, float movement, Transform movedObject)
		{
			var tempVector = new Vector3(0, 0, 0);
			tempVector[index] = movement * tweakScaleCorrector;
			movedObject.transform.Translate(tempVector, Space.Self);
		}
	}
}
