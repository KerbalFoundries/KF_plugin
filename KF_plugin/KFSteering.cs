﻿using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>Control module for the KF steering system.</summary>
	public class KFSteering : PartModule
	{
		[KSPField]
		public string steeringObject;
		
		[KSPField]
		public string steeringAxis = "Y";
		
		int steeringIndex = 1;
		Transform _steering;
		Vector3 initialSteeringAngle;
		
		KFModuleWheel _KFModuleWheel;
		
		public override void OnStart(PartModule.StartState state)
		{
			if (HighLogic.LoadedSceneIsFlight && (!Equals(vessel.vesselType, VesselType.Debris) || !Equals(vessel.vesselType, VesselType.EVA)))
			{
				_KFModuleWheel = part.GetComponentInChildren<KFModuleWheel>();
				_steering = part.FindModelTransform(steeringObject);
				initialSteeringAngle = _steering.transform.localEulerAngles;
				steeringIndex = steeringAxis.SetAxisIndex();
				base.OnStart(state);
				StartCoroutine(Steering());
			}
		}
		
		// disable FunctionNeverReturns
		IEnumerator Steering()
		{
			Vector3 newSteeringAngle;
			
			while (true)
			{
				newSteeringAngle = initialSteeringAngle;
				newSteeringAngle[steeringIndex] += _KFModuleWheel.fSteeringAngle;
				_steering.transform.localEulerAngles = newSteeringAngle;
				yield return new WaitForFixedUpdate();
			}
		}
	}
}
