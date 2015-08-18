using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class ObjectDestroy : PartModule
    {
        [KSPField]
        public string objectName;

        /// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("ObjectDestroy");
        
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            Transform destroyedObject = transform.Search(objectName);
			if (!Equals(destroyedObject, null))
			{
				UnityEngine.Object.Destroy(destroyedObject.gameObject);
				//boundsDestroyed = true; //remove the bounds object to let the wheel colliders take over
				KFLog.Log(string.Format("Destroying: {0}", objectName));
			}
			else
				KFLog.Warning(string.Format("Could not find object named \"{0}\" to destroy.", objectName)); 
        }
    }
}
