using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public static class Extensions
    {
        // disable EmptyGeneralCatchClause
        
        public static Transform Search(this Transform target, string name)
        {
			if (Equals(target.name, name))
				return target;

            for (int i = 0; i < target.childCount; ++i)
            {
                var result = Search(target.GetChild(i), name);
				if (!Equals(result, null))
					return result;
            }
            return null;
        }

        public static Transform SearchStartsWith(this Transform target, string name)
        {
            if (target.name.StartsWith(name, StringComparison.Ordinal))
				return target;

            for (int i = 0; i < target.childCount; ++i)
            {
                var result = SearchStartsWith(target.GetChild(i), name);
                if (!Equals(result, null))
					return result;
            }
            return null;
        }
        //tr.name.StartsWith(rotatorsName, StringComparison.Ordinal

        public static float GetBattery(Part part)
        {
            PartResourceDefinition resourceDefinitions = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");
			var resources = new List<PartResource>();
            //List<PartResource> resources = new List<PartResource>();
            part.GetConnectedResources(resourceDefinitions.id, resourceDefinitions.resourceFlowMode, resources);
			var ratio = (float)resources.Sum(r => r.amount) / (float)resources.Sum(r => r.maxAmount);
            return ratio;
        }

        public static int SetAxisIndex(string axisString)
        {
			int index = 1; // Default to Y
			switch (axisString)
			{
				case "x":
				case "X":
                	index = 0;
					break;
				case "y": 
				case "Y":
                	index = 1;
					break;
				case "z":
				case "Z":
                	index = 2;
					break;
				default: // Supposedly it's a good idea to always provide a default in these switches. - Gaalidas
					index = 1;
					break;
			}
            return index;
        }

		// I had to rename "parta" to "thePart" so it wouldn't look so much like a typo.
		public static void PlaySound(Part thePart, string effectName, float effectPower)
        {
			thePart.Effect(effectName, effectPower);
        }

        public static void DisableAnimateButton(Part part)
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                foreach (ModuleAnimateGeneric ma in part.FindModulesImplementing<ModuleAnimateGeneric>())
                {
                    try
                    {
                        ma.Actions["ToggleAction"].active = false;
                    }
                    catch
                    {
                        //do nothing
                    }
                    try
                    {
                        ma.Events["Toggle"].guiActive = false;
                    }
                    catch
                    {
                        //do nothing
                    }
                }
            }
        }


        public static string GetLast(this string source, int tail_length)
        {
			return tail_length >= source.Length ? source : source.Substring(source.Length - tail_length);
        }
    }
}

//Gash code:

//tempSmoothSteeringy = steeringFound.localEulerAngles.y * 0.5f;

//Transform tempSteering = steeringFound.transform;
//Vector3 tempQuart = tempSteering.transform.eulerAngles;
//tempQuart.x /= steeringratio;
//tempSteering.transform.eulerAngles = tempQuart;

//tempSmoothSteeringx = tempSteering.transform.rotation.x;
// tempSmoothSteeringy = steeringFound.transform.rotation.x;

//tempSmoothSteeringx = Mathf.Lerp(steeringFound.transform.eulerAngles.x, tempSmoothSteeringx, Time.deltaTime * smoothSpeed);// / steeringratio;

//smoothSteering.eulerAngles.Set(tempSmoothSteeringx, smoothSteering.transform.rotation.y, smoothSteering.transform.rotation.z);
//Mathf.Lerp(steeringFound.transform.localEulerAngles.x, smoothSteering.transform.localEulerAngles.x, Time.deltaTime * smoothSpeed);// / steeringratio;
//            print(tempSmoothSteeringx);
//          tempSmoothSteeringx = steeringFound.transform.eulerAngles.y;//Mathf.Lerp(steeringFound.transform.localEulerAngles.y, smoothSteering.transform.localEulerAngles.y, Time.deltaTime * smoothSpeed);// / steeringratio;
//        print(tempSmoothSteeringy);
//      tempSmoothSteeringx = steeringFound.transform.eulerAngles.z;//Mathf.Lerp(steeringFound.transform.localEulerAngles.z, smoothSteering.transform.localEulerAngles.z, Time.deltaTime * smoothSpeed);// / steeringratio;
//    print(tempSmoothSteeringz);

//tempSmoothSteeringy = steeringFound.transform.eulerAngles.y;// / steeringratio;
//print(tempSmoothSteeringy);
//tempSmoothSteeringz = steeringFound.transform.eulerAngles.z;// / steeringratio;
//print(tempSmoothSteeringz);
//smoothSteering.transform.rotation = Quaternion.AngleAxis(tempSmoothSteering, Vector3.up);

//float smoothSteeringTemp = Mathf.Lerp(steeringFound.transform.rotation.z, smoothSteering.transform.rotation.z, Time.deltaTime * smoothSpeed);
//float partnerSuspensionTravel = 0;

//arrow.transform.rotation = Quaternion.AngleAxis(tempSmoothSteeringz, Vector3.up);
//arrow.localEulerAngles = Vector3.Lerp(passback, arrow.localEulerAngles, Time.deltaTime * smoothSpeed); 

