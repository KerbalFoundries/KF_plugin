using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    [KSPModule("ModuleMirror")]
    public class KFModuleMirror : PartModule
    {
        //public Transform leftObject;
        //public Transform rightObject;
        public string right = "right";
        public string left = "left";
        public string swap = "swap";
        [KSPField(isPersistant = true)]
        public string cloneSide;
        [KSPField(isPersistant = true)]
        public string flightSide;

        public KFModuleMirror clone;

        [KSPField]
        public string leftObjectName;
        [KSPField]
        public string rightObjectName;

        List<Transform> leftObject = new List<Transform>();
        List<Transform> rightObject = new List<Transform>();
        string[] rightList;
        string[] leftList;
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFModuleMirror");

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            leftList = leftObjectName.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries); //Thanks, Mihara!
            rightList = rightObjectName.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);
			//KFLog.Error(string.Format("{0} {1}", leftList[0], leftList.Count()));
			//KFLog.Error(string.Format("{0} {1}", rightList[0], rightList.Count()));

			//some defaults
			//if (leftObjectName == "")
			//  leftObjectName = "Left";
			//if (rightObjectName == "")
			//  leftObjectName = "Right";

            for (int i = 0; i < leftList.Count(); i++)
            {
				leftObject.Add(transform.Search(leftList[i]));
				//KFLog.Log(string.Format("iterated left {0}", i));
            }
            for (int i = 0; i < rightList.Count(); i++)
            {
                rightObject.Add(transform.Search(rightList[i]));
				//KFLog.Log(string.Format("iterated right {0}", i));
            }

            /*
            if (tr[i].name.Equals(leftObjectName, StringComparison.Ordinal))
            {
                KFLog.Log("Found left");
                leftObject = tr;
            }
            */

            

            KFLog.Log("Loaded scene is editor");
            KFLog.Log(string.Format("{0}", flightSide));

            FindClone();
			if (!Equals(clone, null))
            {
                KFLog.Log("Part is clone");
                //FindClone(); //make sure we have the clone. No harm in checking again
                SetSide(clone.cloneSide);
            }

			if (Equals(flightSide, "")) //check to see if we have a value in persistence
            {
                KFLog.Log("No flightSide value in persistence. Sertting default");
                //KFLog.Log(string.Format("{0}", this.part.isClone));
                LeftSide();
            }
            else //flightSide has a value, so set it.
            {
                KFLog.Log("Setting value from persistence");
                SetSide(flightSide);
            }
            
            if (HighLogic.LoadedSceneIsFlight) // do this last.
            {
                //SetSide(flightSide); 
                //KFLog.Log("Loaded scene is flight");
                if (Equals(flightSide, left))
                {
                    for (int i = 0; i < rightObject.Count(); i++)
                    {
                        KFLog.Log(string.Format("Destroying Right object {0}", rightList[i]));
                        leftObject[i].gameObject.SetActive(true);
                        UnityEngine.Object.DestroyImmediate(rightObject[i].gameObject);
                    }
                }
                if (Equals(flightSide, right))
                {

                    for (int i = 0; i < leftObject.Count(); i++)
                    {
                        KFLog.Log(string.Format("Destroying left object {0}", leftList[i]));
                        rightObject[i].gameObject.SetActive(true);
                        UnityEngine.Object.DestroyImmediate(leftObject[i].gameObject);
                    }
                }
            }
		}
		// End OnStart

        /// <summary>Sets this side to left and clone to right.</summary>
        [KSPEvent(guiName = "Left", guiActive = false, guiActiveEditor = true)]
        public void LeftSide()
        {
            FindClone();
            SetSide(left);
            if (clone)
                clone.SetSide(right);
		}
        /// <summary>Sets this side to right and clone to left.</summary>
        [KSPEvent(guiName = "Right", guiActive = false, guiActiveEditor = true)]
        public void RightSide()
        {
            FindClone();
            SetSide(right);
            if (clone)
                clone.SetSide(left);
		}

		public void SetSide(string side) // Accepts the string value
        {
			if (Equals(side, left))
            {
                for (int i = 0; i < leftList.Count(); i++)
                {
                    rightObject[i].gameObject.SetActive(false);
                    leftObject[i].gameObject.SetActive(true);
                }
                cloneSide = right;
                flightSide = side;
                Events["LeftSide"].active = false;
                Events["RightSide"].active = true;
            }
			if (Equals(side, right))
            {
                for (int i = 0; i < leftList.Count(); i++)
                {
                    rightObject[i].gameObject.SetActive(true);
                    leftObject[i].gameObject.SetActive(false);
                }
                cloneSide = left;
                flightSide = side;
                Events["LeftSide"].active = true;
                Events["RightSide"].active = false;
            }
        }

        public void FindClone()
        {
			foreach (Part potentialMaster in this.part.symmetryCounterparts) // Search for parts that might be my symmetry counterpart
            {
				if (!Equals(potentialMaster, null)) // Or we'll get a null-ref
                {
                    clone = potentialMaster.Modules.OfType<KFModuleMirror>().FirstOrDefault();
                    //KFLog.Log("found my clone");
                }
            }
        }
	}
	// End class
}
// End namespace
