using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFTrackSurface : PartModule
    {
        GameObject _trackSurface;
        KFModuleWheel _track;
        SkinnedMeshRenderer _smr;
        Vector2 textureOffset;
        Material trackMaterial = new Material(Shader.Find("Diffuse"));

        //config fields
        [KSPField]
        public float trackLength = 10;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            print(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            _smr = part.GetComponentInChildren<SkinnedMeshRenderer>(); //this is the track
            _trackSurface = _smr.gameObject;
            _track = part.GetComponentInChildren<KFModuleWheel>();

            if (HighLogic.LoadedSceneIsFlight && !Equals(vessel.vesselType, VesselType.Debris) && vessel.parts.Count > 1)
            {
                trackMaterial = _trackSurface.renderer.material;
                Vector2 trackTiling = trackMaterial.mainTextureScale;
                trackTiling = new Vector2(trackTiling.x * _track.directionCorrector, trackTiling.y);
                trackMaterial.SetTextureScale("_MainTex",  trackTiling);
                trackMaterial.SetTextureScale("_BumpMap", trackTiling);
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            float distanceTravelled = (float)((_track.averageTrackRPM * 2 * Math.PI) / 60) * Time.deltaTime * _track.directionCorrector; //calculate how far the track will need to move
            textureOffset = trackMaterial.mainTextureOffset;
            textureOffset = textureOffset + new Vector2(-distanceTravelled / trackLength, 0); //tracklength is used to fine tune the speed of movement.
            trackMaterial.SetTextureOffset("_MainTex", textureOffset);
            trackMaterial.SetTextureOffset("_BumpMap", textureOffset);
        }
    }
}
