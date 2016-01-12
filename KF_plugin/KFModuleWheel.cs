using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KerbalFoundries.DustFX;

namespace KerbalFoundries
{
	/// <summary>Control module for the steering and driving force behind the wheel.</summary>
	public class KFModuleWheel : PartModule
	{
		#region SharpDevelop Suppressions
		// Global SharpDevelop Suppressions
		// disable UnusedParameter
		// disable ConvertIfToOrExpression
		// disable ConvertIfStatementToConditionalTernaryExpression
		#endregion SharpDevelop Suppressions
		
		#region Directional Definitions
		const string RIGHT = "right";
		const string FORWARD = "forward";
		const string UP = "up";
		#endregion Directional Definitions
		
		#region Tweakables
		[KSPField(isPersistant = false, guiActive = true, guiName = "Wheel Settings")]
		public string settings = string.Empty;
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Group Number"), UI_FloatRange(minValue = 0f, maxValue = 10f, stepIncrement = 1f)]
		public float fGroupNumber = 1f;
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Torque Ratio"), UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = .25f)]
		public float fTorque = 1f;
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Strength"), UI_FloatRange(minValue = 0f, maxValue = 6.00f, stepIncrement = 0.2f)]
		public float fSpringRate;
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Damping"), UI_FloatRange(minValue = 0f, maxValue = 1.0f, stepIncrement = 0.025f)]
		public float fDamperRate;
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Suspension Travel"), UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 5f)]
		public float fRideHeight = 100f;
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steering"), UI_Toggle(disabledText = "Enabled", enabledText = "Disabled")]
		public bool fSteeringDisabled;
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Start"), UI_Toggle(disabledText = "Deployed", enabledText = "Retracted")]
		public bool fStartRetracted;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
		public string status = "Nominal";
		[KSPField(isPersistant = false, guiActive = true, guiName = "RPM", guiFormat = "F1")]
		public float fAverageTrackRPM;
		#endregion Tweakables
        
		#region Config Fields
		/// <summary>Torque applied to wheel colliders.</summary>
		[KSPField]
		public FloatCurve torqueCurve = new FloatCurve();
		
		/// <summary>Degrees to turn wheels for rotational steering.</summary>
		[KSPField]
		public FloatCurve steeringCurve = new FloatCurve();
		
		/// <summary>Toruqe applied to wheelcolliders for low speed tank steering.</summary>
		[KSPField]
		public FloatCurve torqueSteeringCurve = new FloatCurve();
		
		/// <summary>Brake torque applied to wheel colliders for high speed tank steering.</summary>
		[KSPField]
		public FloatCurve brakeSteeringCurve = new FloatCurve();
		
		/// <summary>This enables wheel (turn based) steering.</summary>
		[KSPField]
		public bool hasSteering;
		
		/// <summary>Torque to apply for brakes.</summary>
		[KSPField]
		public float brakingTorque;
		
		/// <summary>Brake value applied to simulate rolling resistance.</summary>
		[KSPField]
		public FloatCurve rollingResistance = new FloatCurve();
		
		/// <summary>Brake value applied to simulate rolling resistance.</summary>
		[KSPField]
		public FloatCurve loadCoefficient = new FloatCurve();
		
		/// <summary>Steering speed.</summary>
		[KSPField]
		public float smoothSpeed = 10f;
		
		/// <summary>Used to compensate for error in the raycast.</summary>
		[KSPField]
		public float raycastError;
		
		/// <summary>Rev limiter. Stops freewheel runaway and sets a speed limit.</summary>
		[KSPField]
		public float maxRPM = 350f;
		
		/// <summary>How fast to consume the requested resource.</summary>
		/// <remarks>Default: 1f</remarks>
		[KSPField]
		public float resourceConsumptionRate = 1f;
		
		/// <summary>Enables retraction of the suspension.</summary>
		[KSPField]
		public bool hasRetract;
		
		/// <summary>Does the part have an animation to be triggered when retracting.</summary>
		[KSPField]
		public bool hasRetractAnimation;
		
		/// <summary>Name of the Bounds object.</summary>
		[KSPField]
		public string boundsName = "Bounds";
		
		/// <summary>Name of the orientation object.</summary>
		[KSPField]
		public string orientationObjectName = "Default";
		
