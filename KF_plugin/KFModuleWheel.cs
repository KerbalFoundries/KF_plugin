using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFModuleWheel : PartModule
    {
        // disable UnusedParameter
		// disable RedundantDefaultFieldInitializer
		// disable RedundantThisQualifier
        // disable ConvertIfToOrExpression
        // disable ConvertIfStatementToConditionalTernaryExpression
		
		// Name definitions
        const string right = "right";
        const string forward = "forward";
        const string up = "up";

		// Tweakables
        [KSPField(isPersistant = false, guiActive = true, guiName = "Wheel Settings")]
        public string settings = string.Empty;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Group Number"), UI_FloatRange(minValue = 0, maxValue = 10f, stepIncrement = 1f)]
        public float groupNumber = 1f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Torque Ratio"), UI_FloatRange(minValue = 0, maxValue = 2f, stepIncrement = .25f)]
        public float torque = 1f;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Strength"), UI_FloatRange(minValue = 0, maxValue = 6.00f, stepIncrement = 0.2f)]
        public float springRate;        
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Damping"), UI_FloatRange(minValue = 0, maxValue = 1.0f, stepIncrement = 0.025f)]
        public float damperRate;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Suspension Travel"), UI_FloatRange(minValue = 0, maxValue = 100f, stepIncrement = 5f)]
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
		public bool hasSteering = false;
		
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
		public float smoothSpeed = 10;
		
		/// <summary>Used to compensate for error in the raycast.</summary>
        [KSPField]
		public float raycastError;
		
		/// <summary>Rev limiter. Stops freewheel runaway and sets a speed limit.</summary>
        [KSPField]
		public float maxRPM = 350;

        /// <summary>How fast to consume the requested resource.</summary>
        /// <remarks>Default: 1.0</remarks>
        [KSPField]
		public float resourceConsumptionRate = 1f;

        /// <summary>Enables retraction of the suspension.</summary>
        [KSPField]
        public bool hasRetract = false;

        /// <summary>Does the part have an animation to be triggered when retracting.</summary>
        [KSPField]
        public bool hasRetractAnimation = false;

        /// <summary>Name of the Bounds object.</summary>
        [KSPField]
        public string boundsName = "Bounds";

        /// <summary>Name of the orientation object.</summary>
        [KSPField]
        public string orientationObjectName = "Default";

        /// <summary>Is the part passive or not?</summary>
        [KSPField]
        public bool passivePart = false;

        /// <summary>Used for parts which use the module for passive functions.</summary>
        [KSPField]
        public bool disableTweakables = false;

        /// <summary>Name of the resource being requested.</summary>
        /// <remarks>Default: ElectricCharge</remarks>
        [KSPField]
		public string resourceName = "ElectricCharge";
		
        /// <summary>Status text for the "Low Resource" state.</summary>
		public string statusLowResource = "Low Charge";
		public string statusNominal = "Nominal";
		public string statusRetracted = "Retracted";

        //persistent fields
		/// <summary>Will be negative one (-1) if inverted.</summary>
        [KSPField(isPersistant = true)]
		public float steeringInvert = 1;
		
		/// <summary>Saves the brake state.</summary>
        [KSPField(isPersistant = true)]
		public bool brakesApplied;
		
		/// <summary>Saves the retracted state.</summary>
        [KSPField(isPersistant = true)]
        public bool isRetracted = false;

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

        int groundedWheels = 0;
        float effectPower;
        float trackRPM = 0;
        float lastPartCount;
        float steeringInputSmoothed;
        float throttleInputSmoothed;
        float brakeSteeringTorque;

		// Stuff deliberately made available to other modules:
        public float steeringAngle;
        //public float steeringAngleSmoothed;
        public float appliedTravel;
        public int wheelCount;
        
        public int directionCorrector = 1;
        public int steeringCorrector = 1;
        public float steeringRatio;
        public float degreesPerTick;
        public float currentTravel;
        public float smoothedTravel;
		public float susInc;

        //Visible fields (debug)
        [KSPField(isPersistant = true, guiActive = false, guiName = "TS", guiFormat = "F1")] //debug only.
        public float tweakScaleCorrector = 1;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Last Vessel Mass", guiFormat = "F1")]
        public float lastVesselMass;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Vessel Mass", guiFormat = "F1")]
        public float vesselMass;
        
        [KSPField(isPersistant = false, guiActive = false, guiName = "Colliders", guiFormat = "F0")]
        public int _colliderCount = 0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Collider Mass", guiFormat = "F2")]
        public float _colliderMass = 0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Collider load", guiFormat = "F3")]
        public float colliderLoad = 0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Rolling Friction", guiFormat = "F3")]
        public float rollingFriction;

        public List<WheelCollider> wcList = new List<WheelCollider>();
        List<float> suspensionDistance = new List<float>();
        ModuleAnimateGeneric retractionAnimation;
        KFDustFX _dustFX;
        
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
			
			CustomResourceTextSetup(); // Calls a method to set up the statusLowResource text for resource alternatives.

            _colliderMass = 10; //just a beginning value to stop stuff going crazy before it's all calculated properly.
            
            var partOrientationForward = new Vector3(0,0,0);
            var partOrientationRight = new Vector3(0, 0, 0);
            var partOrientationUp = new Vector3(0, 0, 0);

            if (!string.Equals(orientationObjectName, "Default"))
            {
                KFLog.Warning("Transformed part orientation.");
                partOrientationUp = transform.Search(orientationObjectName).up;
                partOrientationForward = transform.Search(orientationObjectName).forward;
                partOrientationRight = transform.Search(orientationObjectName).right;
            }
            else
            {
                KFLog.Warning("Default part orientation.");
                partOrientationUp = this.part.transform.up;
                partOrientationForward = this.part.transform.forward;
                partOrientationRight = this.part.transform.right;
            }

            if (hasRetractAnimation)
            {
                foreach (ModuleAnimateGeneric ma in this.part.FindModulesImplementing<ModuleAnimateGeneric>())
                {
                    ma.Actions["ToggleAction"].active = false;
                    ma.Events["Toggle"].guiActive = false;
                    ma.Events["Toggle"].guiActiveEditor = false;
                }
                SetupAnimation();
            }

            KFLog.Log(string.Format("Version: {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version));

            //disables tweakables if being used on a passive part (mecannum wheel or skid, for example)
            if (disableTweakables)
            {
                KFLog.Warning("Disabling tweakables.");
                foreach (BaseField k in this.Fields)
                {
					KFLog.Log(string.Format("Found {0}", k.guiName));
                    k.guiActive = false;
                    k.guiActiveEditor = false;
                }
                foreach (BaseAction a in this.Actions)
                {
					KFLog.Log(string.Format("Found {0}", a.guiName));
                    a.active = false;
                }
                foreach (BaseEvent e in this.Events)
                {
					KFLog.Log(string.Format("Found {0}", e.guiName));
                    e.active = false;
                }
            }
            
            if (startRetracted)
                isRetracted = true;
			
            if (!isRetracted)
                currentTravel = rideHeight; //set up correct values from persistence
            else
                currentTravel = 0;
            //KFLog.Log(string.Format("\"appliedRideHeight\" = {0}", appliedRideHeight));
            
            // Disable retract tweakables if retract option not specified.
			if (HighLogic.LoadedSceneIsEditor && !hasRetract)
            {
                Extensions.DisableAnimateButton(this.part);
                Actions["AGToggleDeployed"].active = false;
                Actions["Deploy"].active = false;
                Actions["Retract"].active = false;
                Fields["startRetracted"].guiActiveEditor = false;
            } 

            if (HighLogic.LoadedSceneIsFlight && !Equals(vessel.vesselType, VesselType.Debris)) // && vessel.parts.Count > 1) // Vessels don't have to be made up of only one part to still be considered debris.
            {
                _dustFX = this.part.gameObject.GetComponent<KFDustFX>();
                if (Equals(_dustFX, null)) //add if not... sets some defaults.
                {
                    _dustFX = this.part.gameObject.AddComponent<KFDustFX>();
                    _dustFX.OnStart(state);
                }
                _dustFX.tweakScaleFactor = tweakScaleCorrector;

                appliedTravel = rideHeight / 100; // need to be here if no KFWheel or everything gets set to zero as below.
                StartCoroutine(StartupStuff());
                maxRPM /= tweakScaleCorrector;
                startRetracted = false;
                if (!hasRetract)
                    Extensions.DisableAnimateButton(this.part);

				// Wheel steering ratio setup
                rootIndexLong = WheelUtils.GetRefAxis(this.part.transform.forward, this.vessel.rootPart.transform); //Find the root part axis which matches each wheel axis.
                rootIndexLat = WheelUtils.GetRefAxis(this.part.transform.right, this.vessel.rootPart.transform);
                rootIndexUp = WheelUtils.GetRefAxis(this.part.transform.up, this.vessel.rootPart.transform);

                steeringRatio = WheelUtils.SetupRatios(rootIndexLong, this.part, this.vessel, groupNumber); //use the axis which corresponds to the forward axis of the wheel.
				GetControlAxis();

				if (torque > 2) // Check if the torque value is using the old numbering system
                    torque /= 100;
				
                wheelCount = 0;
				
				foreach (WheelCollider wc in this.part.GetComponentsInChildren<WheelCollider>()) // Set colliders to values chosen in editor and activate
                {
                    wheelCount++;
                    JointSpring userSpring = wc.suspensionSpring;
                    userSpring.spring = springRate * tweakScaleCorrector;
                    userSpring.damper = damperRate * tweakScaleCorrector;
                    wc.suspensionSpring = userSpring;
                    wc.suspensionDistance = wc.suspensionDistance * appliedTravel;
                    wcList.Add(wc);
                    suspensionDistance.Add(wc.suspensionDistance);
                    wc.enabled = true;
                    wc.gameObject.layer = 27;
                }
				
                if (brakesApplied)
					brakeTorque = brakingTorque; // Were the brakes left applied?
				
                if (isRetracted)
                    RetractDeploy("retract");
                isReady = true;
			} // End scene is flight
			DestroyBounds();
		}
		//end OnStart

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
            lastPartCount = this.vessel.Parts.Count();
			KFLog.Log(string.Format("Part Count {0}", lastPartCount));
            KFLog.Log("Checking vessel mass.");
            _colliderMass = ChangeColliderMass();
        }

        /// <summary>Physics critical stuff.</summary>
        public void FixedUpdate()
        {
            if (!isReady)
                return;
			// User input
            float steeringTorque;
            float brakeSteering;
			float forwardTorque = torqueCurve.Evaluate((float)this.vessel.srfSpeed / tweakScaleCorrector) * torque * tweakScaleCorrector; //this is used a lot, so may as well calculate once

            throttleInputSmoothed = Mathf.Lerp(throttleInputSmoothed, this.vessel.ctrlState.wheelThrottle + this.vessel.ctrlState.wheelThrottleTrim, smoothSpeed * Time.deltaTime);
            steeringInputSmoothed = (float)Math.Round(Mathf.Lerp(steeringInputSmoothed, this.vessel.ctrlState.wheelSteer + this.vessel.ctrlState.wheelSteerTrim, smoothSpeed * Time.deltaTime),3); // rounding is make sure lerp does return to zero.

            //FIXME - needs to take into account the possibilkty of off-axis parts with named orientationObject!!!!
            float travelDirection = Vector3.Dot(this.part.transform.forward, this.vessel.GetSrfVelocity()); //compare travel velocity with the direction the part is pointed.
            
            if (!steeringDisabled)
            {
                steeringTorque = torqueSteeringCurve.Evaluate((float)this.vessel.srfSpeed * tweakScaleCorrector) * torque * steeringInvert; //low speed steering mode. Differential motor torque
                brakeSteering = brakeSteeringCurve.Evaluate(travelDirection) * tweakScaleCorrector * steeringInvert * torque; //high speed steering. Brake on inside track because Unity seems to weight reverse motor torque less at high speed.
                steeringAngle = (steeringCurve.Evaluate((float)this.vessel.srfSpeed)) * -steeringInputSmoothed * steeringRatio * steeringCorrector * steeringInvert; //steer by turning wheel colliders
            }
            else
            {
                steeringTorque = 0;
                brakeSteering = 0;
                steeringAngle = 0;
            }
    
            if (!isRetracted)
            {
                motorTorque = Mathf.Clamp((forwardTorque * directionCorrector * throttleInputSmoothed) - (steeringTorque * steeringInputSmoothed), -forwardTorque, forwardTorque); //forward and low speed steering torque. Direction controlled by precalulated directioncorrector
                brakeSteeringTorque = Mathf.Clamp(brakeSteering * steeringInputSmoothed, 0, 1000); //if the calculated value is negative, disregard: Only brake on inside track. no need to direction correct as we are using the velocity or the part not the vessel.

                UpdateColliders();
            }
			else // if (isRetracted)
            {
                averageTrackRPM = 0;
                degreesPerTick = 0;
                steeringAngle = 0;
            
                for (int i = 0; i < wcList.Count(); i++)
                {
                    wcList[i].motorTorque = 0;
                    wcList[i].brakeTorque = 500;
                    wcList[i].steerAngle = 0;
                }
            }

            smoothedTravel = Mathf.Lerp(smoothedTravel, currentTravel, Time.deltaTime * 2);
            appliedTravel = smoothedTravel / 100;
			
			susInc = KFPersistenceManager.suspensionIncrement;
		}
		//End OnFixedUpdate

        /// <summary>Stuff that doesn't need to happen every physics frame.</summary>
        public void Update()
        {
            if (!isReady)
                return;
            commandId = this.vessel.referenceTransformId;
			if (!Equals(commandId, lastCommandId))
            {
                //KFLog.Log("Control Axis Changed.");
                GetControlAxis();
            }
            vesselMass = this.vessel.GetTotalMass();
            if (!Equals(Math.Round(vesselMass, 1), Math.Round(lastVesselMass, 1)))
            {
                //KFLog.Log("Vessel mass changed.");
                _colliderMass = ChangeColliderMass();
                lastPartCount = this.vessel.Parts.Count();
                ApplySteeringSettings();
            }
            lastCommandId = commandId;
            effectPower = Math.Abs(averageTrackRPM / maxRPM);
            WheelSound();
		}
		//end OnUpdate

        /// <summary>
        /// Applies calculated torque, braking and steering to the wheel colliders,
        /// gathers some information such as RPM and invokes the DustFX where appropriate.
        /// </summary>
        /// <remarks>This is a major chunk of what happens in FixedUpdate if the part is deployed.</remarks>
        void UpdateColliders()
        {
            float requestedResource;
            float unitLoad = 0;
           
            float resourceConsumption = Time.deltaTime * resourceConsumptionRate * (Math.Abs(motorTorque) / 100);
            requestedResource = part.RequestResource(resourceName, resourceConsumption);
            float freeWheelRPM = 0;
			//KFLog.Log(string.Format("{0} {1}", requestedResource, resourceConsumption));
            if (requestedResource < resourceConsumption - 0.1f)// && resourceConsumption != 0)
            {
                motorTorque = 0;
                status = statusLowResource;
            }
            else
                status = "Nominal";
            if (Math.Abs(averageTrackRPM) >= maxRPM)
            {
                motorTorque = 0;
                status = "Rev Limit";
            }

            colliderLoad = 0;
            for (int i = 0; i < wcList.Count(); i++)
            {

                WheelHit hit;
                bool grounded = wcList[i].GetGroundHit(out hit); //set up to pass out wheelhit coordinates 
                unitLoad += hit.force;

                wcList[i].motorTorque = motorTorque;
                wcList[i].brakeTorque = brakeTorque + brakeSteeringTorque + rollingFriction;
                wcList[i].mass = _colliderMass;

                if (wcList[i].isGrounded) //only count wheels in contact with the floor. Others will be freewheeling and will wreck the calculation. 
                {
                    groundedWheels++;
                    trackRPM += wcList[i].rpm;
                    colliderLoad += hit.force;
                    if (KFPersistenceManager.isDustEnabled)
                    	_dustFX.WheelEmit(hit.point, hit.collider);
                }
                else if (!Equals(wcList[i].suspensionDistance, 0)) //the sprocket colliders could be doing anything. Don't count them.
                    freeWheelRPM += wcList[i].rpm;

                if (hasSteering)
                    wcList[i].steerAngle = steeringAngle;
                wcList[i].suspensionDistance = suspensionDistance[i] * appliedTravel; //sets suspension distance
            }

            if (groundedWheels >= 1)
            {
                averageTrackRPM = trackRPM / groundedWheels;
                colliderLoad /= groundedWheels;
                rollingFriction = (rollingResistance.Evaluate((float)this.vessel.srfSpeed) * tweakScaleCorrector) + (loadCoefficient.Evaluate((float)colliderLoad) / tweakScaleCorrector);
            }
            else
                averageTrackRPM = freeWheelRPM / wheelCount;

            trackRPM = 0;
            degreesPerTick = (averageTrackRPM / 60) * Time.deltaTime * 360; //calculate how many degrees to rotate the wheel mesh
            groundedWheels = 0; //reset number of wheels.
        }

        /// <summary>
        /// Updates wheel colliders with current vessel mass.
        /// </summary>
        /// <remarks>This changes often in KSP, so can't simply be set once and forgotten!</remarks>
        /// <returns></returns>
        public float ChangeColliderMass()
        {
            int colliderCount = 0;
            int partCount = this.vessel.parts.Count();
            var KFMWList = new List<KFModuleWheel>();

            for (int i = 0; i < partCount; i++)
            {
                var KFMW = this.vessel.parts[i].GetComponent<KFModuleWheel>();
                if (!Equals(KFMW, null) )
                {
                    KFMWList.Add(KFMW);
					//KFLog.Log(string.Format("Found KFModuleWheel in {0}.", this.vessel.parts[i].partInfo.name));
                    
                    colliderCount += KFMW.wcList.Count();
                }
            }
            float colliderMass = this.vessel.GetTotalMass() / colliderCount;
            KFLog.Log(string.Format("colliderMass: {0}", colliderMass));

            // set all this up in the other wheels to prevent them having to do so themselves. First part has the honour.
            for (int i = 0; i < KFMWList.Count(); i++)
            {
				//KFLog.Log(string.Format("Setting collidermass in other wheel {0}.", KFMWList[i].part.partInfo.name));
				KFMWList[i]._colliderMass = colliderMass;
				KFMWList[i].lastVesselMass = this.vesselMass; //this should mean that the method does not get triggered for subsequent wheels.
                KFMWList[i].vesselMass = this.vesselMass;
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
                	currentTravel = 0;
                	Events["applySettingsGUI"].guiActive = false;
                    Events["ApplySteeringSettings"].guiActive = false;
                	Events["InvertSteering"].guiActive = false;
                	Fields["rideHeight"].guiActive = false;
                	Fields["torque"].guiActive = false;
                	//Fields["springRate"].guiActive = false;
                	//Fields["damperRate"].guiActive = false;
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
                	//Fields["springRate"].guiActive = true;
                	//Fields["damperRate"].guiActive = true;
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
            controlAxisIndex = WheelUtils.GetRefAxis(this.part.transform.forward, this.vessel.ReferenceTransform); //grab current values for the part and the control module, which may ahve changed.
            directionCorrector = WheelUtils.GetCorrector(this.part.transform.forward, this.vessel.ReferenceTransform, controlAxisIndex); // dets the motor direction correction again.
			if (Equals(controlAxisIndex, rootIndexLat))       //uses the precalulated forward (as far as this part is concerned) to determined the orientation of the control module
                steeringCorrector = WheelUtils.GetCorrector(this.vessel.ReferenceTransform.up, this.vessel.rootPart.transform, rootIndexLat);
			if (Equals(controlAxisIndex, rootIndexLong))
                steeringCorrector = WheelUtils.GetCorrector(this.vessel.ReferenceTransform.up, this.vessel.rootPart.transform, rootIndexLong);
			if (Equals(controlAxisIndex, rootIndexUp))
                steeringCorrector = WheelUtils.GetCorrector(this.vessel.ReferenceTransform.up, this.vessel.rootPart.transform, rootIndexUp);
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
            // note: assumes one ModuleAnimateGeneric (or derived version) for this part
            // if this isn't the case, needs fixing
            
            if (!retractionAnimation)
                return; //the Log.Error line fails syntax check when 'The name 'Log' does not appear in the current context.
            retractionAnimation.Toggle();
        }

		/// <summary>Destroys the Bounds helper object if it is still in the model.</summary>
        public void DestroyBounds()
        {
            Transform bounds = transform.Search(boundsName);
			if (!Equals(bounds, null))
            {
				UnityEngine.Object.Destroy(bounds.gameObject);
                //boundsDestroyed = true; //remove the bounds object to let the wheel colliders take over
                KFLog.Log("Destroying Bounds.");
            }
        }

        #region Action groups
        
        [KSPAction("Brakes", KSPActionGroup.Brakes)]
        public void brakes(KSPActionParam param)
        {
			if (Equals(param.type, KSPActionType.Activate))
            {
                brakeTorque = brakingTorque * ((torque / 2) + .5f);
                brakesApplied = true;
            }
            else
            {
                brakeTorque = 0;
                brakesApplied = false;
            }
        }

        [KSPAction("Increase Torque")]
        public void increase(KSPActionParam param)
        {
            if (torque < 2)
                torque += 0.25f;
        }

        [KSPAction("Decrease Torque")]
        public void decrease(KSPActionParam param)
        {
            if (torque > 0)
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
            if (rideHeight > 0)
                rideHeight -= Mathf.Clamp(susInc, 0f, 100f);

            ApplySettings(true);
        }

        [KSPAction("Raise Suspension")]
        public void RaiseRideHeight(KSPActionParam param)
        {
            if (rideHeight < 100)
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
			Presetter(0);
        }
        [KSPAction("Suspension 25")]
        public void SuspQuarter(KSPActionParam param)
        {
			Presetter(25);
        }
        [KSPAction("Suspension 50")]
        public void SuspFifty(KSPActionParam param)
        {
			Presetter(50);
        }
        [KSPAction("Suspension 75")]
        public void SuspThreeQuarter(KSPActionParam param)
        {
			Presetter(75);
        }
        [KSPAction("Suspension 100")]
        public void SuspFull(KSPActionParam param)
        {
			Presetter(100);
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
            steeringInvert *= -1;
        }

        [KSPEvent(guiActive = true, guiName = "Apply Wheel Settings", active = true)]
        public void ApplySettingsGUI()
        {
            ApplySettings(false);
        }

        [KSPEvent(guiActive = true, guiName = "Apply Steering Settings", active = true)]
        public void ApplySteeringSettings()
        {
            foreach (KFModuleWheel mt in this.vessel.FindPartModulesImplementing<KFModuleWheel>())
            {
                if (!Equals(groupNumber, 0) && Equals(groupNumber, mt.groupNumber))
                {
                    mt.steeringRatio = WheelUtils.SetupRatios(mt.rootIndexLong, mt.part, this.vessel, groupNumber);
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
				if (!Equals(groupNumber, 0) && Equals(groupNumber, mt.groupNumber) && !actionGroup)
                {
                    currentTravel = rideHeight;
                    mt.currentTravel = rideHeight;
                    mt.rideHeight = rideHeight;
                    mt.torque = torque;
                }
				if (actionGroup || Equals(groupNumber, 0))
                    currentTravel = rideHeight;
            }
        }

        //Addons by Gaalidas
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
	//end class
}
//end namespace
