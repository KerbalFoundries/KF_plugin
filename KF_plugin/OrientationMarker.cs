using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>Controls the orientation marker for the editor scenes which helps reduce reports of non-functional wheels due to user-error.</summary>
	public class OrientationMarker : PartModule
	{
		[KSPField]
		public string markerName;
		Transform marker;
		bool isMarkerEnabled;
        
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("OrientationMarker");

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			marker = part.transform.Search(markerName);
			isMarkerEnabled = KFPersistenceManager.isMarkerEnabled;

			if (!Equals(marker, null) && (!isMarkerEnabled || HighLogic.LoadedSceneIsFlight))
				UnityEngine.Object.Destroy(marker.gameObject);
		}
	}
}
