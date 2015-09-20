using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>Handles the track-surface skinned-meshes.</summary>
	public class KFTrackSurface : PartModule
	{
		GameObject _trackSurface;
		KFModuleWheel _moduleWheel;
		SkinnedMeshRenderer _skinnedMeshRenderer;
		Vector2 textureOffset;
		Material trackMaterial = new Material(Shader.Find("Diffuse"));
		
		// Config fields
		[KSPField]
		public float trackLength = 10;
		
		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			print(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
			
			_skinnedMeshRenderer = part.GetComponentInChildren<SkinnedMeshRenderer>();
			_trackSurface = _skinnedMeshRenderer.gameObject;
			_moduleWheel = part.GetComponentInChildren<KFModuleWheel>();
			
			if (HighLogic.LoadedSceneIsFlight && !Equals(vessel.vesselType, VesselType.Debris))
			{
				trackMaterial = _trackSurface.renderer.material;
				Vector2 trackTiling = trackMaterial.mainTextureScale;
				trackTiling = new Vector2(trackTiling.x * _moduleWheel.directionCorrector, trackTiling.y);
				trackMaterial.SetTextureScale("_MainTex", trackTiling);
				trackMaterial.SetTextureScale("_BumpMap", trackTiling);
			}
		}
		
		public override void OnUpdate()
		{
			base.OnUpdate();
			float distanceTravelled = (float)((_moduleWheel.averageTrackRPM * 2f * Math.PI) / 60f) * Time.deltaTime * _moduleWheel.directionCorrector;
			textureOffset = trackMaterial.mainTextureOffset;
			textureOffset = textureOffset + new Vector2(-distanceTravelled / trackLength, 0f);
			trackMaterial.SetTextureOffset("_MainTex", textureOffset);
			trackMaterial.SetTextureOffset("_BumpMap", textureOffset);
		}
	}
}
