using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class ObjectDestroy : PartModule
    {
        [KSPField]
        public string objectName;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            Transform destroyedObject = transform.Search(objectName);
            if (destroyedObject != null)
            {
                UnityEngine.Object.Destroy(destroyedObject.gameObject);
                //boundsDestroyed = true; //remove the bounds object to let the wheel colliders take over
				print(string.Format("destroying {0}", objectName));
            }
            else
				Debug.LogWarning(string.Format("could not find object named {0}", objectName)); 
        }
    }
}