/*
if (!mywc.isGrounded)
{
    this.part.Rigidbody.AddForce(-transform.up * 10);
    print(FlightGlobals.currentMainBody.gravParameter);
}
*/

/* 
if(framecount == randomNumber1)
{
randomNumber1 = UnityEngine.Random.Range(150, 250); //set next number of frames to wait
randomNumber2 = UnityEngine.Random.Range(-5.0f, 5.0f); //set which direction to move
//newSpring = currentSpring + randomNumber2;
framecount = 0; //zero framecount
}
            
//  if (framecount * 2 <= randomNumber1) //for half the frames, interpolate up to valeu
//{
this.part.Rigidbody.AddForce(transform.up * Mathf.Lerp(0, randomNumber2, randomNumber1 * Time.deltaTime));
print(Time.deltaTime);
       // userspring.spring = Mathf.Lerp(mywc.suspensionSpring.spring, newSpring,  smoothSpeed * Time.deltaTime);
       // mywc.suspensionSpring = userspring;
       // print(mywc.suspensionSpring.spring);
//}
//else //for rest of frames, move back :)
//{
    //randomMovement += randomNumber2;
    //print(randomMovement);
        //userspring.spring = Mathf.Lerp(mywc.suspensionSpring.spring, currentSpring, smoothSpeed * Time.deltaTime);
        //mywc.suspensionSpring = userspring;
        //print(mywc.suspensionSpring.spring);
    //lastFrameRideHeight = mywc.suspensionDistance;
    /*            Vector3 newPosition = arrow.transform.position; //set up the current value for this frame
                newPosition.y += randomNumber2; //add the random number
                randomNumber2 /= 2; //divide the random number every frame
                arrow.transform.position = Vector3.Lerp(newPosition, originalPosition, smoothSpeed * Time.deltaTime);//smooth from 
                //arrow.transform.Translate(0, randomNumber2, 0); //move the object
    */
//if arrow.transform.position

//}
//framecount++;

//Proportional Steering
//print("This Vessel");
//Vector3 thisVesselCoM = this.part.vessel.CoM;
//Vector3 thisVesselTransform = this.part.orgPos;

//print(thisVesselCoM);
//print(thisVesselTransform);
//alteredorgPos = DisplayorgPos * 1000f;

/*
                    if (!reverseMotorSet) //run only the first time the craft is loaded
                    {
                        float dot = Vector3.Dot(steeringFound.transform.forward, vessel.ReferenceTransform.up); // up is forward

                        if (dot < 0) // below 0 means the engine is on the left side of the craft
                        {
                            reverseMotor = true;
                            //Debug.Log("FSwheel: Reversing motor, dot: " + dot);
                        }
                        else
                        {
                            reverseMotor = false;
                            //Debug.Log("FSwheel: Motor reversing skipped, dot: " + dot);
                        }
                        reverseMotorSet = true;
;
                    }
*/

/*
maxXMinX = maxXPos - minXPos;
halfX = maxXMinX / 2;
offsetX = (maxXPos + minXPos) / 2;
adjustedPosX = posX - offsetX;

maxYMinY = maxYPos - minYPos;
halfY = maxYMinY / 2;
offsetY = (maxYPos + minYPos) / 2;
adjustedPosY = posY - offsetY;

maxZMinZ = maxZPos - minZPos;
halfZ = maxZMinZ / 2;
offsetZ = (maxZPos + minZPos) / 2;
adjustedPosZ = posZ - offsetZ;
*/

/*
                steeringRatioX = adjustedPosX / halfX;
                steeringRatioY = adjustedPosY / halfY;
                steeringRatioZ = adjustedPosZ / halfZ;

                steeringRatioX = Math.Abs(steeringRatioX);
                steeringRatioY = Math.Abs(steeringRatioY);
                steeringRatioZ = Math.Abs(steeringRatioZ);
 * */

/*
if ((st.part.orgPos.x + 1000) >= (maxXPos + 1000)) //dodgy hack. Make sure all values are positive or we struggle to evaluate < or >
{
    maxXPos = st.part.orgPos.x; //Store transform y value
}

if ((st.part.orgPos.x + 1000) <= (minXPos + 1000))
{
    minXPos = st.part.orgPos.x; //Store transform y value
}

if ((st.part.orgPos.y + 1000) >= (maxYPos + 1000)) //dodgy hack. Make sure all values are positive or we struggle to evaluate < or >
{
    maxYPos = st.part.orgPos.y; //Store transform y value
}

if ((st.part.orgPos.y + 1000) <= (minYPos + 1000))
{
    minYPos = st.part.orgPos.y; //Store transform y value
}

if ((st.part.orgPos.z + 1000) >= (maxZPos + 1000)) //dodgy hack. Make sure all values are positive or we struggle to evaluate < or >
{
    maxZPos = st.part.orgPos.z; //Store transform y value
}

if ((st.part.orgPos.z + 1000) <= (minZPos + 1000))
{
    minZPos = st.part.orgPos.z; //Store transform y value
}
 * */

//find positions
/*
maxXPos = this.part.orgPos.x;
maxYPos = this.part.orgPos.y;
maxZPos = this.part.orgPos.z;
minXPos = this.part.orgPos.x;
minYPos = this.part.orgPos.y;
minZPos = this.part.orgPos.z;
 * */