using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>Allows for the desctruction of specific objects within the part/model before the part is used.</summary>
	public class KFObjectDestroy : PartModule
	{
		[KSPField]
		public string objectName;

		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFObjectDestroy");
        
		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			Transform destroyedObject = transform.Search(objectName);
			if (!Equals(destroyedObject, null))
			{
				UnityEngine.Object.Destroy(destroyedObject.gameObject);
				
				#if DEBUG
				KFLog.Log(string.Format("Destroying: {0}", objectName));
				#endif
			}
			else
				KFLog.Warning(string.Format("Could not find object named \"{0}\" to destroy.", objectName)); 
		}
	}
}
