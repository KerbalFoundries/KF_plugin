using System.Collections.Generic;
using UnityEngine;

namespace KerbalFoundries
{
	// disable ConvertToStaticType
	/// <summary>This class loads, provides and saves global configuration.</summary>
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class KFPersistenceManager : MonoBehaviour
	{
		#region Log
		
		/// <summary>Local name of the KFLogUtil class.</summary>
		static readonly KFLogUtil KFLog = new KFLogUtil("KFPersistenceManager");
		
		#endregion Log
		
		/// <summary>Makes sure the global configuration is good to go.</summary>
		/// <remarks>This is a static constructor. It's called once when the class is loaded by Mono.</remarks>
		static KFPersistenceManager()
		{
			ReadConfig();
		}
		
		#region Read & write
		/// <summary>Retrieves the settings which are stored in the configuration file and are auto-loaded by KSP.</summary>
		static void ReadConfig()
		{
			// KFGlobals.cfg
			ConfigNode configFile = ConfigNode.Load(string.Format("{0}GameData/KerbalFoundries/KFGlobals.cfg", KSPUtil.ApplicationRootPath));
			ConfigNode configNode = configFile.GetNode("KFGlobals");
			
			bool _isDustEnabled = false;
			if (bool.TryParse(configNode.GetValue("isDustEnabled"), out _isDustEnabled))
				isDustEnabled = _isDustEnabled;
			
			bool _isDustCameraEnabled = false;
			if (bool.TryParse(configNode.GetValue("isDustCameraEnabled"), out _isDustCameraEnabled))
				isDustCameraEnabled = _isDustCameraEnabled;
			
			bool _isMarkerEnabled = false;
			if (bool.TryParse(configNode.GetValue("isMarkerEnabled"), out _isMarkerEnabled))
				isMarkerEnabled = _isMarkerEnabled;
			
			KFLog.Log(string.Format("isDustEnabled = {0}", isDustEnabled));
			KFLog.Log(string.Format("isDustCameraEnabled = {0}", isDustCameraEnabled));
			KFLog.Log(string.Format("isMarkerEnabled = {0}", isMarkerEnabled));
			
			// DustColors.cfg
			configFile = ConfigNode.Load(string.Format("{0}GameData/KerbalFoundries/DustColors.cfg", KSPUtil.ApplicationRootPath));
			configNode = configFile.GetNode("DustColorDefinitions");
			
			DustColors = new Dictionary<string, Dictionary<string, Color>>();
			foreach (ConfigNode celestialNode in configNode.GetNodes()) // for each celestial
			{
				var biomes = new Dictionary<string, Color>();
				foreach (ConfigNode biomeNode in celestialNode.GetNodes()) // for each biome of that celestial
				{
					float r = 0f;
					float.TryParse(biomeNode.GetValue("Color").Split(',')[0], out r);
					float g = 0f;
					float.TryParse(biomeNode.GetValue("Color").Split(',')[1], out g);
					float b = 0f;
					float.TryParse(biomeNode.GetValue("Color").Split(',')[2], out b);
					float a = 0f;
					float.TryParse(biomeNode.GetValue("Color").Split(',')[3], out a);
					biomes.Add(biomeNode.name, new Color(r, g, b, a));
				}
				
				DustColors.Add(celestialNode.name, biomes);
				if (Equals(biomes.Count, 0))
					KFLog.Error(string.Format("No biome colors found for {0}!", celestialNode.name));
				else
					KFLog.Log(string.Format("Found {0} biome color definitions for {1}.", biomes.Count, celestialNode.name));
			}
		}
		
		/// <summary>Saves the settings to the configuration file.</summary>
		internal static void SaveConfig()
		{
			// KFGlobals.cfg
			ConfigNode configFile = ConfigNode.Load(string.Format("{0}GameData/KerbalFoundries/KFGlobals.cfg", KSPUtil.ApplicationRootPath));
			ConfigNode configNode = configFile.GetNode("KFGlobals");
			
			configNode.SetValue("isDustEnabled", string.Format("{0}", isDustEnabled), true);
			configNode.SetValue("isDustCameraEnabled", string.Format("{0}", isDustCameraEnabled), true);
			configNode.SetValue("isMarkerEnabled", string.Format("{0}", isMarkerEnabled), true);
			configFile.Save(string.Format("{0}GameData/KerbalFoundries/KFGlobals.cfg", KSPUtil.ApplicationRootPath));
			
			KFLog.Log("Global Settings Saved.");
		}
		#endregion
		
		#region Global configuration properties
		/// <summary>If dust is displayed.</summary>
		public static bool isDustEnabled
		{
			get;
			set;
		}
		
		/// <summary>If a camera is used to identify ground color for setting the correct dust color.</summary>
		public static bool isDustCameraEnabled
		{
			get;
			set;
		}
		
		/// <summary>If orientation markers on wheels are displayed in the VAB/SPH.</summary>
		public static bool isMarkerEnabled
		{
			get;
			set;
		}
		#endregion

        #region DustFX
        /// <summary>Dust colors for each biome.</summary>
		/// <remarks>Key = celestial name, Value(s) = { Key = biome_name Value = color }</remarks>
		public static Dictionary<string, Dictionary<string, Color>> DustColors
		{
			get;
			set;
		}
		
		/// <summary>Use this color of there's no biome dust color defined.</summary>
		public static readonly Color DefaultDustColor = new Color(0.75f, 0.75f, 0.75f, 0.007f);
        #endregion DustFX

        #region Part sizes fix
        void OnDestroy() // last possible point before the loading scene switches to main menu
        {
            FindKFPartsToFix().ForEach(FixPartIcon);
        }

        /// <summary>Finds all KF parts which part icons need to be fixed</summary>
        /// <returns>list of parts with IconOverride configNode</returns>
        static List<AvailablePart> FindKFPartsToFix()
        {
            List<AvailablePart> KFPartsList = PartLoader.LoadedPartsList.FindAll(IsAKFPart);
            //KFLog.Log("\nAll KF parts:", strClassName);
            //KFPartsList.ForEach(part => KFLog.Log(part.name, strClassName));

            List<AvailablePart> KFPartsToFixList = KFPartsList.FindAll(HasIconOverrideModule);
            //KFLog.Log("\nKF parts which need a fix:", strClassName);
            //KFPartsToFixList.ForEach(part => KFLog.Log(part.name, strClassName));

            return KFPartsToFixList;
        }

        /// <summary>Fixes incorrect part icon in the editor's parts list panel for every part which has a IconOverride node.
        /// The node can have several attributes.
        /// Example:
        /// IconOverride
        /// {
        ///     Multiplier = 1.0      // for finetuning icon zoom
        ///     Pivot = transformName // transform to rotate around; rotates around CoM if not specified
        ///     Rotation = vector     // offset to rotation point
        /// }
        /// All parameters are optional. The existence of an IconOverride node is enough to fix the icon.
        /// Example:
        /// IconOverride {}
        /// </summary>
        /// <param name="partToFix">part to fix</param>
        /// <remarks>This method uses code from xEvilReepersx's PartIconFixer.
        /// See https://bitbucket.org/xEvilReeperx/ksp_particonfixer/src/7f2ac4094c19?at=master for original code and license.</remarks>
        static void FixPartIcon(AvailablePart partToFix)
        {
			KFLog.Log(string.Format("Fixing icon of \"{0}\"", partToFix.name));

            // preparations
            GameObject partToFixIconPrefab = partToFix.iconPrefab;
            Bounds bounds = CalculateBounds(partToFixIconPrefab);

            // retrieve icon fixes from cfg and calculate max part size
            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            float multiplier = 1f;
            if (HasIconOverrideMultiplier(partToFix))
                float.TryParse(partToFix.partConfig.GetNode("IconOverride").GetValue("Multiplier"), out multiplier);

            float factor = 40f / max * multiplier;
            factor /= 40f;

            string pivot = string.Empty;
            if (HasIconOverridePivot(partToFix))
                pivot = partToFix.partConfig.GetNode("IconOverride").GetValue("Pivot");

            Vector3 rotation = Vector3.zero; 
            if (HasIconOverrideRotation(partToFix))
                rotation = KSPUtil.ParseVector3( partToFix.partConfig.GetNode("IconOverride").GetValue("Rotation"));

            
            // apply icon fixes
            partToFix.iconScale = max; // affects only the meter scale in the tooltip
            
            partToFixIconPrefab.transform.GetChild(0).localScale *= factor;
            partToFixIconPrefab.transform.GetChild(0).Rotate(rotation, Space.Self);

            
            // after applying the fixes the part could be off-center, correct this now
            if (string.IsNullOrEmpty(pivot))
            {
                Transform model = partToFixIconPrefab.transform.GetChild(0).Find("model");
				if (Equals(model, null))
					model = partToFixIconPrefab.transform.GetChild(0);

                Transform target = model.Find(pivot);
				if (!Equals(target, null))
					partToFixIconPrefab.transform.GetChild(0).position -= target.position;
            }
            else
                partToFixIconPrefab.transform.GetChild(0).localPosition = Vector3.zero;
        }

        /// <summary>Checks if a part velongs to Kerbal Foundries.</summary>
        /// <param name="part">part to check</param>
        /// <returns>true if the part's name starts with "KF."</returns>
        /// <remarks>KSP converts underscores in part names to a dot ("_" -> ".").</remarks>
        static bool IsAKFPart(AvailablePart part)
        {
            //KFLog.Log(part.name +
            //          "  configFileFullName: " + part.configFileFullName +
            //          "  partPath: " + part.partPath +
            //          "  partUrl: " + part.partUrl,
            //          strClassName);
            //
            // example output:
            // [LOG 15:43:15.760] [Kerbal Foundries - KFPersistenceManager()]: KF.TrackLong  configFileFullName: D:\KerbalFoundries\Kerbal Space Program\GameData\KerbalFoundries\Parts\TrackLong.cfg  partPath:   partUrl: KerbalFoundries/Parts/TrackLong/KF_TrackLong
            // Yes, partPath is empty for all parts. Deprecated attribute?

			return part.name.StartsWith("KF.", System.StringComparison.Ordinal);
        }

        /// <summary>Checks if a part has an IconOverride node in it's config.</summary>
        /// <param name="part">part to check</param>
        /// <returns>true if an IconOverride node is there</returns>
        static bool HasIconOverrideModule(AvailablePart part)
        {
            //string nodes = string.Empty;
            //foreach(ConfigNode node in part.partConfig.GetNodes())
            //    nodes += " " + node.name;
            //KFLog.Log(part.name + "  nodes:" + nodes, strClassName);
            //
            // example output:
            // [LOG 15:59:08.239] [Kerbal Foundries - KFPersistenceManager()]: KF.TrackLong  nodes: MODEL MODULE MODULE MODULE MODULE MODULE MODULE MODULE MODULE MODULE MODULE MODULE MODULE MODULE EFFECTS IconOverride MODULE
            
            return part.partConfig.HasNode("IconOverride");
        }

        /// <summary>Checks if there's a Multiplier node in IconOverride</summary>
        /// <param name="part">part to check</param>
        /// <returns>true if IconOverride->Multiplier exists</returns>
        static bool HasIconOverrideMultiplier(AvailablePart part)
        {
            return part.partConfig.GetNode("IconOverride").HasNode("Multiplier");
        }

        /// <summary>Checks if there's a Pivot node in IconOverride</summary>
        /// <param name="part">part to check</param>
        /// <returns>true if IconOverride->Pivot exists</returns>
        static bool HasIconOverridePivot(AvailablePart part)
        {
            return part.partConfig.GetNode("IconOverride").HasNode("Pivot");
        }

        /// <summary>Checks if there's a Rotation node in IconOverride</summary>
        /// <param name="part">part to check</param>
        /// <returns>true if IconOverride->Rotation exists</returns>
        static bool HasIconOverrideRotation(AvailablePart part)
        {
            return part.partConfig.GetNode("IconOverride").HasNode("Rotation");
        }

        /// <summary>Calculates the bounds of a game object.</summary>
        /// <param name="partGO">part which bounds have to be calculated</param>
        /// <returns>bounds</returns>
        /// <remarks>This code is copied from xEvilReepersx's PartIconFixer and is slightly modified.
        /// See https://bitbucket.org/xEvilReeperx/ksp_particonfixer/src/7f2ac4094c19?at=master for original code and license.</remarks>
        static Bounds CalculateBounds(GameObject partGO)
        {
            var renderers = new List<Renderer>(partGO.GetComponentsInChildren<Renderer>(true));

			if (Equals(renderers.Count, 0))
				return default(Bounds);

            var boundsList = new List<Bounds>();

            renderers.ForEach(r =>
            {
                // disable once CanBeReplacedWithTryCastAndCheckForNull
                if (r is SkinnedMeshRenderer)
                {
                    var smr = r as SkinnedMeshRenderer;
                    var mesh = new Mesh();
                    smr.BakeMesh(mesh);

                    Matrix4x4 m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                    var vertices = mesh.vertices;
                    var smrBounds = new Bounds(m.MultiplyPoint3x4(vertices[0]), Vector3.zero);

                    for (int i = 1; i < vertices.Length; ++i)
                        smrBounds.Encapsulate(m.MultiplyPoint3x4(vertices[i]));

                    Destroy(mesh);
                    boundsList.Add(smrBounds);
                }
                else if (r is MeshRenderer)
                {
                    r.gameObject.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
                    boundsList.Add(r.bounds);
                }
            });


            Bounds bounds = boundsList[0];
            boundsList.RemoveAt(0);
			boundsList.ForEach(bounds.Encapsulate);

            return bounds;

        }
        #endregion Part sizes fix
    }
}
