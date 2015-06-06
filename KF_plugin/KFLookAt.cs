using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine; 

namespace KerbalFoundries
{
    public class KFLookAt : PartModule
    {
        [KSPField]
        public string targetName; 
        [KSPField]
        public string rotatorsName;
        [KSPField]
        public bool activeEditor = false;

        List<Transform> rotators = new List<Transform>();
        List<Transform> targets = new List<Transform>();

        string[] rotatorList;
        string[] targetList;
        List<float> rotationY = new List<float>();
        List<float> rotationZ = new List<float>();

        int objectCount = 0;

        bool countAgrees;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            rotatorList = rotatorsName.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries); //Thanks, Mihara!
            targetList = targetName.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);

			if ((HighLogic.LoadedSceneIsEditor && activeEditor) || HighLogic.LoadedSceneIsFlight)
                StartCoroutine(Setup());
            }

        public void SetupObjects()
        {
            print("setup objects");
            rotators.Clear();
            targets.Clear();
            for (int i = 0; i < rotatorList.Count(); i++)
            {
                rotators.Add(transform.SearchStartsWith(rotatorList[i]));
				print(string.Format("iterated rotators {0}", rotatorList.Count()));
            }
            for (int i = 0; i < targetList.Count(); i++)
            {
                targets.Add(transform.SearchStartsWith(targetList[i]));
				print(string.Format("iterated targets {0}", targetList.Count()));
            }
            objectCount = rotators.Count();

			countAgrees |= Equals(objectCount, targets.Count());
        }

        IEnumerator Setup()
        {
			Debug.LogWarning(string.Format("Waiting a frame {0}", Time.frameCount));
            yield return null;
            //Wait a frame for GameObjects to be destroyed. This only happens at the end of a frame,
            //and will be handled by another module - usually KFPartMirror. If we don't wait
            //we will usually find the LHS objects before they are actually destroyed.

            SetupObjects();
            if (countAgrees)
                StartCoroutine(Rotator());
			
            yield break;
        }

		// disable once FunctionNeverReturns
        IEnumerator Rotator()
        {
            while (true)
            {
                for (int i = 0; i < objectCount; i++)
                {
                    Vector3 vectorBetween = targets[i].position - rotators[i].position;
                    Vector3 lookAtVector = rotators[i].transform.forward;
                    Vector3 vectorProject = vectorBetween - (rotators[i].transform.right) * Vector3.Dot(vectorBetween, rotators[i].transform.right);
                    
                    float rotateAngle = Mathf.Acos(Vector3.Dot(lookAtVector, vectorProject) / Mathf.Sqrt(Mathf.Pow(lookAtVector.magnitude, 2) * Mathf.Pow(vectorProject.magnitude, 2))) * Mathf.Rad2Deg;

                    Vector3 normalvectorY = Vector3.Cross(lookAtVector, vectorProject);

                    if (Vector3.Dot(rotators[i].transform.up, vectorProject) > 0.0f)
                        rotateAngle *= -1;

                    if (!float.IsNaN(rotateAngle))
                        rotators[i].Rotate(Vector3.right, rotateAngle);
                }
                yield return null;
            }
        }

        public void Update()
        {
            base.OnUpdate();
        }
    }
}

/*
 * 
                     Vector3 hitchProjectX = rotators[i].transform.right - (rotators[i].transform.forward) * Vector3.Dot(rotators[i].transform.right, rotators[i].transform.forward);
                    Vector3 attachProjectX = vectorBetween - (rotators[i].transform.forward) * Vector3.Dot(vectorBetween, rotators[i].transform.forward);
 Vector3 hitchProjectZ = rotators[i].transform.forward - (rotators[i].transform.up) * Vector3.Dot(rotators[i].transform.forward, rotators[i].transform.up);
                    Vector3 attachProjectZ = vectorBetween - (rotators[i].transform.up) * Vector3.Dot(vectorBetween, rotators[i].transform.up);
 * 
 * float angleX = Mathf.Acos(Vector3.Dot(hitchProjectX, attachProjectX) / Mathf.Sqrt(Mathf.Pow(hitchProjectX.magnitude, 2) * Mathf.Pow(attachProjectX.magnitude, 2))) * Mathf.Rad2Deg;
 * float angleZ = Mathf.Acos(Vector3.Dot(hitchProjectZ, attachProjectZ) / Mathf.Sqrt(Mathf.Pow(hitchProjectZ.magnitude, 2) * Mathf.Pow(attachProjectZ.magnitude, 2))) * Mathf.Rad2Deg;
 * 
 * 
 * 
 * */

// old code

//Vector3 tempRotation = rotators[i].transform.transform.localEulerAngles;
//rotators[i].LookAt(targets[i], rotators[i].transform.up);
//tempRotation.x = rotators[i].transform.localEulerAngles.x;
//rotators[i].transform.localEulerAngles = tempRotation;

//Vector3 new_WayPointPos = new Vector3(rotators[i].position.x, targets[i].position.y, targets[i].position.z);
//Quaternion RotateTo = Quaternion.LookRotation(new_WayPointPos - rotators[i].position);
//rotators[i].rotation = RotateTo;
/*
Vector3 localPlanarPos = new Vector3(targets[i].InverseTransformPoint(rotators[i].position).x, targets[i].localPosition.y, targets[i].localPosition.z);

Vector3 planarPos = targets[i].TransformPoint(localPlanarPos);

Vector3 relativePos = planarPos - rotators[i].position;
                    
rotators[i].rotation = Quaternion.LookRotation(relativePos);

                    
Quaternion tmpRotation = rotators[i].localRotation; //preserves the local rotation since we only want to rotate on local Y axis
//Vector3 leadTargetPosition = FirstOrderIntercept(transform.position, Vector3.zero, laserParticleLeft.particleEmitter.localVelocity.z, target.transform.position, target.rigidbody.velocity);
Vector3 targetPointTurret = (targets[i].position - rotators[i].position).normalized; //get normalized vector toward target
Quaternion targetRotationTurret = Quaternion.LookRotation(targetPointTurret, targets[i].parent.transform.up); //get a rotation for the turret that looks toward the target
rotators[i].rotation =  targetRotationTurret; //gradually turn towards the target at the specified turnSpeed
rotators[i].localRotation = Quaternion.Euler(rotators[i].eulerAngles.x, tmpRotation.eulerAngles.y, tmpRotation.eulerAngles.z); //reset x and z rotations and only rotates the y on its local axis
                     

Vector3 ProjectX = rotators[i].transform.right - (rotators[i].transform.forward) * Vector3.Dot(rotators[i].transform.right, rotators[i].transform.forward);
//Vector3 attachProjectX = coupling.transform.right - (rotators[i].transform.forward) * Vector3.Dot(coupling.transform.right, rotators[i].transform.forward);


Vector3 targetDir = new Vector3(rotators[i].localPosition.x, rotators[i].InverseTransformPoint(targets[i].position).y, rotators[i].InverseTransformPoint(targets[i].position).z) - rotators[i].position;
Vector3 forward = rotators[i].forward;
float angle = Vector3.Angle(targetDir, forward);


//float angleX = Mathf.Acos(Vector3.Dot(hitchProjectX, attachProjectX) / Mathf.Sqrt(Mathf.Pow(hitchProjectX.magnitude, 2) * Mathf.Pow(attachProjectX.magnitude, 2))) * Mathf.Rad2Deg;
 * 
 * */
//this projects vectors onto chosen 2D planes. planes are defined by their normals, in this case hitchObject.transform.forward.