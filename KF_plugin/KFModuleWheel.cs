using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFModuleWheel : PartModule
    {
		// disable RedundantDefaultFieldInitializer
		
		// Name definitions
        public string right = "right";
        public string forward = "forward";
        public string up = "up";

		// Tweakables
        [KSPField(isPersistant = false, guiActive = true, guiName = "Wheel Settings")]
        public string settings = "";
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Group Number"), UI_FloatRange(minValue = 0, maxValue = 10f, stepIncrement = 1f)]
        public float groupNumber = 1;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Torque Ratio"), UI_FloatRange(minValue = 0, maxValue = 2f, stepIncrement = .25f)]
        public float torque = 1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Strength"), UI_FloatRange(minValue = 0, maxValue = 6.00f, stepIncrement = 0.2f)]
        public float springRate;        
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Damping"), UI_FloatRange(minValue = 0, maxValue = 1.0f, stepIncrement = 0.025f)]
        public float damperRate;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Suspension Travel"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 5)]
        public float rideHeight = 100;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steering"), UI_Toggle(disabledText = "Enabled", enabledText = "Disabled")]
        public bool steeringDisabled;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Start"), UI_Toggle(disabledText = "Deployed", enabledText = "Retracted")]
        public bool startRetracted;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string status = "Nominal";
        
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
		
		/// <summary>Constant brake value aplpied to simulate rolling resistance.</summary>
        [KSPField]
		public FloatCurve rollingResistance = new FloatCurve();
		
		/// <summary>Constant brake value aplpied to simulate rolling resistance.</summary>
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
		
		/// <summary>How fast to consume ElectricCharge.</summary>
        [KSPField]
		public float chargeConsumptionRate = 1f;

        /// <summary>Enables retraction of the suspension</summary>
        [KSPField]
        public bool hasRetract = false;

        /// <summary>DOes the part has an animation to be triggered when retracting</summary>
        [KSPField]
        public bool hasRetractAnimation = false;

        /// <summary>Name of the Bounds object</summary>
        [KSPField]
        public string boundsName = "Bounds";

        /// <summary>Name of the Bounds object</summary>
        [KSPField]
        public string orientationObjectName = "Default";

        /// <summary>I'm not sure</summary>
        [KSPField]
        public bool passivePart = false;

        /// <summary>Used for parts which use the module for passive functions</summary>
        [KSPField]
        public bool disableTweakables = false;

        //persistent fields
		/// <summary>Will be negative one (-1) if inverted.</summary>
        [KSPField(isPersistant = true)]
		public float steeringInvert = 1;
		
		/// <summary>Saves the brake state.</summary>
        [KSPField(isPersistant = true)]
		public bool brakesApplied;
        [KSPField(isPersistant = true)]
        public bool isRetracted = false;
        [KSPField(isPersistant = true, guiActive = false, guiName = "TS", guiFormat = "F1")] //debug only.
        public float tweakScaleCorrector = 1;

		// Global variables
        int rootIndexLong;      
        int rootIndexLat;
        int rootIndexUp;
        int controlAxisIndex;  
        uint commandId;
        uint lastCommandId;
        float brakeTorque;
        float brakeSteering;
        float motorTorque;
        int groundedWheels = 0; 
        float effectPower;
        float trackRPM = 0;
        float lastPartCount;

		// Stuff deliberately made available to other modules:
        public float steeringAngle;
        public float steeringAngleSmoothed;
        public float appliedRideHeight;
        public int wheelCount;
        
        public int directionCorrector = 1;
        public int steeringCorrector = 1;
        public float steeringRatio;
        public float degreesPerTick;
        public float currentRideHeight;
        public float smoothedRideHeight;

        //Visible fields
        [KSPField(isPersistant = false, guiActive = true, guiName = "Vessel Mass", guiFormat = "F1")]
        public float vesselMass;
        [KSPField(isPersistant = false, guiActive = true, guiName = "RPM", guiFormat = "F1")]
        public float averageTrackRPM;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Colliders", guiFormat = "F0")]
        public int _colliderCount = 0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Collider Mass", guiFormat = "F2")]
        public float _colliderMass = 0;


        public List<WheelCollider> wcList = new List<WheelCollider>();
        public ModuleAnimateGeneric retractionAnimation;
        
		/// <summary>This is the info string that will display when the part info is shown.</summary>
		/// <remarks>This can be overridden in the config for this module in the part file.</remarks>
		[KSPField]
		public string strInfo = "This part comes with enhanced steering and suspension.";
		public override string GetInfo()
		{
			return strInfo;
		}
		
		public override void OnStart(PartModule.StartState state)  //when started 
        {
			base.OnStart(state);

            _colliderMass = 10; //jsut a beginning value to stop stuff going crazy before it's all calculated properly.
            

            Vector3 partOrientationForward = new Vector3(0,0,0);
            Vector3 partOrientationRight = new Vector3(0, 0, 0);
            Vector3 partOrientationUp = new Vector3(0, 0, 0);

            if (!string.Equals(orientationObjectName, "Default"))
            {
                Debug.LogWarning("Default part orientation");
                partOrientationUp = transform.Search(orientationObjectName).up;
                partOrientationForward = transform.Search(orientationObjectName).forward;
                partOrientationRight = transform.Search(orientationObjectName).right;
            }
            else
            {
                Debug.LogWarning("Transformed part orientation");
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
                setupAnimation();
            }

            print(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            if (disableTweakables)
            {
                Debug.LogWarning("Disabling tweakables");
                foreach (BaseField k in this.Fields)
                {
					print(string.Format("found {0}", k.guiName));
                    k.guiActive = false;
                    k.guiActiveEditor = false;
                }
                foreach (BaseAction a in this.Actions)
                {
					print(string.Format("found {0}", a.guiName));
                    a.active = false;
                }
                foreach (BaseEvent e in this.Events)
                {
					print(string.Format("found {0}", e.guiName));
                    e.active = false;
                }
            }
            
            // disable once ConvertIfToOrExpression
            if (startRetracted)
                isRetracted = true;
			
            // disable once ConvertIfStatementToConditionalTernaryExpression
            if (!isRetracted)
                currentRideHeight = rideHeight; //set up correct values from persistence
            else
                currentRideHeight = 0;
			/* SharpDevelop reports that these two "if" checks can be replaced with single line checks. - Gaalidas ... WRONG ... actually, not wrong, but suppressing for now.
            isRetracted |= startRetracted;
            currentRideHeight = !isRetracted ? rideHeight : 0;
            smoothedRideHeight = currentRideHeight;
            appliedRideHeight = smoothedRideHeight / 100;
             */
            //print(appliedRideHeight);
           
			if (HighLogic.LoadedSceneIsEditor && !hasRetract)
            {
                Extensions.DisableAnimateButton(this.part);
                Actions["AGToggleDeployed"].active = false;
                Actions["Deploy"].active = false;
                Actions["Retract"].active = false;
                Fields["startRetracted"].guiActiveEditor = false;
            } 

            if (HighLogic.LoadedSceneIsFlight)
            {
                StartCoroutine(StartupStuff());
                maxRPM /= tweakScaleCorrector;
                startRetracted = false;
                if (!hasRetract)
                    Extensions.DisableAnimateButton(this.part);

				// Wheel steering ratio setup
                rootIndexLong = WheelUtils.GetRefAxis(partOrientationForward, this.vessel.rootPart.transform); //Find the root part axis which matches each wheel axis.
                rootIndexLat = WheelUtils.GetRefAxis(partOrientationRight, this.vessel.rootPart.transform);
                rootIndexUp = WheelUtils.GetRefAxis(partOrientationUp, this.vessel.rootPart.transform);

                steeringRatio = WheelUtils.SetupRatios(rootIndexLong, this.part, this.vessel, groupNumber); //use the axis which corresponds to the forward axis of the wheel.
				GetControlAxis();

				if (torque > 2) // Check if the torque value is using the old numbering system
                    torque /= 100;
				
                wheelCount = 0;
				this.part.force_activate(); // Force the part active or OnFixedUpate is not called
				foreach (WheelCollider wc in this.part.GetComponentsInChildren<WheelCollider>()) // Set colliders to values chosen in editor and activate
                {
                    wheelCount++;
                    JointSpring userSpring = wc.suspensionSpring;
                    userSpring.spring = springRate * tweakScaleCorrector;
                    userSpring.damper = damperRate * tweakScaleCorrector;
                    wc.suspensionSpring = userSpring;
                    wc.suspensionDistance = wc.suspensionDistance * appliedRideHeight;
                    wcList.Add(wc);
                    wc.enabled = true;
                    wc.gameObject.layer = 27;
                }
				
                if (brakesApplied)
					brakeTorque = brakingTorque; // Were the brakes left applied?
				
                if (isRetracted)
                    UpdateColliders("retract");
				
			} // End scene is flight
			DestroyBounds();
		}
		//end OnStart

        public void WheelSound()
        {
            part.Effect("WheelEffect", effectPower);
        }

        /// <summary>Stuff that needs to wait for the first physics frame. Maybe because this ensure the vessel is totally spawnedor physics is active</summary>
        IEnumerator StartupStuff()
        {
            yield return new WaitForFixedUpdate();
            lastPartCount = this.vessel.Parts.Count();
            print("Part Count " + lastPartCount);

            print("Checking vessel mass");
            _colliderMass = ChangeColliderMass();
        }


        public override void OnFixedUpdate()
        {
            vesselMass = this.vessel.GetTotalMass();
			// User input
            float electricCharge;
            float unitLoad = 0;
			float forwardTorque = torqueCurve.Evaluate((float)this.vessel.srfSpeed / tweakScaleCorrector) * torque; //this is used a lot, so may as well calculate once
            float steeringTorque;
            float brakeSteeringTorque;

            Vector3 travelVector = this.vessel.GetSrfVelocity();

            float travelDirection = Vector3.Dot(this.part.transform.forward, travelVector); //compare travel velocity with the direction the part is pointed.
            //print(travelDirection);

            if (!steeringDisabled)
            {
                steeringTorque = torqueSteeringCurve.Evaluate((float)this.vessel.srfSpeed / tweakScaleCorrector) * torque * steeringInvert; //low speed steering mode. Differential motor torque
                brakeSteering = brakeSteeringCurve.Evaluate(travelDirection) / tweakScaleCorrector * steeringInvert * torque; //high speed steering. Brake on inside track because Unity seems to weight reverse motor torque less at high speed.
                steeringAngle = (steeringCurve.Evaluate((float)this.vessel.srfSpeed)) * -this.vessel.ctrlState.wheelSteer * steeringRatio * steeringCorrector * steeringInvert; //low speed steering mode. Differential motor torque
            }
            else
            {
                steeringTorque = 0;
                brakeSteering = 0;
                steeringAngle = 0;
            }
    
            if (!isRetracted)
            {
                

                if (this.vessel.Parts.Count() != lastPartCount)
                {
                    print("vessel mass changed");
                    _colliderMass = ChangeColliderMass();
                    lastPartCount = this.vessel.Parts.Count();
                    ApplySteeringSettings();
                }

                motorTorque = (forwardTorque * directionCorrector * this.vessel.ctrlState.wheelThrottle) - (steeringTorque * this.vessel.ctrlState.wheelSteer); //forward and low speed steering torque. Direction controlled by precalulated directioncorrector
                brakeSteeringTorque = Mathf.Clamp(brakeSteering * this.vessel.ctrlState.wheelSteer, 0, 1000); //if the calculated value is negative, disregard: Only brake on inside track. no need to direction correct as we are using the velocity or the part not the vessel.

                float chargeConsumption = Time.deltaTime * chargeConsumptionRate * (Math.Abs(motorTorque) / 100);
                electricCharge = part.RequestResource("ElectricCharge", chargeConsumption);
                float freeWheelRPM = 0;

                if (!Equals(electricCharge, chargeConsumption))
                {
                    motorTorque = 0;
                    status = "Low Charge";
                }
                else if (Math.Abs(averageTrackRPM) >= maxRPM)
                {
                    motorTorque = 0;
                    status = "Rev Limit";
                }
                else
                    status = "Nominal";

				for (int i = 0; i < wcList.Count(); i++)
                {
				
                    WheelHit hit;
                    bool grounded = wcList[i].GetGroundHit(out hit); //set up to pass out wheelhit coordinates 
                    unitLoad += hit.force;
                    //unitLoad /= wcList.Count();
                    //unitLoad *= 10;
                    //print("unitLoad = " + unitLoad);
                    var rollingFriction = rollingResistance.Evaluate((float)this.vessel.srfSpeed) * loadCoefficient.Evaluate((float)unitLoad); // 
                    //print("rollingfriction = "+ rollingFriction);

                    wcList[i].motorTorque = motorTorque;
                    wcList[i].brakeTorque = brakeTorque + brakeSteeringTorque + rollingFriction;
                    wcList[i].mass = _colliderMass;

                    if (wcList[i].isGrounded) //only count wheels in contact with the floor. Others will be freewheeling and will wreck the calculation. 
                    {
                        groundedWheels++;
                        trackRPM += wcList[i].rpm;
                    }
					else if (!Equals(wcList[i].suspensionDistance, 0)) //the sprocket colliders could be doing anything. Don't count them.
                        freeWheelRPM += wcList[i].rpm;

					if (hasSteering)
                        wcList[i].steerAngle = steeringAngleSmoothed;
                }

                if (groundedWheels >= 1)
                    averageTrackRPM = trackRPM / groundedWheels;
                else
                    averageTrackRPM = freeWheelRPM / wheelCount;
				
                trackRPM = 0;
                degreesPerTick = (averageTrackRPM / 60) * Time.deltaTime * 360; //calculate how many degrees to rotate the wheel mesh
                groundedWheels = 0; //reset number of wheels.
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
            smoothedRideHeight = Mathf.Lerp(smoothedRideHeight, currentRideHeight, Time.deltaTime * 2);
            appliedRideHeight = smoothedRideHeight / 100;
            steeringAngleSmoothed = Mathf.Lerp(steeringAngleSmoothed, steeringAngle, Time.deltaTime * smoothSpeed);
            
		}
		//End OnFixedUpdate

        public override void OnUpdate()
        {
            base.OnUpdate();
            commandId = this.vessel.referenceTransformId;
			if (!Equals(commandId, lastCommandId))
            {
                print("Control Axis Changed");
                GetControlAxis();
            }
            lastCommandId = commandId;
            effectPower = Math.Abs(averageTrackRPM / maxRPM);
            WheelSound();
		}
		//end OnUpdate

        [KSPEvent(guiActive = true, guiName = "Find Modules", active = true)]
        public void ChangeColliderMassAction()
        {
            ChangeColliderMass();
        }

        public float ChangeColliderMass()
        {
            int colliderCount = 0;
            int partCount = this.vessel.parts.Count();
            var KFMWList = new List<KFModuleWheel>();

            for (int i = 0; i < partCount; i++)
            {
                var KFMW = this.vessel.parts[i].GetComponent<KFModuleWheel>();
                if (KFMW)
                {
                    KFMWList.Add(KFMW);
                    print("found KFModuleWheel in " + this.vessel.parts[i].partInfo.name);
                    
                    colliderCount += KFMW.wcList.Count();
                }
            }
            float colliderMass = this.vessel.GetTotalMass() / colliderCount;
            print(colliderMass);

            /// set all this up in the other wheels to prevent them having to do so themselves. First part has the honour.
            for (int i = 0; i < KFMWList.Count(); i++)
            {
                    print("setting collidermass in other wheel " + this.vessel.parts[i].partInfo.name);

                    KFMWList[i]._colliderMass = colliderMass;
                    KFMWList[i].lastPartCount = partCount; //this should mean that the method does not get triggered for subsequent wheels.
            }
            
            return colliderMass;
        }

        public void UpdateColliders(string mode)
        {
			// I am unsure if this is more efficient or what, but this format was suggested. - Gaalidas
			switch (mode)
			{
				case "retract":
                	isRetracted = true;
                	currentRideHeight = 0;
                	Events["applySettingsGUI"].guiActive = false;
                    Events["ApplySteeringSettings"].guiActive = false;
                	Events["InvertSteering"].guiActive = false;
                	Fields["rideHeight"].guiActive = false;
                	Fields["torque"].guiActive = false;
                	//Fields["springRate"].guiActive = false;
                	//Fields["damperRate"].guiActive = false;
                	Fields["steeringDisabled"].guiActive = false;
                	status = "Retracted";
					break;
				case "deploy":
                	isRetracted = false;
                	currentRideHeight = rideHeight;
                	Events["applySettingsGUI"].guiActive = true;
                    Events["ApplySteeringSettings"].guiActive = true;
                	Fields["rideHeight"].guiActive = true;
                	Fields["torque"].guiActive = true;
                	//Fields["springRate"].guiActive = true;
                	//Fields["damperRate"].guiActive = true;
                	Fields["steeringDisabled"].guiActive = true;
                	status = "Nominal";
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

        public void setupAnimation()
        {
            retractionAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault();
        }

        public void PlayAnimation()
        {
            // note: assumes one ModuleAnimateGeneric (or derived version) for this part
            // if this isn't the case, needs fixing
            
            if (!retractionAnimation)
                return; //the Log.Error line fails syntax check with 'The name 'Log' does not appear in the current context.

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
                print("destroying Bounds");
            }
        }

        //Action groups
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
        
        [KSPEvent(guiActive = true, guiName = "Invert Steering", active = true)]
        public void InvertSteering()
        {
            steeringInvert *= -1;
        }

        [KSPEvent(guiActive = true, guiName = "Apply Wheel Settings", active = true)]
        public void applySettingsGUI()
        {
            ApplySettings(false);
        }
        
        public void ApplySettings(bool actionGroup)
        {
			foreach (KFModuleWheel mt in vessel.FindPartModulesImplementing<KFModuleWheel>())
            {
				if (!Equals(groupNumber, 0) && Equals(groupNumber, mt.groupNumber) && !actionGroup)
                {
                    currentRideHeight = rideHeight;
                    mt.currentRideHeight = rideHeight;
                    mt.rideHeight = rideHeight;
                    mt.torque = torque;
                }
				if (actionGroup || Equals(groupNumber, 0))
                    currentRideHeight = rideHeight;
            }
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

				UpdateColliders("deploy");
            }
        }

        [KSPAction("Retract")]
        public void Retract(KSPActionParam param)
        {
			if (!isRetracted)
            {
                if (hasRetractAnimation)
                    PlayAnimation();

				UpdateColliders("retract");
            }
		}

        //Addons by Gaalidas
        [KSPAction("Lower Suspension")]
        public void LowerRideHeight(KSPActionParam param)
        {
            if (rideHeight > 0)
                rideHeight -= 5;

			ApplySettings(true);
        }
  
        [KSPAction("Raise Suspension")]
        public void RaiseRideHeight(KSPActionParam param)
        {
            if (rideHeight < 100)
                rideHeight += 5;
			
			ApplySettings(true);
        }

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
	}
	//end class
}
//end namespaces
