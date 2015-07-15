using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	public class KFSuspension : PartModule
	{
		[KSPField]
		public string colliderNames;
		[KSPField]
		public string susTravName;
		[KSPField]
		public string susTravAxis = "Y";

		List<WheelCollider> colliders = new List<WheelCollider>();
		Transform susTrav;

		Vector3 initialPosition = new Vector3(0, 0, 0);

        KFModuleWheel KFMW;

		string[] colliderList;

		int objectCount;
		int susTravIndex = 1;


		//persistent fields. Not to be used for config
		[KSPField(isPersistant = true)]
		public float lastFrameTraverse;
		[KSPField(isPersistant = true)]
		public float suspensionDistance;

		float tweakScaleCorrector = 1;

		bool isReady;

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			if (HighLogic.LoadedSceneIsFlight)
			{
				//GameEvents.onGamePause.Add(new EventVoid.OnEvent(this.OnPause));
				//GameEvents.onGameUnpause.Add(new EventVoid.OnEvent(this.OnUnPause));
                KFMW = this.part.GetComponentInChildren<KFModuleWheel>();
                if (!Equals(KFMW, null))
                {
                    tweakScaleCorrector = KFMW.tweakScaleCorrector;
                }
                Debug.LogWarning("TS corrector " + tweakScaleCorrector);

				colliderList = Extensions.SplitString(colliderNames);
                
				for (int i = 0; i < colliderList.Count(); i++)
                {
                    colliders.Add(transform.SearchStartsWith(colliderList[i]).GetComponent<WheelCollider>());
                    objectCount++;
                }
                susTrav = transform.SearchStartsWith(susTravName);



                initialPosition = susTrav.localPosition;
                susTravIndex = Extensions.SetAxisIndex(susTravAxis);

                MoveSuspension(susTravIndex, -lastFrameTraverse, susTrav); //to get the initial stuff correct
                if (objectCount > 0)
                    isReady = true;
                else
                    Debug.LogError("KFSuspension not configured correctly");
            }
        
        }

        public void FixedUpdate()
        {
            if (!isReady)
                return;
            float suspensionMovement = 0;
            float frameTraverse = 0;

            for (int i = 0; i < objectCount; i++)
            {
                float traverse = 0;
                WheelHit hit; //set this up to grab sollider raycast info
                
                bool grounded = colliders[i].GetGroundHit(out hit); //set up to pass out wheelhit coordinates
                //float tempLastFrameTraverse = lastFrameTraverse; //we need the value, but will over-write shortly. Store it here.
                if (grounded) //is it on the ground
                {
                    traverse = (-colliders[i].transform.InverseTransformPoint(hit.point).y - (colliders[i].radius) ) * tweakScaleCorrector; //calculate suspension travel using the collider raycast.

                    if (traverse > (colliders[i].suspensionDistance * tweakScaleCorrector)) //the raycast sometimes goes further than its max value. Catch and stop the mesh moving further
                        traverse = colliders[i].suspensionDistance * tweakScaleCorrector;
                    else if (traverse < -0.1) //the raycast can be negative (!); catch this too
                        traverse = 0;
                }
                else
                    traverse = colliders[i].suspensionDistance * tweakScaleCorrector; //movement defaults back to last position when the collider is not grounded. Ungrounded collider returns suspension travel of zero!

                suspensionMovement += traverse; //debug only
            }

            frameTraverse = suspensionMovement / objectCount; //average the movement.
            lastFrameTraverse = frameTraverse;

            susTrav.localPosition = initialPosition;
            MoveSuspension(susTravIndex, -frameTraverse, susTrav);
        }

        public static void MoveSuspension(int index, float movement, Transform movedObject) //susTrav Axis, amount to move, named object.
        {
            // Instead of reiterating "Vector3" we an use "var" in this instance. - Gaalidas
            var tempVector = new Vector3(0, 0, 0);
            tempVector[index] = movement;
            movedObject.transform.Translate(tempVector, Space.Self);
        }

    }
}
