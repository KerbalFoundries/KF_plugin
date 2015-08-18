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
		public bool activeEditor;

		readonly List<Transform> rotators = new List<Transform>();
		readonly List<Transform> targets = new List<Transform>();

		string[] rotatorList;
		string[] targetList;

		int objectCount = 0;

		bool countAgrees;

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);

			rotatorList = rotatorsName.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries); //Thanks, Mihara!
			targetList = targetName.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);

			if ((HighLogic.LoadedSceneIsEditor && activeEditor) || (HighLogic.LoadedSceneIsFlight && !Equals(vessel.vesselType, VesselType.Debris) && vessel.parts.Count > 1))
				StartCoroutine(Setup());
		}

		public void SetupObjects()
		{
			//print("setup objects");
			rotators.Clear();
			targets.Clear();
			for (int i = 0; i < rotatorList.Count(); i++)
			{
				rotators.Add(transform.SearchStartsWith(rotatorList[i]));
				//print(string.Format("iterated rotators {0}", rotatorList.Count()));
			}
			for (int i = 0; i < targetList.Count(); i++)
			{
				targets.Add(transform.SearchStartsWith(targetList[i]));
				//print(string.Format("iterated targets {0}", targetList.Count()));
			}
			objectCount = rotators.Count();
			countAgrees |= Equals(objectCount, targets.Count());
		}

		IEnumerator Setup()
		{
			//Debug.LogWarning(string.Format("Waiting a frame {0}.", Time.frameCount));
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
	}
}
