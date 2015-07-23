using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFTrackSurface : PartModule, IPartSizeModifier
    {
        GameObject _trackSurface;
        KFModuleWheel _track;
        Material trackMaterial = new Material(Shader.Find("Diffuse"));
        //[KSPField(isPersistant = true)]
        public static Vector3 initialCraftSize = Vector3.zero;
        
        Vector3 attachedCraftSize = Vector3.zero;

        [KSPField]
        public float trackLength = 10;

        public override void OnAwake()
        {
            base.OnAwake();
            Debug.Log("Track Surface Awake");
            if (HighLogic.LoadedSceneIsEditor)
            {
                Debug.Log("Awake and in editor");
                initialCraftSize = ShipConstruction.CalculateCraftSize(EditorLogic.fetch.ship);
                Debug.LogError(initialCraftSize);
            }
        }

        public Vector3 GetInitialSize()
        {
            return initialCraftSize;
        }

        public void OnEditorAttach()
        {
            //initialCraftSize = ShipConstruction.CalculateCraftSize(EditorLogic.fetch.ship);
            print("editor attach");
        }

        public void Update()
        {
            //print(initialCraftSize);
        }

        public void onPartAttach()
        {
            print("part attach");
        }

        public Vector3 GetModuleSize(Vector3 defaultSize) //to do with theIPartSizeModifier stupid jiggery.
        {
            Vector3 sizeDoesMatter = GetInitialSize();
            Debug.LogError(sizeDoesMatter + " initial");
            if (defaultSize.magnitude > attachedCraftSize.magnitude)
                attachedCraftSize = defaultSize;
            Debug.LogError(attachedCraftSize + " attach size");
            Vector3 returnCraftSize = -attachedCraftSize + sizeDoesMatter;
            Debug.LogError(returnCraftSize + " return");
            return returnCraftSize;
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            print(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

			foreach (SkinnedMeshRenderer Track in this.part.GetComponentsInChildren<SkinnedMeshRenderer>()) //this is the track
                _trackSurface = Track.gameObject;
            _track = this.part.GetComponentInChildren<KFModuleWheel>();

            

            if (HighLogic.LoadedSceneIsFlight)
            {
                trackMaterial = _trackSurface.renderer.material;
                Vector2 trackTiling = trackMaterial.mainTextureScale;
				Debug.LogWarning(string.Format("Texture tiling is: {0}", trackTiling));
                trackTiling = new Vector2(trackTiling.x * _track.directionCorrector, trackTiling.y);
				Debug.LogWarning(string.Format("New texture tiling is: {0}", trackTiling));
                trackMaterial.SetTextureScale("_MainTex",  trackTiling);
                trackMaterial.SetTextureScale("_BumpMap", trackTiling);
            }

            if (HighLogic.LoadedSceneIsEditor)
            {
                part.OnEditorAttach += OnEditorAttach;
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
