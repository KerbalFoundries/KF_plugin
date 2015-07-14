using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class OrientationMarker : PartModule
    {
        [KSPField]
        public string markerName;
        Transform marker;
        bool isMarkerEnabled;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            marker = part.transform.Search(markerName);
            isMarkerEnabled = KFPersistenceManager.isMarkerEnabled;

            if (!Equals(markerName, null) && isMarkerEnabled)
                UnityEngine.Object.Destroy(marker.gameObject);

            if (!Equals(markerName, null) && HighLogic.LoadedSceneIsFlight)
            {
                UnityEngine.Object.Destroy(marker.gameObject);
                Debug.LogWarning("Marker destroyed");


            }
            else
                Debug.LogWarning("Marker not found");
        }
    }
}
