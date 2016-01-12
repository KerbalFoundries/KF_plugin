using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KerbalFoundries
{
	// SharpDevelop Suppressions.
	// disable ConvertToStaticType
	
	/// <summary>Loads, contains, and saves global configuration nodes.</summary>
	/// <remarks>Brain-Child of *Aqua* and without which we would never have created a working GUI config system.</remarks>
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class KFPersistenceManager : MonoBehaviour
	{
		#region Log Parameters
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		static readonly KFLogUtil KFLog = new KFLogUtil("KFPersistenceManager");
		
		/// <summary>A KFLog definition for an initial log entry.</summary>
		static KFLogUtil KFLogInit;
		
		/// <summary>Path\Settings\KFGlobals.txt</summary>
		static string configFileName = string.Format("{0}GameData/KerbalFoundries/Settings/KFGlobals.txt", KSPUtil.ApplicationRootPath);
		
		/// <summary>Path\Settings\DustColors.cfg</summary>
		static string dustColorsFileName = string.Format("{0}GameData/KerbalFoundries/Settings/DustColors.cfg", KSPUtil.ApplicationRootPath);
        
		/// <summary>Log related.  Moved to the outer scope so I can manipulate it later.</summary>
		const string strClassName = "KFPersistenceManager";
		
		#endregion Log Parameters
		
		#region Initialization
		/// <summary>Makes sure the global configuration is good to go.</summary>
		/// <remarks>This is a static constructor. It's called once when the class is loaded by Mono.</remarks>
		static KFPersistenceManager()
		{
			writeToLogFile = false; // This makes sure that the logging thread can't start before all of the configuration is read or bad things will happen.
			KFLogInit = new KFLogUtil();
            
			KFLogInit.Log(string.Format("Version: {0}", KFVersion.versionString));
			
			ReadConfigFile();
			ReadDustColor();
		}
		
		#endregion Initialization
		
		#region Read & write
		/// <summary>Retrieves the settings which are stored in the configuration file and are auto-loaded by KSP.</summary>
		static void ReadConfigFile()
		{
			// KFGlobals.txt
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
			{
				writeToLogFile = _writeToLogFile;
				if (writeToLogFile)
					KerbalFoundries.Log.KFLog.StartWriter();
			}
			
			string _logfile = configNode.GetValue("logFile");
			logFilePath = Equals(_logfile, string.Empty) || Equals(_logfile, null) ? "KF.log" : _logfile;
			
			float _cameraRes = 6;
			if (float.TryParse(configNode.GetValue("cameraRes"), out _cameraRes))
				cameraRes = _cameraRes;
			
			float _cameraFraterate = 10f;
			if (float.TryParse(configNode.GetValue("cameraFramerate"), out _cameraFraterate))
				cameraFramerate = _cameraFraterate;
			
			bool _isGUIEnabled = false;
			if (bool.TryParse(configNode.GetValue("isGUIEnabled"), out _isGUIEnabled))
				isGUIEnabled = _isGUIEnabled;
			
			LogConfigValues();
		}
		
		static void LogConfigValues()
		{
			KFLog.Log("Configuration Settings are:");
			KFLog.Log(string.Format("  isDustEnabled = {0}", isDustEnabled));
			KFLog.Log(string.Format("    isDustCameraEnabled = {0}", isDustCameraEnabled));
			KFLog.Log(string.Format("  isMarkerEnabled = {0}", isMarkerEnabled));
			KFLog.Log(string.Format("  isRepLightEnabled = {0}", isRepLightEnabled));
			KFLog.Log(string.Format("  dustamount = {0}", dustAmount));
			KFLog.Log(string.Format("  suspensionIncrement = {0}", suspensionIncrement));
			KFLog.Log(string.Format("  isDebugEnabled = {0}", isDebugEnabled));
			KFLog.Log(string.Format("    debugIsWaterColliderVisible = {0}", isWaterColliderVisible));
			KFLog.Log(string.Format("    cameraRes = {0}", cameraRes));
			KFLog.Log(string.Format("    cameraFramerate = {0}", cameraFramerate));
			KFLog.Log(string.Format("  writeToLogFile = {0}", writeToLogFile));
			KFLog.Log(string.Format("  logFilePath = {0}", logFilePath));
			KFLog.Log(string.Format("  isGUIEnabled = {0}", isGUIEnabled));
		}
		
		/// <summary>Retrieves the dust colors which are stored in the DustColors-file and are auto-loaded by KSP.</summary>
		static void ReadDustColor()
		{
			bool error = false;
			
			// DustColors.cfg
			DustColors = new Dictionary<string, Dictionary<string, Color>>();
            
			ConfigNode configFile = ConfigNode.Load(dustColorsFileName);
			ConfigNode configNode = configFile.GetNode("DustColorDefinitions");
			
			if (Equals(configFile, null) || !configFile.HasNode("DustColorDefinitions"))  // DustColorDefinitions node doesn't exist.
			{
				KFLog.Warning("DustColors.cfg is missing or damaged!");
				error = true;
			}
			if (!error && (Equals(configNode, null) || Equals(configNode.CountNodes, 0)))
			{
				KFLog.Warning("Dust color definitions not found or damaged!");
				error = true;
			}
			
			if (error)
			{
				dustConfigsPresent = false;
				return;
			}
			dustConfigsPresent = true;	// Implied: "error" is false, which means the above two checks also returned false,
										// which means the file is there and properly formatted.
			
			foreach (ConfigNode celestialNode in configNode.GetNodes()) // For each celestial body do this:
			{
				var biomes = new Dictionary<string, Color>();
				foreach (ConfigNode biomeNode in celestialNode.GetNodes()) // For each biome of that celestial body do this:
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
			File.Delete(configFileName);
            
			// Sync debug state with debug options.
			if (!isDebugEnabled && isWaterColliderVisible)
				isWaterColliderVisible = false;
			isDustCameraEnabled &= isDustEnabled;
            
			var configFile = new ConfigNode();
			configFile.AddNode("KFGlobals");
			ConfigNode configNode = configFile.GetNode("KFGlobals");
            
			configNode.SetValue("isDustEnabled", isDustEnabled.ToString(), true);
			configNode.SetValue("isDustCameraEnabled", isDustCameraEnabled.ToString(), true);
			configNode.SetValue("isMarkerEnabled", isMarkerEnabled.ToString(), true);
			configNode.SetValue("isRepLightEnabled", isRepLightEnabled.ToString(), true);
			configNode.SetValue("dustAmount", Mathf.Clamp(dustAmount.RoundToNearestValue(0.25f), 0f, 3f).ToString(), true);
			configNode.SetValue("suspensionIncrement", Mathf.Clamp(suspensionIncrement.RoundToNearestValue(5f), 5f, 20f).ToString(), true);
			configNode.SetValue("isDebugEnabled", isDebugEnabled.ToString(), true);
			configNode.SetValue("debugIsWaterColliderVisible", isWaterColliderVisible.ToString(), true);
			configNode.SetValue("writeToLogFile", writeToLogFile.ToString(), true);
			configNode.SetValue("logFilePath", logFilePath, true);
			configNode.SetValue("cameraRes", Mathf.Clamp(cameraRes, 2f, 10f).ToString(), true);
			configNode.SetValue("cameraFramerate", Mathf.Clamp(cameraFramerate, 5f, 20f).ToString(), true);
			configNode.SetValue("isGUIEnabled", isGUIEnabled.ToString(), true);
			
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
			
			dustAmount = 1f;
			suspensionIncrement = 5f;
			
			isDebugEnabled = false;
			isWaterColliderVisible = false;
            
			writeToLogFile = false;
			logFilePath = "KF.log";
            
			cameraRes = 6f;
			cameraFramerate = 10f;
			
			isGUIEnabled = true;
			
			KFLog.Log("Default Config Created.");
			SaveConfig();
		}
		#endregion Read & write
		
		#region Global Config Properties
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
        
		#region Debug Stuff
		/// <summary>Tracks whether or not the debug options should be made visible or not.</summary>
		public static bool isDebugEnabled
		{
			get;
			set;
		}
        
		public static bool isWaterColliderVisible
		{
			get;
			set;
		}
		
		/// <summary>Resolution for the camera in ModuleCameraShot.</summary>
		public static float cameraRes
		{
			get;
			set;
		}
		
		/// <summary>Framerate for the camera is ModuleCameraShot.</summary>
		public static float cameraFramerate
		{
			get;
			set;
		}
		#endregion Debug Stuff
		
		/// <summary>If all KF log messages should also be written to a log file.</summary>
		/// <remarks>logFile must be specified in the global config!</remarks>
		public static bool writeToLogFile
		{
			get;
			set;
		}
		
		/// <summary>Path of the log file</summary>
		public static string logFilePath
		{
			get;
			set;
		}
		
		/// <summary>Enabled/Disables the GUI from ever being initialized.</summary>
		public static bool isGUIEnabled
		{
			get;
			set;
		}
		
		/// <summary>Non-config file variable, simply tracks the state of the dust configs.  False when no dust configs could be located.</summary>
		public static bool dustConfigsPresent
		{
			get;
			set;
		}
		#endregion Global Config Properties
		
		#region DustFX
		/// <summary>Dust colors for each biome.</summary>
		/// <remarks>Key = celestial body name, Value(s) = { Key = biome_name, Value = color }</remarks>
		public static Dictionary<string, Dictionary<string, Color>> DustColors
		{
			get;
			set;
		}
		
		/// <summary>Use this color of there's no biome dust color defined.</summary>
		public static readonly Color DefaultDustColor = new Color(0.75f, 0.75f, 0.75f, 0.007f);
		#endregion DustFX
	}
}
