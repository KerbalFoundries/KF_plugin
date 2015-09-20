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
		// Global SharpDevelop Suppressions
		// disable UnusedParameter
		// disable ConvertIfToOrExpression
		// disable ConvertIfStatementToConditionalTernaryExpression
		
		// Name definitions
		const string right = "right";
		const string forward = "forward";
		const string up = "up";
		
		// Tweakables
		[KSPField(isPersistant = false, guiActive = true, guiName = "Wheel Settings")]
		public string settings = string.Empty;
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Group Number"), UI_FloatRange(minValue = 0f, maxValue = 10f, stepIncrement = 1f)]
		public float groupNumber = 1f;
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Torque Ratio"), UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = .25f)]
		public float torque = 1f;
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Strength"), UI_FloatRange(minValue = 0f, maxValue = 6.00f, stepIncrement = 0.2f)]
		public float springRate;
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Damping"), UI_FloatRange(minValue = 0f, maxValue = 1.0f, stepIncrement = 0.025f)]
		public float damperRate;
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Suspension Travel"), UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 5f)]
		public float rideHeight = 100f;
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steering"), UI_Toggle(disabledText = "Enabled", enabledText = "Disabled")]
		public bool steeringDisabled;
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Start"), UI_Toggle(disabledText = "Deployed", enabledText = "Retracted")]
		public bool startRetracted;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
		public string status = "Nominal";
		[KSPField(isPersistant = false, guiActive = true, guiName = "RPM", guiFormat = "F1")]
		public float averageTrackRPM;
        
		// Config fields
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
		
		/// <summary>Status text for the "Low Resource" state.</summary>
		public string statusLowResource = "Low Charge";
		/// <summary>Status text for the "all is good" state.</summary>
		public string statusNominal = "Nominal";
		/// <summary>Status text for the "retracted" state.</summary>
		public string statusRetracted = "Retracted";
		
		// Persistent fields
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
		
		// Global variables
		int rootIndexLong;
		int rootIndexLat;
		int rootIndexUp;
		int controlAxisIndex;
		uint commandId;
		uint lastCommandId;
		float brakeTorque;
		float motorTorque;
		bool isReady;
		
		int groundedWheels;
		float effectPower;
		float trackRPM = 0f;
		float lastPartCount;
		float steeringInputSmoothed;
		float throttleInputSmoothed;
		float brakeSteeringTorque;
		
		// Stuff deliberately made available to other modules:
		public float steeringAngle;
		public float appliedTravel;
		public int wheelCount;
        
		public int directionCorrector = 1;
		public int steeringCorrector = 1;
		public float steeringRatio;
		public float degreesPerTick;
		public float currentTravel;
		public float smoothedTravel;
		public float susInc;
		
		// Visible fields (debug)
		[KSPField(isPersistant = true, guiActive = false, guiName = "TS", guiFormat = "F1")] //debug only.
        public float tweakScaleCorrector = 1f;
		[KSPField(isPersistant = false, guiActive = false, guiName = "Last Vessel Mass", guiFormat = "F1")]
		public float lastVesselMass;
		[KSPField(isPersistant = false, guiActive = false, guiName = "Vessel Mass", guiFormat = "F1")]
		public float vesselMass;
        
		[KSPField(isPersistant = false, guiActive = false, guiName = "Colliders", guiFormat = "F0")]
		public int _colliderCount = 0;
		[KSPField(isPersistant = false, guiActive = false, guiName = "Collider Mass", guiFormat = "F2")]
		public float _colliderMass = 0f;
		[KSPField(isPersistant = false, guiActive = false, guiName = "Collider load", guiFormat = "F3")]
		public float colliderLoad = 0f;
		[KSPField(isPersistant = false, guiActive = false, guiName = "Rolling Friction", guiFormat = "F3")]
		public float rollingFriction;
		
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
			
			susInc = KFPersistenceManager.suspensionIncrement;
			
			CustomResourceTextSetup();
			
			_colliderMass = 10;
            
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
            
			if (startRetracted)
				isRetracted = true;
			
			if (!isRetracted)
				currentTravel = rideHeight; //set up correct values from persistence
            else
				currentTravel = 0f;
            
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
				_dustFX = part.gameObject.GetComponent<KFDustFX>();
				if (Equals(_dustFX, null))
				{
					_dustFX = part.gameObject.AddComponent<KFDustFX>();
					_dustFX.OnStart(state);
					_dustFX.tweakScaleFactor = tweakScaleCorrector;
				}
				
				appliedTravel = rideHeight / 100f;
				StartCoroutine(StartupStuff());
				maxRPM /= tweakScaleCorrector;
				startRetracted = false;
				if (!hasRetract)
					part.DisableAnimateButton();
				
				// Wheel steering ratio setup
				rootIndexLong = WheelUtils.GetRefAxis(part.transform.forward, vessel.rootPart.transform);
				rootIndexLat = WheelUtils.GetRefAxis(part.transform.right, vessel.rootPart.transform);
				rootIndexUp = WheelUtils.GetRefAxis(part.transform.up, vessel.rootPart.transform);
				
				steeringRatio = WheelUtils.SetupRatios(rootIndexLong, part, vessel, groupNumber);
				GetControlAxis();

				if (torque > 2f)
                    torque /= 100f;
				
				wheelCount = 0;
				
				foreach (WheelCollider wheelCollider in part.GetComponentsInChildren<WheelCollider>())
				{
					wheelCount++;
					JointSpring userSpring = wheelCollider.suspensionSpring;
					userSpring.spring = springRate * tweakScaleCorrector;
					userSpring.damper = damperRate * tweakScaleCorrector;
					wheelCollider.suspensionSpring = userSpring;
					wheelCollider.suspensionDistance = wheelCollider.suspensionDistance * appliedTravel;
					wcList.Add(wheelCollider);
					suspensionDistance.Add(wheelCollider.suspensionDistance);
					wheelCollider.enabled = true;
					wheelCollider.gameObject.layer = 27;
				}
				
				if (brakesApplied)
					brakeTorque = brakingTorque; // Were the brakes left applied?
				
				if (isRetracted)
					RetractDeploy("retract");
				isReady = true;
			}
			DestroyBounds();
			SetupWaterSlider();
		}
		
		void SetupWaterSlider()
		{
			if (!isFloatingEnabled)
				return;
			_waterSlider = vessel.rootPart.GetComponent<KFModuleWaterSlider>();
			if (Equals(_waterSlider, null))
			{
				_waterSlider = vessel.rootPart.gameObject.AddComponent<KFModuleWaterSlider>();
				_waterSlider.StartUp();
			}
		}
		
		public void UpdateWaterSlider()
		{
			_waterSlider.colliderHeight = -1f;
		}
		
		/// <summary>Sets off the sound effect.</summary>
		public void WheelSound()
		{
			part.Effect("WheelEffect", effectPower);
		}
		
		/// <summary>
		/// Stuff that needs to wait for the first physics frame. Maybe because this ensure the vessel is totally spawned or physics is active
		/// </summary>
		IEnumerator StartupStuff()
		{
			yield return new WaitForFixedUpdate();
			lastPartCount = vessel.Parts.Count();
            
			#if DEBUG
			KFLog.Log(string.Format("Part Count: {0}", lastPartCount));
			KFLog.Log(string.Format("Checking vessel mass.  Mass = {0}", vesselMass));
			#endif
			
			_colliderMass = ChangeColliderMass();
		}
		
		/// <summary>Physics critical stuff.</summary>
		public void FixedUpdate()
		{
			if (!isReady)
				return;
            
			UpdateWaterSlider();
            
			// User input
			float steeringTorque;
			float brakeSteering;
			float forwardTorque = torqueCurve.Evaluate((float)vessel.srfSpeed / tweakScaleCorrector) * torque * tweakScaleCorrector;
			
			throttleInputSmoothed = Mathf.Lerp(throttleInputSmoothed, vessel.ctrlState.wheelThrottle + vessel.ctrlState.wheelThrottleTrim, smoothSpeed * Time.deltaTime);
			steeringInputSmoothed = (float)Math.Round(Mathf.Lerp(steeringInputSmoothed, vessel.ctrlState.wheelSteer + vessel.ctrlState.wheelSteerTrim, smoothSpeed * Time.deltaTime), 3);
			
			float travelDirection = Vector3.Dot(part.transform.forward, vessel.GetSrfVelocity());
            
			if (!steeringDisabled)
			{
				steeringTorque = torqueSteeringCurve.Evaluate((float)vessel.srfSpeed * tweakScaleCorrector) * torque * steeringInvert;
				brakeSteering = brakeSteeringCurve.Evaluate(travelDirection) * tweakScaleCorrector * steeringInvert * torque;
				steeringAngle = (steeringCurve.Evaluate((float)vessel.srfSpeed)) * -steeringInputSmoothed * steeringRatio * steeringCorrector * steeringInvert;
			}
			else
			{
				steeringTorque = 0f;
				brakeSteering = 0f;
				steeringAngle = 0f;
			}
    		
			if (!isRetracted)
			{
				motorTorque = Mathf.Clamp((forwardTorque * directionCorrector * throttleInputSmoothed) - (steeringTorque * steeringInputSmoothed), -forwardTorque, forwardTorque);
				brakeSteeringTorque = Mathf.Clamp(brakeSteering * steeringInputSmoothed, 0f, 1000f);
				UpdateColliders();
			}
			else // if (isRetracted)
			{
				averageTrackRPM = 0f;
				degreesPerTick = 0f;
				steeringAngle = 0f;
            	
				for (int i = 0; i < wcList.Count(); i++)
				{
					wcList[i].motorTorque = 0f;
					wcList[i].brakeTorque = 500f;
					wcList[i].steerAngle = 0f;
				}
			}
			
			smoothedTravel = Mathf.Lerp(smoothedTravel, currentTravel, Time.deltaTime * 2f);
			appliedTravel = smoothedTravel / 100f;
			
			susInc = KFPersistenceManager.suspensionIncrement;
		}
		
		/// <summary>Stuff that doesn't need to happen every physics frame.</summary>
		public void Update()
		{
			if (!isReady)
				return;
			commandId = vessel.referenceTransformId;
			if (!Equals(commandId, lastCommandId))
			{
				#if DEBUG
                KFLog.Log("Control Axis Changed.");
				#endif
                
				GetControlAxis();
			}
			vesselMass = vessel.GetTotalMass();
			if (!Equals(Math.Round(vesselMass, 1), Math.Round(lastVesselMass, 1)))
			{
				#if DEBUG
                KFLog.Log("Vessel mass changed.");
				#endif
                
				_colliderMass = ChangeColliderMass();
				lastPartCount = vessel.Parts.Count();
				ApplySteeringSettings();
			}
			lastCommandId = commandId;
			effectPower = Math.Abs(averageTrackRPM / maxRPM);
			WheelSound();
		}
		
		/// <summary>
		/// Applies calculated torque, braking and steering to the wheel colliders,
		/// gathers some information such as RPM and invokes the DustFX where appropriate.
		/// </summary>
		/// <remarks>This is a major chunk of what happens in FixedUpdate if the part is deployed.</remarks>
		void UpdateColliders()
		{
			float requestedResource;
			float unitLoad = 0f;
			
			float resourceConsumption = Time.deltaTime * resourceConsumptionRate * (Math.Abs(motorTorque) / 100f);
			requestedResource = part.RequestResource(resourceName, resourceConsumption);
			float freeWheelRPM = 0f;
            
			#if DEBUG
			KFLog.Log(string.Format("Requested Resource: \"{0}\" - Consumption: \"{1}\"", requestedResource, resourceConsumption));
			#endif
			
			if (requestedResource < resourceConsumption - 0.1f && !Equals(resourceConsumption, 0f))
			{
				motorTorque = 0f;
				status = statusLowResource;
			}
			else
				status = "Nominal";
			if (Math.Abs(averageTrackRPM) >= maxRPM)
			{
				motorTorque = 0f;
				status = "Rev Limit";
			}
			
			colliderLoad = 0f;
			for (int i = 0; i < wcList.Count(); i++)
			{
				WheelHit hit;
				bool grounded = wcList[i].GetGroundHit(out hit); //set up to pass out wheelhit coordinates 
				unitLoad += hit.force;
				
				wcList[i].motorTorque = motorTorque;
				wcList[i].brakeTorque = brakeTorque + brakeSteeringTorque + rollingFriction;
				wcList[i].mass = _colliderMass;
				
				if (wcList[i].isGrounded) 
				{
					groundedWheels++;
					trackRPM += wcList[i].rpm;
					colliderLoad += hit.force;
					if (KFPersistenceManager.isDustEnabled)
						_dustFX.WheelEmit(hit.point, hit.collider);
				}
				else if (!Equals(wcList[i].suspensionDistance, 0f))
                    freeWheelRPM += wcList[i].rpm;
				
				if (hasSteering)
					wcList[i].steerAngle = steeringAngle;
				wcList[i].suspensionDistance = suspensionDistance[i] * appliedTravel;
			}
			
			if (groundedWheels >= 1)
			{
				averageTrackRPM = trackRPM / groundedWheels;
				colliderLoad /= groundedWheels;
				rollingFriction = (rollingResistance.Evaluate((float)vessel.srfSpeed) * tweakScaleCorrector) + (loadCoefficient.Evaluate((float)colliderLoad) / tweakScaleCorrector);
			}
			else
				averageTrackRPM = freeWheelRPM / wheelCount;
			
			trackRPM = 0f;
			degreesPerTick = (averageTrackRPM / 60f) * Time.deltaTime * 360f;
			groundedWheels = 0;
		}
		
		/// <summary>Updates wheel colliders with current vessel mass.</summary>
		/// <remarks>This changes often in KSP, so can't simply be set once and forgotten!</remarks>
		/// <returns>New mass.</returns>
		public float ChangeColliderMass()
		{
			int colliderCount = 0;
			int partCount = vessel.parts.Count();
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
				
				_moduleWheelList[i]._colliderMass = colliderMass;
				_moduleWheelList[i].lastVesselMass = vesselMass;
				_moduleWheelList[i].vesselMass = vesselMass;
			}
			return colliderMass;
		}
		
		/// <summary>Disables tweakables when retracted.</summary>
		/// <param name="mode"></param>
		public void RetractDeploy(string mode)
		{
			switch (mode)
			{
				case "retract":
					isRetracted = true;
					currentTravel = 0f;
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
					currentTravel = rideHeight;
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
			if (Equals(param.type, KSPActionType.Activate))
			{
				brakeTorque = brakingTorque * ((torque / 2f) + .5f);
				brakesApplied = true;
			}
			else
			{
				brakeTorque = 0f;
				brakesApplied = false;
			}
		}
		
		[KSPAction("Increase Torque")]
		public void increase(KSPActionParam param)
		{
			if (torque < 2f)
				torque += 0.25f;
		}
		
		[KSPAction("Decrease Torque")]
		public void decrease(KSPActionParam param)
		{
			if (torque > 0f)
				torque -= 0.25f;
		}
		
		[KSPAction("Toggle Steering")]
		public void toggleSteering(KSPActionParam param)
		{
			steeringDisabled = !steeringDisabled;
		}
		
		[KSPAction("Invert Steering")]
		public void InvertSteeringAG(KSPActionParam param)
		{
			InvertSteering();
		}
		
		[KSPAction("Lower Suspension")]
		public void LowerRideHeight(KSPActionParam param)
		{
			if (rideHeight > 0f)
				rideHeight -= Mathf.Clamp(susInc, 0f, 100f);
			
			ApplySettings(true);
		}
		
		[KSPAction("Raise Suspension")]
		public void RaiseRideHeight(KSPActionParam param)
		{
			if (rideHeight < 100f)
				rideHeight += Mathf.Clamp(susInc, 0f, 100f);
			
			ApplySettings(true);
		}
		
		#region Presets
		/// <summary>Sets the rideHeight to the preset value specified.</summary>
		/// <param name="value">The height requested. (0-100 float)</param>
		void Presetter(float value)
		{
			rideHeight = Mathf.Clamp(value, 0f, 100f);
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
				if (!Equals(groupNumber, 0f) && Equals(groupNumber, mt.groupNumber))
				{
					mt.steeringRatio = WheelUtils.SetupRatios(mt.rootIndexLong, mt.part, vessel, groupNumber);
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
				if (!Equals(groupNumber, 0f) && Equals(groupNumber, mt.groupNumber) && !actionGroup)
				{
					currentTravel = rideHeight;
					mt.currentTravel = rideHeight;
					mt.rideHeight = rideHeight;
					mt.torque = torque;
				}
				if (actionGroup || Equals(groupNumber, 0f))
					currentTravel = rideHeight;
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
	}
}
