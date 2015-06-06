using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class PartMirror : PartModule
    {
        Transform rootObject;
        const string right = "right";
        const string left = "left";
        const string swap = "swap";
        const string mirror = "mirror";
        const string move = "move";
        const string rotate = "rotate";
        const string scale = "scale";
        [KSPField(isPersistant = true)] 
        public string flightSide; 
        [KSPField(isPersistant = true)]
        public string cloneSide;
        [KSPField(isPersistant = true)]
        public Vector3 initialPosition;
        [KSPField(isPersistant = true)]
        public bool alreadyConfigured = false;

        public PartMirror clone;
        Vector3 _scale;

        [KSPField]
        public string rootObjectName;
        [KSPField]
        public int objectAxisIndex = 0; //default X
        [KSPField]
        public string mode = "mirror"; 
        [KSPField]
        public Vector3 moveAxis = new Vector3(0, 0, 1);
        [KSPField]
        public float moveAmount = 0;
        
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            rootObject = transform.Search(rootObjectName);
            //rootObject = this.part.transform;
			if (Equals(rootObject, null))
                print("did not find root part");
            _scale = rootObject.transform.localScale;
            if (!alreadyConfigured)
                initialPosition = rootObject.transform.localPosition;
            alreadyConfigured = true;

			if (Equals(flightSide, "")) // Check to see if we have a value in persistence
            {
                print("No flightSide value in persistence. Setting default");
                //print(this.part.isClone);
                SetSide(left);
            }
			else // FlightSide has a value, so set it.
            {
                print("Setting value from persistence");
                SetSide(flightSide);
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                //SetSide(flightSide); 
                print("Loaded scene is flight");
				if (Equals(flightSide, left))
                {
                    print("Setting LHS");
                    SetSide(left);
                }
				if (Equals(flightSide, right))
                {
                    print("Setting RHS");
                    SetSide(right);
                }
            }
            print("Side is");
            print(flightSide);

            FindClone();
			if (!Equals(clone, null))
            {
                print("Part is clone");
				//FindClone(); // Make sure we have the clone. No harm in checking again
                SetSide(clone.cloneSide);
            }

			if (Equals(flightSide, "")) // Check to see if we have a value in persistence
            {
                print("No flightSide value in persistence. Sertting default");
                //print(this.part.isClone);
                LeftSide();
            }
			else // FlightSide has a value, so set it.
            {
                print("Setting value from persistence");
                SetSide(flightSide);
            }
                /*
                ConfigNode _fx = new ConfigNode("MODULE");
                _fx.name = "FXModuleLookAtConstraint";
                ConfigNode _cn = new ConfigNode("CONSTRAINLOOKFX");
                _cn.AddValue("targetName", "susTravLeft");
                _cn.AddValue("rotatorsName" ,"SuspensionArmLeft");
                _fx.AddNode(_cn);
                this.part.AddModule(_fx);
 */
        }//end OnStart

        public void FindClone()
        {
            foreach (Part potentialMaster in part.symmetryCounterparts) //search for parts that might be my symmetry counterpart
            {
				if (!Equals(potentialMaster, null)) //or we'll get a null-ref
                {
                    clone = potentialMaster.Modules.OfType<PartMirror>().FirstOrDefault();
                    //print("found my clone");
                }
            }
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

        /// <summary>Sets this side to left and clone to right.</summary>
        [KSPEvent(guiName = "Left", guiActive = false, guiActiveEditor = true)]
        public void LeftSide() // Sets this side to left and clone to right
        {
            FindClone();
            SetSide(left);
            if (clone)
                clone.SetSide(right);
            }

        public void SetSide(string side) //accepts the string value
        {
			if (Equals(side, left))
            {
				if (Equals(mode, scale))
                {
                    _scale[objectAxisIndex] = Math.Abs(_scale[objectAxisIndex]); // make sure it's positive
                    print(_scale);
                    rootObject.transform.localScale = _scale;
                    //rootObject.transform.localScale = _scale;
                }
				else if (Equals(mode, move))
                {
                    rootObject.localPosition = initialPosition;
                    rootObject.transform.Translate(moveAxis * -moveAmount);
                }
                flightSide = side;
                cloneSide = right;
                Events["LeftSide"].active = false;
                Events["RightSide"].active = true;
            }
			if (Equals(side, right))
            {
				if (Equals(mode, scale))
                {
                    _scale[objectAxisIndex] = -Math.Abs(_scale[objectAxisIndex]); // make sure it's negative
                    print(_scale);
                    rootObject.transform.localScale = _scale;
                    //rootObject.transform.localScale = _scale;
                }
				else if (Equals(mode, move))
                {
                    rootObject.localPosition = initialPosition;
                    rootObject.transform.Translate(moveAxis * moveAmount);
                }
                flightSide = side;
                cloneSide = left;
                Events["LeftSide"].active = true;
                Events["RightSide"].active = false;
            }
        }
    }
    // End class
}
// End namespace