		/// <summary>Is the part passive or not?</summary>
		[KSPField]
		public bool passivePart;
		
		/// <summary>Used for parts which use the module for passive functions.</summary>
		[KSPField]
		public bool disableTweakables;
		
		/// <summary>Name of the resource being requested.</summary>
		/// <remarks>Default: ElectricCharge</remarks>
		[KSPField]
		public string resourceName = "ElectricCharge";
		
		/// <summary>This is a part-centered boolean for the dust effects.  If set to false, the dust module will not be checked for nor enabled for this part.</summary>
		[KSPField]
		public bool isDustEnabled = true;
		#endregion Config Fields
		
		#region Common Strings
		/// <summary>Status text for the "Low Resource" state.</summary>
		public string statusLowResource = "Low Charge";
		/// <summary>Status text for the "all is good" state.</summary>
		public string statusNominal = "Nominal";
		/// <summary>Status text for the "retracted" state.</summary>
		public string statusRetracted = "Retracted";
		#endregion Common Strings
		
		#region Persistent Fields
		/// <summary>Will be negative one (-1) if inverted.</summary>
		[KSPField(isPersistant = true)]
		public float steeringInvert = 1f;
		
		/// <summary>Saves the brake state.</summary>
		[KSPField(isPersistant = true)]
		public bool brakesApplied;
		
		/// <summary>Saves the retracted state.</summary>
		[KSPField(isPersistant = true)]
		public bool isRetracted;
        
