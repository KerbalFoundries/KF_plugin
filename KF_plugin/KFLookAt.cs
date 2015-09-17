using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>A KF-specific LookAt controller similar to the stock LookAt.</summary>
	public class KFLookAt : PartModule
	{
		[KSPField]
		public string targetName;
		[KSPField]
		public string rotatorsName;
		[KSPField]
		public bool activeEditor;

		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFLookAt");
		
		readonly List<Transform> rotators = new List<Transform>();
		readonly List<Transform> targets = new List<Transform>();

		string[] rotatorList;
		string[] targetList;

		int objectCount;

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
			#if DEBUG
			KFLog.Log("Setting up objects.");
			#endif
			
			rotators.Clear();
			targets.Clear();
			for (int i = 0; i < rotatorList.Count(); i++)
			{
				rotators.Add(transform.SearchStartsWith(rotatorList[i]));
				
				#if DEBUG
				KFLog.Log(string.Format("Iterated rotators: {0}", rotatorList.Count()));
				#endif
			}
			for (int i = 0; i < targetList.Count(); i++)
			{
				targets.Add(transform.SearchStartsWith(targetList[i]));
				
				#if DEBUG
				KFLog.Log(string.Format("Iterated targets: {0}", targetList.Count()));
				#endif
			}
			objectCount = rotators.Count();
			countAgrees |= Equals(objectCount, targets.Count());
		}

		IEnumerator Setup()
		{
			yield return null;

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
                    
					float rotateAngle = Mathf.Acos(Vector3.Dot(lookAtVector, vectorProject) / Mathf.Sqrt(Mathf.Pow(lookAtVector.magnitude, 2f) * Mathf.Pow(vectorProject.magnitude, 2f))) * Mathf.Rad2Deg;

					Vector3 normalvectorY = Vector3.Cross(lookAtVector, vectorProject);

					if (Vector3.Dot(rotators[i].transform.up, vectorProject) > 0.0f)
						rotateAngle *= -1f;

					if (!float.IsNaN(rotateAngle))
						rotators[i].Rotate(Vector3.right, rotateAngle);
				}
				yield return null;
			}
		}
	}
}
