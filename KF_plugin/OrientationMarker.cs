using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class OrientationMarker : PartModule
    {
        [KSPField]
        public string markerName;
        public Transform marker;
        /*
        protected override void onPartLoad()
        { 
            base.onPartLoad();
            Debug.LogWarning("OrientationMarker");
            marker = transform.Search(markerName);
            if (markerName != null)
            {
                marker.gameObject.SetActive(false);
                Debug.LogWarning("Marker deactivated");
            }
            else
                Debug.LogError("Marker not Found");
        }
        */

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            marker = part.transform.Search(markerName);
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
