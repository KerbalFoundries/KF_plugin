using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>Control module for the KF suspension system.</summary>
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

		KFModuleWheel _moduleWheel;

		string[] colliderList;

		int objectCount;
		int susTravIndex = 1;

		// Persistent fields. Not to be used for configs.
		[KSPField(isPersistant = true)]
		public float lastFrameTraverse;
		[KSPField(isPersistant = true)]
		public float suspensionDistance;

		float tweakScaleCorrector = 1;
		bool isReady;
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFSuspension");

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			if (HighLogic.LoadedSceneIsFlight && !Equals(vessel.vesselType, VesselType.Debris))
			{
				GameEvents.onGamePause.Add(new EventVoid.OnEvent(OnPause));
				GameEvents.onGameUnpause.Add(new EventVoid.OnEvent(OnUnPause));
				_moduleWheel = part.GetComponentInChildren<KFModuleWheel>();
				if (!Equals(_moduleWheel, null))
					tweakScaleCorrector = _moduleWheel.tweakScaleCorrector;
				KFLog.Warning(string.Format("TS Corrector: {0}", tweakScaleCorrector));

				colliderList = KFExtensions.SplitString(colliderNames);
                
				for (int i = 0; i < colliderList.Count(); i++)
				{
					colliders.Add(transform.SearchStartsWith(colliderList[i]).GetComponent<WheelCollider>());
					objectCount++;
				}
				susTrav = transform.SearchStartsWith(susTravName);

				initialPosition = susTrav.localPosition;
				susTravIndex = KFExtensions.SetAxisIndex(susTravAxis);

				MoveSuspension(susTravIndex, -lastFrameTraverse, susTrav);
				if (objectCount > 0)
					StartCoroutine("WaitAndStart");
				else
					KFLog.Error("KFSuspension not configured correctly");
			}
		}

		System.Collections.IEnumerator WaitAndStart()
		{
			int i = 0;
			while (i < 50)
			{
				i++;
				yield return new WaitForFixedUpdate();
			}
			isReady = true;
		}

		public void Update()
		{
			if (!isReady)
				return;
			float suspensionMovement = 0f;
			float frameTraverse = lastFrameTraverse;

			for (int i = 0; i < objectCount; i++)
			{
				float traverse = 0f;
				WheelHit hit;
                
				bool grounded = colliders[i].GetGroundHit(out hit);
				if (grounded)
				{
					traverse = (-colliders[i].transform.InverseTransformPoint(hit.point).y - (colliders[i].radius)) * tweakScaleCorrector;
					if (traverse > (colliders[i].suspensionDistance * tweakScaleCorrector))
                        traverse = colliders[i].suspensionDistance * tweakScaleCorrector;
					else if (traverse < -0.01f)
                        traverse = 0f;
				}
				else
					traverse = colliders[i].suspensionDistance * tweakScaleCorrector;

				suspensionMovement += traverse; 
			}

			frameTraverse = suspensionMovement / objectCount;
			lastFrameTraverse = frameTraverse;
			susTrav.localPosition = initialPosition;
			MoveSuspension(susTravIndex, -frameTraverse, susTrav);
		}

		public static void MoveSuspension(int index, float movement, Transform _movedObject)
		{
			var tempVector = new Vector3(0f, 0f, 0f);
			tempVector[index] = movement;
			_movedObject.transform.Translate(tempVector, Space.Self);
		}

		public void OnPause()
		{
			isReady = false;
		}

		public void OnUnPause()
		{
			isReady = true;
		}

		public void OnDestroy()
		{
			GameEvents.onGamePause.Remove(new EventVoid.OnEvent(OnPause));
			GameEvents.onGameUnpause.Remove(new EventVoid.OnEvent(OnUnPause));
		}
	}
}
