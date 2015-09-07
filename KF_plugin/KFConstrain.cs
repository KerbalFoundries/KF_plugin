using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>A KF-specific position-constrain module similar to the stock position constrainers.</summary>
    public class KFConstrain : PartModule
    {
        // disable FieldCanBeMadeReadOnly.Local
        // disable RedundantDefaultFieldInitializer
        // disable FunctionNeverReturns
        
        [KSPField]
        public string targetName;
        [KSPField]
        public string moversName;
        [KSPField]
        public bool constrainRotation;
        [KSPField]
        public bool constrainPosition;
        [KSPField]
        public bool randomise;
        [KSPField]
        public int axis = 0;
        [KSPField]
        public string rotationAxis;

        List<float> random = new List<float>();
        List<Transform> movers = new List<Transform>();
        Transform _target;
        string[] moverList;
        int objectCount = 0;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            moverList = moversName.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries); //Thanks, Mihara!

            if (HighLogic.LoadedSceneIsFlight)
                StartCoroutine(Setup());
        }

        public void SetupObjects()
        {
            _target = transform.SearchStartsWith(targetName);
            movers.Clear();
            for (int i = 0; i < moverList.Count(); i++)
            {
                movers.Add(transform.SearchStartsWith(moverList[i]));
                if (randomise)
                    random.Add(UnityEngine.Random.Range(0, 359));
                else
                    random.Add(0);
            }

            objectCount = movers.Count();
        }
        
        System.Collections.IEnumerator Setup()
        {
            //Debug.LogWarning(string.Format("Waiting a frame {0}.", Time.frameCount));
            yield return null;
            //Wait a frame for GameObjects to be destroyed. This only happens at the end of a frame,
            //and will be handled by another module - usually KFPartMirror. If we don't wait
            //we will usually find the LHS objects before they are actually destroyed.

            SetupObjects();
            if(constrainRotation)
                StartCoroutine("ConstrainRotation");
            if(constrainPosition)
                StartCoroutine("ConstrainPosition");

            yield break;
        }
        
        System.Collections.IEnumerator ConstrainRotation()
        {
            while (true)
            {
                for (int i = 0; i < objectCount; i++)
                {
                    Vector3 temp = movers[i].localEulerAngles;
                    temp[axis] = _target.localEulerAngles[axis] + random[i];
                    movers[i].localEulerAngles = temp;
                }
                yield return null;
            }
        }
        
        System.Collections.IEnumerator ConstrainPosition()
        {
            while (true)
            {
				for (int i = 0; i < objectCount; i++)
					movers[i].position = _target.position;
                yield return null;
            }
        }
    }
}