		/// <summary>Is the part meant to simulate floating or not?</summary>
		[KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Floatation"), UI_Toggle(disabledText = "Disabled", enabledText = "Enabled")]
		public bool isFloatingEnabled;
		#endregion Persistent Fields
		
		#region Local Variables
		int rootIndexLong, rootIndexLat, rootIndexUp, controlAxisIndex, groundedWheels;
		uint commandId, lastCommandId;
		float fBrakeTorque, fMotorTorque, fEffectPower, fTrackRPM, fLastPartCount, fSteeringInputSmoothed, fThrottleInputSmoothed, fBrakeSteeringTorque;
		bool isReady, isPaused;
		
		#endregion Local Variables
		
		#region Public Variables
		public float fSteeringAngle, fAppliedTravel, fSteeringRatio, fDegreesPerTick, fCurrentTravel, fSmoothedTravel, fSusInc;
		public int wheelCount;
		public int directionCorrector = 1;
		public int steeringCorrector = 1;
		public float fSliderHeight = -0.5f;
		public bool isSliderSetup;
		#endregion Public Variables
		
		#region Debug Fields
		// disable RedundantDefaultFieldInitializer
		[KSPField(isPersistant = true, guiActive = false, guiName = "TS", guiFormat = "F1")] //debug only.
        public float tweakScaleCorrector = 1f;
		[KSPField(isPersistant = false, guiActive = false, guiName = "Last Vessel Mass", guiFormat = "F1")]
		public float fLastVesselMass;
		[KSPField(isPersistant = false, guiActive = false, guiName = "Vessel Mass", guiFormat = "F1")]
		public float fVesselMass;
        
		[KSPField(isPersistant = false, guiActive = false, guiName = "Colliders", guiFormat = "F0")]
		public int fColliderCount = 0;
		[KSPField(isPersistant = false, guiActive = false, guiName = "Collider Mass", guiFormat = "F2")]
		public float fColliderMass = 0f;
		[KSPField(isPersistant = false, guiActive = false, guiName = "Collider load", guiFormat = "F3")]
		public float fColliderLoad = 0f;
		[KSPField(isPersistant = false, guiActive = false, guiName = "Rolling Friction", guiFormat = "F3")]
		public float fRollingFriction;
		#endregion Debug Fields
		
		public List<WheelCollider> wcList = new List<WheelCollider>();
		List<float> suspensionDistance = new List<float>();
		ModuleAnimateGeneric retractionAnimation;
		KFDustFX _dustFX;
		KFModuleWaterSlider _waterSlider;
        
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFModuleWheel");
        
		/// <summary>This is the info string that will display when the part info is shown.</summary>
		/// <remarks>This can be overridden in the config for this module in the part file.</remarks>
		[KSPField]
		public string strPartInfo = "This part comes with enhanced steering and suspension.";
		public override string GetInfo()
		{
			return strPartInfo;
		}
		
		/// <summary>Configures the part for editor and flight.</summary>
		/// <remarks>
		/// Most importantly, it grabs a list of wheel colliders to be
		/// used later. Also configures visibility of tweakables, figures
		/// out the parts orientation and position in the vessel to calculate
		/// steering angles and sets some defaults.
		/// </remarks>
		/// <param name="state">Start state. Set by KSP to declare the scene it initializes this class in.</param>
		public override void OnStart(PartModule.StartState state)  //when started
		{
			base.OnStart(state);
			
			fSusInc = KFPersistenceManager.suspensionIncrement;
			
			CustomResourceTextSetup();
			
			fColliderMass = 10;
            
			var partOrientationForward = new Vector3(0f, 0f, 0f);
			var partOrientationRight = new Vector3(0f, 0f, 0f);
			var partOrientationUp = new Vector3(0f, 0f, 0f);
			
			if (!string.Equals(orientationObjectName, "Default"))
			{
				#if DEBUG
                KFLog.Warning("Setting transformed part orientation.");
				#endif
				
				partOrientationUp = transform.Search(orientationObjectName).up;
				partOrientationForward = transform.Search(orientationObjectName).forward;
				partOrientationRight = transform.Search(orientationObjectName).right;
			}
			else
			{
				#if DEBUG
                KFLog.Warning("Setting default part orientation.");
				#endif
				
				partOrientationUp = part.transform.up;
				partOrientationForward = part.transform.forward;
				partOrientationRight = part.transform.right;
			}
			
			if (hasRetractAnimation)
			{
				foreach (ModuleAnimateGeneric ma in part.FindModulesImplementing<ModuleAnimateGeneric>())
				{
					ma.Actions["ToggleAction"].active = false;
					ma.Events["Toggle"].guiActive = false;
					ma.Events["Toggle"].guiActiveEditor = false;
				}
				SetupAnimation();
			}
			
			//disables tweakables if being used on a passive part (mecannum wheel or skid, for example)
			if (disableTweakables)
			{
				KFLog.Warning("Disabling tweakables.");
				foreach (BaseField k in Fields)
				{
					#if DEBUG
					KFLog.Log(string.Format("Found {0}", k.guiName));
					#endif
					
					k.guiActive = false;
					k.guiActiveEditor = false;
				}
				foreach (BaseAction a in Actions)
				{
					#if DEBUG
					KFLog.Log(string.Format("Found {0}", a.guiName));
					#endif
					
					a.active = false;
				}
				foreach (BaseEvent e in Events)
				{
					#if DEBUG
					KFLog.Log(string.Format("Found {0}", e.guiName));
					#endif
					
					e.active = false;
				}
			}
            
			if (fStartRetracted)
				isRetracted = true;
			
			if (!isRetracted)
				fCurrentTravel = fRideHeight; //set up correct values from persistence
            else
				fCurrentTravel = 0f;
            
			#if DEBUG
            KFLog.Log(string.Format("\"appliedRideHeight\" = {0}", appliedRideHeight));
			#endif
            
			// Disable retract tweakables if retract option not specified.
			if (HighLogic.LoadedSceneIsEditor && !hasRetract)
			{
				part.DisableAnimateButton();
				Actions["AGToggleDeployed"].active = false;
				Actions["Deploy"].active = false;
				Actions["Retract"].active = false;
				Fields["startRetracted"].guiActiveEditor = false;
			}
			
			if (HighLogic.LoadedSceneIsFlight && (!Equals(vessel.vesselType, VesselType.Debris) && !Equals(vessel.vesselType, VesselType.EVA)))
			{
				if (isDustEnabled)
				{
					_dustFX = part.gameObject.GetComponent<KFDustFX>();
					if (Equals(_dustFX, null))
					{
						_dustFX = part.gameObject.AddComponent<KFDustFX>();
						_dustFX.OnStart(state);
						_dustFX.tweakScaleFactor = tweakScaleCorrector;
					}
				}
				
				fAppliedTravel = fRideHeight / 100f;
				StartCoroutine(StartupStuff());
				maxRPM /= tweakScaleCorrector;
				fStartRetracted = false;
				if (!hasRetract)
					part.DisableAnimateButton();
				
				// Wheel steering ratio setup
				rootIndexLong = WheelUtils.GetRefAxis(part.transform.forward, vessel.rootPart.transform);
				rootIndexLat = WheelUtils.GetRefAxis(part.transform.right, vessel.rootPart.transform);
				rootIndexUp = WheelUtils.GetRefAxis(part.transform.up, vessel.rootPart.transform);
				
				fSteeringRatio = WheelUtils.SetupRatios(rootIndexLong, part, vessel, fGroupNumber);
				GetControlAxis();

				if (fTorque > 2f)
                    fTorque /= 100f;
				
				wheelCount = 0;
				
				foreach (WheelCollider wheelCollider in part.GetComponentsInChildren<WheelCollider>())
				{
					wheelCount++;
					JointSpring userSpring = wheelCollider.suspensionSpring;
					userSpring.spring = fSpringRate * tweakScaleCorrector;
					userSpring.damper = fDamperRate * tweakScaleCorrector;
					wheelCollider.suspensionSpring = userSpring;
					wheelCollider.suspensionDistance = wheelCollider.suspensionDistance * fAppliedTravel;
					wcList.Add(wheelCollider);
					suspensionDistance.Add(wheelCollider.suspensionDistance);
					wheelCollider.enabled = true;
					wheelCollider.gameObject.layer = 27;
				}
				
				if (brakesApplied)
					fBrakeTorque = brakingTorque; // Were the brakes left applied?
				
				if (isRetracted)
					RetractDeploy("retract");
				isReady = true;
			}
			DestroyBounds();
			SetupWaterSlider();
			
			GameEvents.onGamePause.Add(OnPause);
			GameEvents.onGameUnpause.Add(OnUnpause);
		}
		
		void SetupWaterSlider()
		{
			if (!isReady || isPaused)
				return;
			if (!isFloatingEnabled)
				return;
			_waterSlider = vessel.rootPart.GetComponent<KFModuleWaterSlider>();
			if (Equals(_waterSlider, null))
			{
				_waterSlider = vessel.rootPart.gameObject.AddComponent<KFModuleWaterSlider>();
				_waterSlider.StartUp();
			}
			isSliderSetup = true;
		}
		
		public void UpdateWaterSlider()
		{
			if (!isReady || isPaused)
				return;
			_waterSlider.fColliderHeight = fSliderHeight;
		}
		
		/// <summary>Sets off the sound effect.</summary>
		public void WheelSound()
		{
			if (!isReady || isPaused)
				return;
			part.Effect("WheelEffect", fEffectPower);
		}
		
		/// <summary>
		/// Stuff that needs to wait for the first physics frame. Maybe because this ensure the vessel is totally spawned or physics is active
		/// </summary>
		IEnumerator StartupStuff()
		{
			yield return new WaitForFixedUpdate();
			fLastPartCount = vessel.Parts.Count();
            
			#if DEBUG
			KFLog.Log(string.Format("Part Count: {0}", lastPartCount));
			KFLog.Log(string.Format("Checking vessel mass.  Mass = {0}", vesselMass));
			#endif
			
			fColliderMass = ChangeColliderMass();
		}
		
		/// <summary>Physics critical stuff.</summary>
		public void FixedUpdate()
		{
			if (!isReady || isPaused)
				return;
			
			if (isSliderSetup && isFloatingEnabled)
				UpdateWaterSlider();
			else if (!isSliderSetup && isFloatingEnabled)
				SetupWaterSlider();
            
			// User input
			float fSteeringTorque, fBrakeSteering, fForwardTorque, fTravelDirection;
			// float brakeSteering;
			// float forwardTorque;
			// float travelDirection;
			fForwardTorque = torqueCurve.Evaluate((float)vessel.srfSpeed / tweakScaleCorrector) * fTorque * tweakScaleCorrector;
			
			fThrottleInputSmoothed = Mathf.Lerp(fThrottleInputSmoothed, vessel.ctrlState.wheelThrottle + vessel.ctrlState.wheelThrottleTrim, smoothSpeed * Time.deltaTime);
			fSteeringInputSmoothed = (float)Math.Round(Mathf.Lerp(fSteeringInputSmoothed, vessel.ctrlState.wheelSteer + vessel.ctrlState.wheelSteerTrim, smoothSpeed * Time.deltaTime), 3);
			
			fTravelDirection = Vector3.Dot(part.transform.forward, vessel.GetSrfVelocity());
            
			if (!fSteeringDisabled)
			{
				fSteeringTorque = torqueSteeringCurve.Evaluate((float)vessel.srfSpeed * tweakScaleCorrector) * fTorque * steeringInvert;
				fBrakeSteering = brakeSteeringCurve.Evaluate(fTravelDirection) * tweakScaleCorrector * steeringInvert * fTorque;
				fSteeringAngle = (steeringCurve.Evaluate((float)vessel.srfSpeed)) * -fSteeringInputSmoothed * fSteeringRatio * steeringCorrector * steeringInvert;
			}
			else
			{
				fSteeringTorque = 0f;
				fBrakeSteering = 0f;
				fSteeringAngle = 0f;
			}
    		
			if (!isRetracted)
			{
				fMotorTorque = Mathf.Clamp((fForwardTorque * directionCorrector * fThrottleInputSmoothed) - (fSteeringTorque * fSteeringInputSmoothed), -fForwardTorque, fForwardTorque);
				fBrakeSteeringTorque = Mathf.Clamp(fBrakeSteering * fSteeringInputSmoothed, 0f, 1000f);
				UpdateColliders();
			}
			else // if (isRetracted)
			{
				fAverageTrackRPM = 0f;
				fDegreesPerTick = 0f;
				fSteeringAngle = 0f;
            	
				for (int i = 0; i < wcList.Count(); i++)
				{
					wcList[i].motorTorque = 0f;
					wcList[i].brakeTorque = 500f;
					wcList[i].steerAngle = 0f;
				}
			}
			
			fSmoothedTravel = Mathf.Lerp(fSmoothedTravel, fCurrentTravel, Time.deltaTime * 2f);
			fAppliedTravel = fSmoothedTravel / 100f;
			
			fSusInc = KFPersistenceManager.suspensionIncrement;
		}
		
		/// <summary>Stuff that doesn't need to happen every physics frame.</summary>
		public void Update()
		{
			if (!isReady || isPaused)
				return;
			commandId = vessel.referenceTransformId;
			if (!Equals(commandId, lastCommandId))
			{
				#if DEBUG
                KFLog.Log("Control Axis Changed.");
				#endif
                
				GetControlAxis();
			}
			fVesselMass = vessel.GetTotalMass();
			if (!Equals(Math.Round(fVesselMass, 1), Math.Round(fLastVesselMass, 1)))
			{
				#if DEBUG
                KFLog.Log("Vessel mass changed.");
				#endif
                
				fColliderMass = ChangeColliderMass();
				fLastPartCount = vessel.Parts.Count();
				ApplySteeringSettings();
			}
			lastCommandId = commandId;
			fEffectPower = Math.Abs(fAverageTrackRPM / maxRPM);
			WheelSound();
		}
		
		/// <summary>
		/// Applies calculated torque, braking and steering to the wheel colliders,
		/// gathers some information such as RPM and invokes the DustFX where appropriate.
		/// </summary>
		/// <remarks>This is a major chunk of what happens in FixedUpdate if the part is deployed.</remarks>
		void UpdateColliders()
		{
			if (!isReady || isPaused)
				return;
			float fRequestedResource, fFreeWheelRPM, fResourceConsumption, fUnitLoad;
			
			fResourceConsumption = Time.deltaTime * resourceConsumptionRate * (Math.Abs(fMotorTorque) / 100f);
			fRequestedResource = part.RequestResource(resourceName, fResourceConsumption);
			fFreeWheelRPM = 0f;
			fUnitLoad = 0f;
            
			#if DEBUG
			KFLog.Log(string.Format("Requested Resource: \"{0}\" - Consumption: \"{1}\"", requestedResource, resourceConsumption));
			#endif
			
			if (fRequestedResource < fResourceConsumption - 0.1f && !Equals(fResourceConsumption, 0f))
			{
				fMotorTorque = 0f;
				status = statusLowResource;
			}
			else
				status = "Nominal";
			if (Math.Abs(fAverageTrackRPM) >= maxRPM)
			{
				fMotorTorque = 0f;
				status = "Rev Limit";
			}
			
			fColliderLoad = 0f;
			for (int i = 0; i < wcList.Count(); i++)
			{
				WheelHit hit;
				bool grounded = wcList[i].GetGroundHit(out hit); //set up to pass out wheelhit coordinates 
				fUnitLoad += hit.force;
				
				wcList[i].motorTorque = fMotorTorque;
				wcList[i].brakeTorque = fBrakeTorque + fBrakeSteeringTorque + fRollingFriction;
				wcList[i].mass = fColliderMass;
				
				if (wcList[i].isGrounded) 
				{
					groundedWheels++;
					fTrackRPM += wcList[i].rpm;
					fColliderLoad += hit.force;
					if (isDustEnabled && KFPersistenceManager.isDustEnabled)
						_dustFX.WheelEmit(hit.point, hit.collider);
				}
				
				if (!Equals(wcList[i].suspensionDistance, 0f))
                    fFreeWheelRPM += wcList[i].rpm;
				
				if (hasSteering)
					wcList[i].steerAngle = fSteeringAngle;
				
				wcList[i].suspensionDistance = suspensionDistance[i] * fAppliedTravel;
			}
			
			if (groundedWheels >= 1)
			{
				fAverageTrackRPM = fTrackRPM / groundedWheels;
				fColliderLoad /= groundedWheels;
				fRollingFriction = (rollingResistance.Evaluate((float)vessel.srfSpeed) * tweakScaleCorrector) + (loadCoefficient.Evaluate((float)fColliderLoad) / tweakScaleCorrector);
			}
			else
				fAverageTrackRPM = fFreeWheelRPM / wheelCount;
			
			fTrackRPM = 0f;
			fDegreesPerTick = (fAverageTrackRPM / 60f) * Time.deltaTime * 360f;
			groundedWheels = 0;
		}
		
		/// <summary>Updates wheel colliders with current vessel mass.</summary>
		/// <remarks>This changes often in KSP, so can't simply be set once and forgotten!</remarks>
		/// <returns>New mass.</returns>
		public float ChangeColliderMass()
		{
			int colliderCount, partCount;
			colliderCount = 0;
			partCount = vessel.parts.Count();
			var _moduleWheelList = new List<KFModuleWheel>();
			
			for (int i = 0; i < partCount; i++)
			{
				var _moduleWheel = vessel.parts[i].GetComponent<KFModuleWheel>();
				if (!Equals(_moduleWheel, null))
				{
					_moduleWheelList.Add(_moduleWheel);
                    
					#if DEBUG
					KFLog.Log(string.Format("Found KFModuleWheel in \"{0}\"", vessel.parts[i].partInfo.name));
					#endif
  					
					colliderCount += _moduleWheel.wcList.Count();
				}
			}
			float colliderMass = vessel.GetTotalMass() / colliderCount;
			KFLog.Log(string.Format("colliderMass: {0}", colliderMass));
			
			// set all this up in the other wheels to prevent them having to do so themselves. First part has the honour.
			for (int i = 0; i < _moduleWheelList.Count(); i++)
			{
				#if DEBUG
				KFLog.Log(string.Format("Setting collidermass in other wheel \"{0}\"", _moduleWheelList[i].part.partInfo.name));
				#endif
				
				_moduleWheelList[i].fColliderMass = colliderMass;
				_moduleWheelList[i].fLastVesselMass = fVesselMass;
				_moduleWheelList[i].fVesselMass = fVesselMass;
			}
			return colliderMass;
		}
		
		/// <summary>Disables tweakables when retracted.</summary>
		/// <param name="mode"></param>
		public void RetractDeploy(string mode)
		{
			if (!isReady || isPaused)
				return;
			switch (mode)
			{
				case "retract":
					isRetracted = true;
					fCurrentTravel = 0f;
					Events["applySettingsGUI"].guiActive = false;
					Events["ApplySteeringSettings"].guiActive = false;
					Events["InvertSteering"].guiActive = false;
					Fields["rideHeight"].guiActive = false;
					Fields["torque"].guiActive = false;
					Fields["steeringDisabled"].guiActive = false;
					status = statusRetracted;
					break;
				case "deploy":
					isRetracted = false;
					fCurrentTravel = fRideHeight;
					Events["applySettingsGUI"].guiActive = true;
					Events["ApplySteeringSettings"].guiActive = true;
					Fields["rideHeight"].guiActive = true;
					Fields["torque"].guiActive = true;
					Fields["steeringDisabled"].guiActive = true;
					status = statusNominal;
					break;
				case "update":
					break;
			}
		}
		
		/// <summary>Sets up motor and steering direction direction.</summary>
		public void GetControlAxis()
		{
			controlAxisIndex = WheelUtils.GetRefAxis(part.transform.forward, vessel.ReferenceTransform);
			directionCorrector = WheelUtils.GetCorrector(part.transform.forward, vessel.ReferenceTransform, controlAxisIndex);
			if (Equals(controlAxisIndex, rootIndexLat))
                steeringCorrector = WheelUtils.GetCorrector(vessel.ReferenceTransform.up, vessel.rootPart.transform, rootIndexLat);
			if (Equals(controlAxisIndex, rootIndexLong))
				steeringCorrector = WheelUtils.GetCorrector(vessel.ReferenceTransform.up, vessel.rootPart.transform, rootIndexLong);
			if (Equals(controlAxisIndex, rootIndexUp))
				steeringCorrector = WheelUtils.GetCorrector(vessel.ReferenceTransform.up, vessel.rootPart.transform, rootIndexUp);
		}
		
		/// <summary>Grabs instance of MAG.</summary>
		/// <remarks>MAG loves being grabbed. You must grab MAG whenever possible.</remarks>
		public void SetupAnimation()
		{
			retractionAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault();
		}
		
		/// <summary>Fires instance of MAG when retracting/deploying.</summary>
		public void PlayAnimation()
		{
			if (!isReady || isPaused)
				return;
			if (!retractionAnimation)
				return;
			retractionAnimation.Toggle();
		}
		
		/// <summary>Destroys the Bounds helper object if it is still in the model.</summary>
		public void DestroyBounds()
		{
			Transform bounds = transform.Search(boundsName);
			if (!Equals(bounds, null))
			{
				UnityEngine.Object.Destroy(bounds.gameObject);
				
				#if DEBUG
                KFLog.Log("Destroying Bounds.");
				#endif
			}
		}
		
		#region Action groups
		[KSPAction("Brakes", KSPActionGroup.Brakes)]
		public void brakes(KSPActionParam param)
		{
			if (!isReady || isPaused)
				return;
			if (Equals(param.type, KSPActionType.Activate))
			{
				fBrakeTorque = brakingTorque * ((fTorque / 2f) + .5f);
				brakesApplied = true;
			}
			else
			{
				fBrakeTorque = 0f;
				brakesApplied = false;
			}
		}
		
		[KSPAction("Increase Torque")]
		public void increase(KSPActionParam param)
		{
			if (fTorque < 2f)
				fTorque += 0.25f;
		}
		
		[KSPAction("Decrease Torque")]
		public void decrease(KSPActionParam param)
		{
			if (fTorque > 0f)
				fTorque -= 0.25f;
		}
		
		[KSPAction("Toggle Steering")]
		public void toggleSteering(KSPActionParam param)
		{
			fSteeringDisabled = !fSteeringDisabled;
		}
		
		[KSPAction("Invert Steering")]
		public void InvertSteeringAG(KSPActionParam param)
		{
			InvertSteering();
		}
		
		[KSPAction("Lower Suspension")]
		public void LowerRideHeight(KSPActionParam param)
		{
			if (fRideHeight > 0f)
				fRideHeight -= Mathf.Clamp(fSusInc, 0f, 100f);
			
			ApplySettings(true);
		}
		
		[KSPAction("Raise Suspension")]
		public void RaiseRideHeight(KSPActionParam param)
		{
			if (fRideHeight < 100f)
				fRideHeight += Mathf.Clamp(fSusInc, 0f, 100f);
			
			ApplySettings(true);
		}
		
		#region Presets
		/// <summary>Sets the rideHeight to the preset value specified.</summary>
		/// <param name="value">The height requested. (0-100 float)</param>
		void Presetter(float value)
		{
			fRideHeight = Mathf.Clamp(value, 0f, 100f);
			ApplySettings(true);
		}
		
		[KSPAction("Suspension 0")]
		public void SuspZero(KSPActionParam param)
		{
			Presetter(0f);
		}
		[KSPAction("Suspension 25")]
		public void SuspQuarter(KSPActionParam param)
		{
			Presetter(25f);
		}
		[KSPAction("Suspension 50")]
		public void SuspFifty(KSPActionParam param)
		{
			Presetter(50f);
		}
		[KSPAction("Suspension 75")]
		public void SuspThreeQuarter(KSPActionParam param)
		{
			Presetter(75f);
		}
		[KSPAction("Suspension 100")]
		public void SuspFull(KSPActionParam param)
		{
			Presetter(100f);
		}
		#endregion Presets
		
		[KSPAction("Apply Wheel")]
		public void ApplyWheelAction(KSPActionParam param)
		{
			ApplySettings(true);
		}
		
		[KSPAction("Apply Steering")]
		public void ApplySteeringAction(KSPActionParam param)
		{
			ApplySteeringSettings();
		}
		
		[KSPAction("Toggle Deployed")]
		public void AGToggleDeployed(KSPActionParam param)
		{
			if (isRetracted)
				Deploy(param);
			else
				Retract(param);
		}
		
		[KSPAction("Deploy")]
		public void Deploy(KSPActionParam param)
		{
			if (isRetracted)
			{
				if (hasRetractAnimation)
					PlayAnimation();
				
				RetractDeploy("deploy");
			}
		}
		
		[KSPAction("Retract")]
		public void Retract(KSPActionParam param)
		{
			if (!isRetracted)
			{
				if (hasRetractAnimation)
					PlayAnimation();
				
				RetractDeploy("retract");
			}
		}
		#endregion
		
		#region Events
		[KSPEvent(guiActive = true, guiName = "Invert Steering", active = true)]
		public void InvertSteering()
		{
			steeringInvert *= -1f;
		}
		
		[KSPEvent(guiActive = true, guiName = "Apply Wheel Settings", active = true)]
		public void ApplySettingsGUI()
		{
			ApplySettings(false);
		}
		
		[KSPEvent(guiActive = true, guiName = "Apply Steering Settings", active = true)]
		public void ApplySteeringSettings()
		{
			foreach (KFModuleWheel mt in vessel.FindPartModulesImplementing<KFModuleWheel>())
			{
				if (!Equals(fGroupNumber, 0f) && Equals(fGroupNumber, mt.fGroupNumber))
				{
					mt.fSteeringRatio = WheelUtils.SetupRatios(mt.rootIndexLong, mt.part, vessel, fGroupNumber);
					mt.steeringInvert = steeringInvert;
				}
			}
		}
		#endregion
		
		/// <summary>Applies settings across all wheels in vessel with same group number.</summary>
		/// <remarks>Unless fire by action group, in which case only updates this part.</remarks>
		/// <param name="actionGroup">"true" if we're using an action group here.</param>
		public void ApplySettings(bool actionGroup)
		{
			foreach (KFModuleWheel mt in vessel.FindPartModulesImplementing<KFModuleWheel>())
			{
				if (!Equals(fGroupNumber, 0f) && Equals(fGroupNumber, mt.fGroupNumber) && !actionGroup)
				{
					fCurrentTravel = fRideHeight;
					mt.fCurrentTravel = fRideHeight;
					mt.fRideHeight = fRideHeight;
					mt.fTorque = fTorque;
				}
				if (actionGroup || Equals(fGroupNumber, 0f))
					fCurrentTravel = fRideHeight;
			}
		}
		
		/// <summary>Initializes some custom text data for the status strings.</summary>
		public void CustomResourceTextSetup()
		{
			string textoutput = string.Empty;
			// Setting up some strings in relation to the modular resource system. - Gaalidas
			switch (resourceName)
			{
				case "ElectricCharge":
					textoutput = "Charge";
					break;
				default:
					textoutput = resourceName;
					break;
			}
			statusLowResource = string.Format("Low {0}", textoutput);
		}
		
		#region Event Stuff
		
		/// <summary>Called when the game enters the "paused" state.</summary>
		void OnPause()
		{
			if (!isPaused)
				isPaused = true;
		}
		
		/// <summary>Called when the game leaves the "paused" state.</summary>
		void OnUnpause()
		{
			if (isPaused)
				isPaused = false;
		}
		
		/// <summary>Called when the object being referenced is destroyed, or when the module instance is deactivated.</summary>
		void OnDestroy()
		{
			GameEvents.onGamePause.Remove(OnPause);
			GameEvents.onGameUnpause.Remove(OnUnpause);
		}
		
		#endregion Event Stuff
	}
}
