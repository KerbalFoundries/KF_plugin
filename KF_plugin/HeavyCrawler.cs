/*
 * KSP [0.23.5] Anti-Grav Repulsor plugin by Lo-Fi
 * Much inspiration and a couple of code snippets for deployment taken from BahamutoD's Critter Crawler mod. Huge respect, it's a fantastic mod :)
 * 
 */

using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    [KSPModule("HeavyCrawler")]
    public class HeavyCrawler : PartModule
    {
    	/// <summary>Container for wheelcollider we grab from wheelmodule.</summary>
        public WheelCollider thiswheelCollider;
        public WheelCollider mywc;
        public JointSpring userspring;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Ride Height"), UI_FloatRange(minValue = 0, maxValue = 2.00f, stepIncrement = 0.25f)]
        public float Rideheight;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring strength"), UI_FloatRange(minValue = 0, maxValue = 3.00f, stepIncrement = 0.2f)]
        public float SpringRate;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Damping"), UI_FloatRange(minValue = 0, maxValue = 1.00f, stepIncrement = 0.025f)]
        public float DamperRate;

        public float TargetPosition;
        
		// Steeringstuff
        public Transform steeringFound;
        public Transform smoothSteering;

        public float smoothSpeed = 40f;
        [KSPField(isPersistant = true)]
        public Vector3 thisTransform;

        public float rearWheel;
        public float frontWheel;
        public float frontToBack;
        public float midToFore;
        public float offset;
		public float myPositionx;
		public float myPositionz;
        public float myPosition;
        public float myAdjustedPosition;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Steering Ratio", guiFormat = "F1")]
        public float steeringRatio;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Steering Ratio.Abs", guiFormat = "F1")]
        public float steeringRatioAbsolute;
        [KSPField]
		public float angle = 0.5f;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Steering Angle", guiFormat = "F1")]
        public float steeringAngle;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Crab Angle", guiFormat = "F1")]
        public float crabAngle;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Rotation Angle", guiFormat = "F1")]
        public float rotationAngle;

		// Begin start
		public override void OnStart(PartModule.StartState state)  // When started
        {
			thiswheelCollider = part.gameObject.GetComponentInChildren<WheelCollider>();   // Find the 'wheelCollider' gameobject named by KSP convention.
            mywc = thiswheelCollider.GetComponent<WheelCollider>();
            userspring = mywc.suspensionSpring;
            // degub only: print("onstart");
            base.OnStart(state);
            
            //print(steeringFound);
            smoothSteering = transform.Search("smoothSteering");

            print("start called");

            if (HighLogic.LoadedSceneIsEditor)
            {
				if (Equals(SpringRate, 0)) // Check if a value exists already. This is important, because if a wheel has been tweaked from the default value, we will overwrite it!
                {
                    print("part creation");
                    //thiswheelCollider = part.gameObject.GetComponentInChildren<WheelCollider>();   //find the 'wheelCollider' gameobject named by KSP convention.
                    //mywc = thiswheelCollider.GetComponent<WheelCollider>();     //pull collider properties
                    userspring = mywc.suspensionSpring;         //set up jointspring to modify spring property
                    SpringRate = mywc.suspensionSpring.spring;              //pass to springrate to be used in the GUI
                    DamperRate = mywc.suspensionSpring.damper;
                    Rideheight = mywc.suspensionDistance;
                    TargetPosition = mywc.suspensionSpring.targetPosition;
                }
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
				this.part.force_activate(); // Force the part active or OnFixedUpate is not called
				// Start of initial proportional steering routine

                myPosition = this.part.orgPos.y;
                myPositionx = this.part.orgPos.x;
                myPositionz = this.part.orgPos.z;

				// Find positions
                frontWheel = this.part.orgPos.y; //values for forwe and aftmost wheels
                rearWheel = this.part.orgPos.y;

                foreach (HeavyCrawler st in this.vessel.FindPartModulesImplementing<HeavyCrawler>()) //scan vessel to find fore or rearmost wheel. 
                {
                    if ((st.part.orgPos.y + 1000) >= (frontWheel + 1000)) //dodgy hack. Make sure all values are positive or we struggle to evaluate < or >
                    {
                        frontWheel = st.part.orgPos.y; //Store transform y value
                        //print(st.part.orgPos.y);
                    }
                    if ((st.part.orgPos.y + 1000) <= (rearWheel + 1000))
                    {
                        rearWheel = st.part.orgPos.y; //Store transform y value
                        //print(st.part.orgPos.y);
                    }
                }

				// Grab this one to compare
                frontToBack = frontWheel - rearWheel; //distance front to back wheel
                midToFore = frontToBack / 2;
                offset = (frontWheel + rearWheel) / 2; //here is the problem

                myAdjustedPosition = myPosition - offset; //change the value based on our calculated offset 
                steeringRatio = myAdjustedPosition / midToFore; // this sets how much this wheel steers as a proportion of rear/front wheel steering angle
                steeringRatioAbsolute = Math.Abs(steeringRatio);
                //if (steeringRatio < 0) //this just changes all values to positive
                //{
                  //  steeringRatio /= -1; //if it's negative
                //}
			}
			// End isInFlight
		}
		// End start

        public override void OnFixedUpdate()
        {
            //smoothSteering.transform.rotation = Quaternion.Lerp(steeringFound.transform.rotation, smoothSteering.transform.rotation, Time.deltaTime * smoothSpeed);
			// Above is original code for smoothing steering input. Depracated.
            bool left = Input.GetKey(KeyCode.A);
            bool right = Input.GetKey(KeyCode.D);
            bool crabLeft = Input.GetKey(KeyCode.J);
            bool crabRight = Input.GetKey(KeyCode.L);
            
            if (left && steeringAngle >= -90)
            {
                //smoothSteering.Rotate(Vector3.up, -(angle*steeringRatio));
                steeringAngle -= angle;
            }
            if (right && steeringAngle <= 90)
            {
                //smoothSteering.Rotate(Vector3.up, (angle*steeringRatio));
                steeringAngle += angle;
            }
            if (crabLeft)
            {
                crabAngle -= angle;
                //smoothSteering.transform.Rotate(Vector3.up, -angle);
            }
            if (crabRight)
            {
                crabAngle += angle;
                //smoothSteering.transform.Rotate(Vector3.up, angle);
            }
			rotationAngle = crabAngle + (((steeringAngle * (float)Math.Cos(Mathf.Deg2Rad * crabAngle)) * steeringRatio) + ((steeringAngle * (float)Math.Sin(Mathf.Deg2Rad * crabAngle) * 0.2f)) * steeringRatioAbsolute);
            Vector3 tempVector = smoothSteering.transform.localEulerAngles;
            tempVector.y = rotationAngle;
            smoothSteering.transform.localEulerAngles = tempVector;
		}
		// End OnFixedUpdate
	}
	// End class
}
// End namespace
