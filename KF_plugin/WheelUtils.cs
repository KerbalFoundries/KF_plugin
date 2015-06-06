using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public static class WheelUtils
    {
		public static int GetCorrector(Vector3 transformVector, Transform referenceVector, int directionIndex) // Takes a vector (usually from a parts axis) and a transform, plus an index giving which axis to   
		{                                                                                               	   // Use for the scalar product of the two. Returns a value of -1 or 1, depending on whether the product is positive or negative.
            int corrector = 1;
            float dot = 0;

			switch (directionIndex) // Three if checks to see if a single variable is equal to a different number.  This is so much cleaner as a single switch with three cases. - Gaalidas
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

            //print(dot);
			/*
			if (dot < 0) // below 0 means the engine is on the left side of the craft
            {
                corrector = -1;
            }
            else
            {
                corrector = 1;
            }
			*/
			corrector = dot < 0 ? -1 : 1;
            return corrector;
        }

		public static int GetRefAxis(Vector3 refDirection, Transform refTransform) // Takes a vector 3 derived from the axis of the parts transform (typically), and the transform of the part to compare to (usually the root part).
		{                                                                   	   // Uses scalar products to determine which axis is closest to the axis specified in refDirection, return an index value 0 = X, 1 = Y, 2 = Z.
			//orgpos = this.part.orgPos; // Debugguing
            float dotx = Math.Abs(Vector3.Dot(refDirection, refTransform.right)); // up is forward
			//print(dotx); // Debugging
            float doty = Math.Abs(Vector3.Dot(refDirection, refTransform.up));
			//print(doty); // Debugging
            float dotz = Math.Abs(Vector3.Dot(refDirection, refTransform.forward));
			//print(dotz); // Debugging

            int orientationIndex = 0;

            if (dotx > doty && dotx > dotz)
            {
                //print("root part mounted right");
                orientationIndex = 0;
            }
            if (doty > dotx && doty > dotz)
            {
                //print("root part mounted forward");
                orientationIndex = 1;
            }
            if (dotz > doty && dotz > dotx)
            {
                //print("root part mounted up");
                orientationIndex = 2;
            }
            /*
            if (referenceDirection == 0)
            {
                referenceTranformVector.x = Math.Abs(referenceTranformVector.x);
            }
             */
            return orientationIndex;
        }

        public static float SetupRatios(int refIndex, Part thisPart, Vessel thisVessel, float groupNumber)      // Determines how much this wheel should be steering according to its position in the craft. Returns a value -1 to 1.
        {
            float myPosition = thisPart.orgPos[refIndex];
            float maxPos = thisPart.orgPos[refIndex];
            float minPos = thisPart.orgPos[refIndex];
            float ratio = 1;
            foreach (KFModuleWheel st in thisVessel.FindPartModulesImplementing<KFModuleWheel>()) //scan vessel to find fore or rearmost wheel. 
            {
				if (Equals(st.groupNumber, groupNumber) && !Equals(groupNumber, 0))
                {
                    float otherPosition = myPosition;
                    otherPosition = st.part.orgPos[refIndex];

					if ((otherPosition + 1000) >= (maxPos + 1000)) // Dodgy hack. Make sure all values are positive or we struggle to evaluate < or >
                        maxPos = otherPosition; // Store transform y value

                    if ((otherPosition + 1000) <= (minPos + 1000))
						minPos = otherPosition; // Store transform y value
                }
            }

            float minToMax = maxPos - minPos;
            float midPoint = minToMax / 2;
            float offset = (maxPos + minPos) / 2;
            float myAdjustedPosition = myPosition - offset;

            ratio = myAdjustedPosition / midPoint;

			if (Equals(ratio, 0) || float.IsNaN(ratio)) // Check is we managed to evaluate to zero or infinity somehow. Happens with less than three wheels, or all wheels mounted at the same position.
                ratio = 1;
            //print("ratio"); //Debugging
            //print(ratio);
            return ratio;
        }
	}
	// End class
}
