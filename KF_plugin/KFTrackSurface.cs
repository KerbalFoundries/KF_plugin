using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFTrackSurface : PartModule
    {
        GameObject _trackSurface;
        KFModuleWheel _track;
        Material trackMaterial = new Material(Shader.Find("Diffuse"));

        [KSPField]
        public float trackLength = 10;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            print(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            foreach (SkinnedMeshRenderer Track in this.part.GetComponentsInChildren<SkinnedMeshRenderer>()) //this is the track
            {
                _trackSurface = Track.gameObject;
            }
            _track = this.part.GetComponentInChildren<KFModuleWheel>();

            if (HighLogic.LoadedSceneIsFlight)
            {
                trackMaterial = _trackSurface.renderer.material;
                Vector2 trackTiling = trackMaterial.mainTextureScale;
                Debug.LogWarning("texture tiling is " + trackTiling);
                trackTiling = new Vector2(trackTiling.x * _track.directionCorrector, trackTiling.y);
                Debug.LogWarning("new texture tiling is " + trackTiling);
                trackMaterial.SetTextureScale("_MainTex",  trackTiling);
                trackMaterial.SetTextureScale("_BumpMap", trackTiling);
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            float distanceTravelled = (float)((_track.averageTrackRPM * 2 * Math.PI) / 60) * Time.deltaTime * _track.directionCorrector; //calculate how far the track will need to move
            Vector2 textureOffset = trackMaterial.mainTextureOffset;
            textureOffset = textureOffset + new Vector2(-distanceTravelled / trackLength, 0); //tracklength is used to fine tune the speed of movement.
            trackMaterial.SetTextureOffset("_MainTex", textureOffset);
            trackMaterial.SetTextureOffset("_BumpMap", textureOffset);
        }
    }
}
