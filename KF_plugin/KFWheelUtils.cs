using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>Various extension utilities for use with the wheel modules.</summary>
	public static class WheelUtils
	{
		static string strClassName = "WheelUtils";
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		static readonly KFLogUtil KFLog = new KFLogUtil(strClassName);
		
		/// <summary>Takes a vector (usually from a parts axis) and a transform, plus an index giving which axis to use for the scalar product of the two.</summary>
		/// <param name="transformVector">Vector of the transform.</param>
		/// <param name="referenceVector">Reference vector.</param>
		/// <param name="directionIndex">Direction index.</param>
		/// <returns>A value of -1 or 1, depending on whether the product is positive or negative.</returns>
		public static int GetCorrector(Vector3 transformVector, Transform referenceVector, int directionIndex)
		{
			int corrector = 1;
			float dot = 0;
			
			switch (directionIndex)
			{
				case 0:
					dot = Vector3.Dot(transformVector, referenceVector.right);
					break;
				case 1:
					dot = Vector3.Dot(transformVector, referenceVector.up);
					break;
				case 2:
					dot = Vector3.Dot(transformVector, referenceVector.forward);
					break;
			}
			
			#if DEBUG
			KFLog.Log(string.Format("\"dot\" = {0}", dot));
			#endif
			
			corrector = dot < 0 ? -1 : 1;
			return corrector;
		}
		
		/// <summary>Takes a vector3 derived from the axis of the parts transform (typically), and the transform of the part to compare to (usually the root part).</summary>
		/// <param name="refDirection">Reference direction.</param>
		/// <param name="refTransform">Reference transform.</param>
		/// <returns>The index of the orientation.</returns>
		/// <remarks>Uses scalar products to determine which axis is closest to the axis specified in refDirection, return an index value 0 = X, 1 = Y, 2 = Z.</remarks>
		public static int GetRefAxis(Vector3 refDirection, Transform refTransform)
		{
			float dotx = Math.Abs(Vector3.Dot(refDirection, refTransform.right)); // up is forward
			float doty = Math.Abs(Vector3.Dot(refDirection, refTransform.up));
			float dotz = Math.Abs(Vector3.Dot(refDirection, refTransform.forward));
			
			#if DEBUG
			KFLog.Log(string.Format("\"dotx\" = {0}", dotx));
			KFLog.Log(string.Format("\"doty\" = {0}", doty));
			KFLog.Log(string.Format("\"dotz\" = {0}", dotz));
			#endif
			
			int orientationIndex = 0;
			
			if (dotx > doty && dotx > dotz)
			{
				#if DEBUG
                KFLog.Log("Root part mounted rightwards.");
				#endif
				
				orientationIndex = 0;
			}
			if (doty > dotx && doty > dotz)
			{
				#if DEBUG
                KFLog.Log("Root part mounted forwards.");
				#endif
				
				orientationIndex = 1;
			}
			if (dotz > doty && dotz > dotx)
			{
				#if DEBUG
                KFLog.Log("Root part mounted upwards.");
				#endif
				
				orientationIndex = 2;
			}
			return orientationIndex;
		}
		
		/// <summary>Determines how much this wheel should be steering according to its position in the craft.</summary>
		/// <param name="refIndex">Reference index.</param>
		/// <param name="thisPart">Reference part.</param>
		/// <param name="thisVessel">Reference vessel.</param>
		/// <param name="groupNumber">Reference group number.</param>
		/// <returns>A value of -1 to 1.</returns>
		public static float SetupRatios(int refIndex, Part thisPart, Vessel thisVessel, float groupNumber)
		{
			strClassName += ": SetupRatios()";
			float myPosition = thisPart.orgPos[refIndex];
			float maxPos = thisPart.orgPos[refIndex];
			float minPos = thisPart.orgPos[refIndex];
			float ratio = 1f;
			foreach (KFModuleWheel st in thisVessel.FindPartModulesImplementing<KFModuleWheel>()) 
			{
				if (Equals(st.groupNumber, groupNumber) && !Equals(groupNumber, 0f))
				{
					float otherPosition = myPosition;
					otherPosition = st.part.orgPos[refIndex];
					
					if ((otherPosition + 1000f) >= (maxPos + 1000f))
                        maxPos = otherPosition;
					if ((otherPosition + 1000f) <= (minPos + 1000f))
						minPos = otherPosition;
				}
			}
			
			float minToMax = maxPos - minPos;
			float midPoint = minToMax / 2f;
			float offset = (maxPos + minPos) / 2f;
			float myAdjustedPosition = myPosition - offset;
			
			ratio = myAdjustedPosition / midPoint;
			
			if (Equals(ratio, 0f) || float.IsNaN(ratio))
                ratio = 1f;
			
			#if DEBUG
            KFLog.Log(string.Format("\"ratio\" = {0}", ratio));
			#endif
			
			return ratio;
		}
	}
}