/*

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ObjectTools
{
    [KSPModule("ModuleMirror")]
    public class ModuleMirror : PartModule
    {

        public Transform leftObject;
        public Transform rightObject;
        public string right = "right";
        public string left = "left";
        public string swap = "swap";
        [KSPField(isPersistant = true)]
        public string cloneSide;
        [KSPField(isPersistant = true)]
        public string flightSide;

        public ModuleMirror clone; 

        [KSPField]
        public string leftObjectName;
        [KSPField]
        public string rightObjectName;

        [KSPField]
        public string leftTargetName;
        [KSPField]
        public string leftRotatorsName;

        [KSPField]
        public string rightTargetName;
        [KSPField]
        public string rightRotatorsName;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            if (leftObjectName == "")
                leftObjectName = "Left";
            if (rightObjectName == "")
                leftObjectName = "Right";

            foreach (Transform tr in this.part.GetComponentsInChildren<Transform>())
            {
                if (tr.name.Equals(leftObjectName, StringComparison.Ordinal))
                {
                    //print("Found left"); 
                    leftObject = tr;
                }

                if (tr.name.Equals(rightObjectName, StringComparison.Ordinal))
                {
                    //print("Found right");   
                    rightObject = tr;
                }
            }

            if (HighLogic.LoadedSceneIsFlight)
            { 
                //SetSide(flightSide); 
                print("Loaded scene is flight");
                if (flightSide == left)
                {
                    print("Destroying Right object");
                   
                }

                if (flightSide == right)
                {
                    print("Destroying left object");

                }
            }

            print("Loaded scene is editor");
            print(flightSide);

            FindClone();
            if (clone != null)
            {
                print("Part is clone");
                //FindClone(); //make sure we have the clone. No harm in checking again
                SetSide(clone.cloneSide);
            }

            if (flightSide == "") //check to see if we have a value in persistence
            {
                print("No flightSide value in persistence. Sertting default");
                //print(this.part.isClone);
                LeftSide();
            }
            else //flightSide has a value, so set it.
            {
                print("Setting value from persistence");
                SetSide(flightSide);
            }


        }//end OnStart

        [KSPEvent(guiName = "Left", guiActive = false, guiActiveEditor = true)]
        public void LeftSide() //sets this side to left and clone to right
        {
            FindClone();
            SetSide(left);

            if (clone)
            {
                clone.SetSide(right);
            }
        }
        [KSPEvent(guiName = "Right", guiActive = false, guiActiveEditor = true)]
        public void RightSide()
        {
            FindClone();
            SetSide(right);

            if (clone)
            {
                clone.SetSide(left);
            }
        }

        public void SetSide(string side) //accepts the string value
        {
            if (side == left)
            {
                rightObject.gameObject.SetActive(false);
                leftObject.gameObject.SetActive(true);
                cloneSide = right;
                flightSide = side;
                Events["LeftSide"].active = false;
                Events["RightSide"].active = true;
            }
            if (side == right)
            {
                rightObject.gameObject.SetActive(true);
                leftObject.gameObject.SetActive(false);
                cloneSide = left;
                flightSide = side;
                Events["LeftSide"].active = true;
                Events["RightSide"].active = false;
            }

        }

        public void FindClone()
        {
            foreach (Part potentialMaster in this.part.symmetryCounterparts) //search for parts that might be my symmetry counterpart
            {
                if (potentialMaster != null) //or we'll get a null-ref
                {
                    clone = potentialMaster.Modules.OfType<ModuleMirror>().FirstOrDefault();
                    //print("found my clone");
                }
            }
        }
    }//end class
}//end namespace

*/