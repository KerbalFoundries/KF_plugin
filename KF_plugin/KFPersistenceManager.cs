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
		static KFLogUtil KFLog;

        /// <summary>Path\KFGlobals.txt</summary>
        static string configFileName;

        /// <summary>Path\DustColors.cfg</summary>
        static string dustColorsFileName;
		
		#endregion Log
		
		/// <summary>Makes sure the global configuration is good to go.</summary>
		/// <remarks>This is a static constructor. It's called once when the class is loaded by Mono.</remarks>
		static KFPersistenceManager()
		{
            writeToLogFile = false; // this makes sure that the logging thread can't start before all of the configuration is read or bad things will happen
            KFLog = new KFLogUtil("KFPersistenceManager");

			ReadConfigFile();
            ReadDustColor();
		}
		
		#region Read & write
		/// <summary>Retrieves the settings which are stored in the configuration file and are auto-loaded by KSP.</summary>
		static void ReadConfigFile()
		{
			// KFGlobals.cfg
            configFileName = string.Format("{0}GameData/KerbalFoundries/KFGlobals.txt", KSPUtil.ApplicationRootPath);

            ConfigNode configFile = ConfigNode.Load(configFileName);
            if (Equals(configFile, null) || !configFile.HasNode("KFGlobals")) // KFGlobals-node doesn't exist
            {
                CreateConfig();
                configFile = ConfigNode.Load(configFileName);
            }

            ConfigNode configNode = configFile.GetNode("KFGlobals");
            if (Equals(configNode, null) || Equals(configNode.CountValues, 0)) // KFGlobals-node is empty
            {
                CreateConfig();
                configNode = configFile.GetNode("KFGlobals");
            }

			bool _isDustEnabled = false;
			if (bool.TryParse(configNode.GetValue("isDustEnabled"), out _isDustEnabled))
				isDustEnabled = _isDustEnabled;
			
			bool _isDustCameraEnabled = false;
			if (bool.TryParse(configNode.GetValue("isDustCameraEnabled"), out _isDustCameraEnabled))
				isDustCameraEnabled = _isDustCameraEnabled;
			
			bool _isMarkerEnabled = false;
			if (bool.TryParse(configNode.GetValue("isMarkerEnabled"), out _isMarkerEnabled))
				isMarkerEnabled = _isMarkerEnabled;
			
			bool _isRepLightEnabled = false;
			if (bool.TryParse(configNode.GetValue("isRepLightEnabled"), out _isRepLightEnabled))
				isRepLightEnabled = _isRepLightEnabled;

            float _dustAmount = 1;
            if (float.TryParse(configNode.GetValue("dustAmount"), out _dustAmount))
                dustAmount = _dustAmount;
            
			float _suspensionIncrement = 5;
			if (float.TryParse(configNode.GetValue("suspensionIncrement"), out _suspensionIncrement))
				suspensionIncrement = _suspensionIncrement;
			
			bool _isDebugEnabled = false;
			if (bool.TryParse(configNode.GetValue("isDebugEnabled"), out _isDebugEnabled))
				isDebugEnabled = _isDebugEnabled;
			
			bool _writeToLogFile = false;
			if (bool.TryParse(configNode.GetValue("writeToLogFile"), out _writeToLogFile))
				writeToLogFile = _writeToLogFile;
			
			logFile = configNode.GetValue("logFile");
            
			if (writeToLogFile)
				KerbalFoundries.Log.KFLog.StartWriter();
			
			LogConfigValues();
        }

		static void LogConfigValues()
		{
			KFLog.Log(string.Format("isDustEnabled = {0}", isDustEnabled));
			KFLog.Log(string.Format("isDustCameraEnabled = {0}", isDustCameraEnabled));
			KFLog.Log(string.Format("isMarkerEnabled = {0}", isMarkerEnabled));
			KFLog.Log(string.Format("isRepLightEnabled = {0}", isRepLightEnabled));
			KFLog.Log(string.Format("dustamount = {0}", dustAmount));
			KFLog.Log(string.Format("suspensionIncrement = {0}", suspensionIncrement));
			KFLog.Log(string.Format("isDebugEnabled = {0}", isDebugEnabled));
			if (isDebugEnabled)
			{
				KFLog.Log(string.Format("debugIsWaterColliderVisible = {0}", debugIsWaterColliderVisible));
			}
			KFLog.Log(string.Format("writeToLogFile = {0}", writeToLogFile));
			KFLog.Log(string.Format("LogFile = {0}", logFile));
		}
		
        /// <summary>Retrieves the dust colors which are stored in the DustColors-file and are auto-loaded by KSP.</summary>
        static void ReadDustColor()
        {
			// DustColors.cfg
            dustColorsFileName = string.Format("{0}GameData/KerbalFoundries/DustColors.cfg", KSPUtil.ApplicationRootPath);
            DustColors = new Dictionary<string, Dictionary<string, Color>>();
            
			ConfigNode configFile = ConfigNode.Load(dustColorsFileName);
            if (Equals(configFile, null) || !configFile.HasNode("DustColorDefinitions"))  // DustColorDefinitions-node doesn't exist
            {
                KFLog.Log("DustColors.cfg is missing or damaged!");
                return;
            }
			
            ConfigNode configNode = configFile.GetNode("DustColorDefinitions");
            if (Equals(configNode, null) || Equals(configNode.CountNodes, 0)) // DustColorDefinitions-node is empty
            {
                KFLog.Warning("Dust color definitions not found or damaged!");
                return;
            }
			
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
            System.IO.File.Delete(configFileName);
            
            // Sync debug state with debug options.
            if (!isDebugEnabled && debugIsWaterColliderVisible)
            {
				debugIsWaterColliderVisible = false;
            }
            
			var configFile = new ConfigNode();
            configFile.AddNode("KFGlobals");
            ConfigNode configNode = configFile.GetNode("KFGlobals");
            
			configNode.SetValue("isDustEnabled", string.Format("{0}", isDustEnabled), true);
			configNode.SetValue("isDustCameraEnabled", string.Format("{0}", isDustCameraEnabled), true);
			configNode.SetValue("isMarkerEnabled", string.Format("{0}", isMarkerEnabled), true);
			configNode.SetValue("isRepLightEnabled", string.Format("{0}", isRepLightEnabled), true);
			configNode.SetValue("dustAmount", string.Format("{0}", Mathf.Clamp(Extensions.RoundToNearestValue(dustAmount, 0.25f), 0f, 3f)), true);
			configNode.SetValue("suspensionIncrement", string.Format("{0}", Mathf.Clamp(Extensions.RoundToNearestValue(suspensionIncrement, 5f), 5f, 20f)), true);
			configNode.SetValue("isDebugEnabled", string.Format("{0}", isDebugEnabled), true);
			configNode.SetValue("debugIsWaterColliderVisible", string.Format("{0}", debugIsWaterColliderVisible), true);
			configNode.SetValue("writeToLogFile", writeToLogFile.ToString(), true);
			configNode.SetValue("logFile", logFile, true);
			
			configFile.Save(configFileName);

			KFLog.Log("Global Settings Saved.");
			LogConfigValues();
		}

        /// <summary>Creates configuration file with default values.</summary>
        static void CreateConfig()
        {
            isDustEnabled = true;
            isDustCameraEnabled = true;
            isMarkerEnabled = true;
			isRepLightEnabled = true;
			
            dustAmount = 1.5f; // I found this to be more like the original density, while 1 seems way too sparse.
			suspensionIncrement = 5f;
			
			isDebugEnabled = false;
			debugIsWaterColliderVisible = false;
            
			writeToLogFile = false;
            logFile = "KF.log";
			
			KFLog.Log("Default Config Created.");
            SaveConfig();
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
		
		/// <summary>If repulsor lighting is enabled.</summary>
		public static bool isRepLightEnabled
		{
			get;
			set;
		}

		/// <summary>The amount of dust to be emitted.</summary>
        public static float dustAmount
        {
            get;
            set;
        }

		/// <summary>The incremental value to change the ride height by when using action groups.</summary>
		/// <remarks>Should be rounded to the nearest whole number before the setter is called.</remarks>
        public static float suspensionIncrement
        {
            get;
            set;
        }
        
        /// <summary>Tracks whether or not the debug options should be made visible or not.</summary>
        public static bool isDebugEnabled
        {
			get;
			set;
        }
        
        public static bool debugIsWaterColliderVisible
        {
			get;
			set;
        }

		/// <summary>If all KF log messages should also be written to a log file.</summary>
		/// <remarks>logFile must be specified in the config!</remarks>
		public static bool writeToLogFile
		{
			get;
			set;
		}

		/// <summary>Path of the log file</summary>
		public static string logFile
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
				rotation = KSPUtil.ParseVector3(partToFix.partConfig.GetNode("IconOverride").GetValue("Rotation"));

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
                    if (r.tag == "Icon_Hidden") //KSP ignores Icon_Hidden tag for Skined Mesh renderers. 
                        Destroy(r);
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
            // disable ConvertClosureToMethodGroup
            boundsList.ForEach(b => bounds.Encapsulate(b)); // Do not change that to boundsList.ForEach(bounds.Encapsulate)!

			return bounds;
		}
		#endregion Part sizes fix
	}
}
