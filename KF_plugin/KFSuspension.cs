﻿using System;
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
		public float fLastFrameTraverse;
		
		[KSPField(isPersistant = true)]
		public float fSuspensionDistance;
		
		float tweakScaleCorrector = 1;
		bool isReady, isPaused;
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFSuspension");
		
		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			if (HighLogic.LoadedSceneIsFlight && !Equals(vessel.vesselType, VesselType.Debris))
			{
				GameEvents.onGamePause.Add(OnPause);
				GameEvents.onGameUnpause.Add(OnUnPause);
				
				_moduleWheel = part.GetComponentInChildren<KFModuleWheel>();
				if (!Equals(_moduleWheel, null))
					tweakScaleCorrector = _moduleWheel.tweakScaleCorrector;
				KFLog.Warning(string.Format("TS Corrector: {0}", tweakScaleCorrector));
				
				colliderList = colliderNames.SplitString();
                
				for (int i = 0; i < colliderList.Count(); i++)
				{
					colliders.Add(transform.SearchStartsWith(colliderList[i]).GetComponent<WheelCollider>());
					objectCount++;
				}
				susTrav = transform.SearchStartsWith(susTravName);
				
				initialPosition = susTrav.localPosition;
				susTravIndex = susTravAxis.SetAxisIndex();
				
				MoveSuspension(susTravIndex, -fLastFrameTraverse, susTrav);
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
			if (!isReady || isPaused)
				return;
			
			float fSuspensionMovement = 0f;
			float fFrameTraverse = fLastFrameTraverse;
			float fTraverse;
			
			bool isGrounded;
			
			for (int i = 0; i < objectCount; i++)
			{
				fTraverse = 0f;
				WheelHit hit;
				
				isGrounded = colliders[i].GetGroundHit(out hit);
				if (isGrounded)
				{
					fTraverse = (-colliders[i].transform.InverseTransformPoint(hit.point).y - (colliders[i].radius)) * tweakScaleCorrector;
					if (fTraverse > (colliders[i].suspensionDistance * tweakScaleCorrector))
                        fTraverse = colliders[i].suspensionDistance * tweakScaleCorrector;
					else if (fTraverse < -0.01f)
                        fTraverse = 0f;
				}
				else
					fTraverse = colliders[i].suspensionDistance * tweakScaleCorrector;
				
				fSuspensionMovement += fTraverse; 
			}
			
			fFrameTraverse = fSuspensionMovement / objectCount;
			fLastFrameTraverse = fFrameTraverse;
			susTrav.localPosition = initialPosition;
			MoveSuspension(susTravIndex, -fFrameTraverse, susTrav);
		}
		
		public static void MoveSuspension(int index, float movement, Transform _movedObject)
		{
			var tempVector = new Vector3(0f, 0f, 0f);
			tempVector[index] = movement;
			_movedObject.transform.Translate(tempVector, Space.Self);
		}
		
		public void OnPause()
		{
			if (!isPaused)
				isPaused = true;
		}
		
		public void OnUnPause()
		{
			if (isPaused)
				isPaused = false;
		}
		
		public void OnDestroy()
		{
			isPaused = false;
			GameEvents.onGamePause.Remove(OnPause);
			GameEvents.onGameUnpause.Remove(OnUnPause);
		}
	}
}
