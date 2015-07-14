using System.Collections.Generic;
using UnityEngine;

namespace KerbalFoundries
{
    /// <summary>
    /// This class loads, provides and saves global configuration.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly,true)]
    public class KFPersistenceManager : MonoBehaviour
    {
        #region Log
        /// <summary>
        /// Local name of the KFLogUtil class.
        /// </summary>
        private static readonly KFLogUtil KFLog = new KFLogUtil();

        /// <summary>
        /// Name of the class for logging purposes.
        /// </summary>
        internal static readonly string strClassName = "KFPersistenceManager";
        #endregion

        /// <summary>
        /// Makes sure the global configuration is good to go.
        /// </summary>
        /// <remarks>
        /// This is a static constructor. It's called once when the class is loaded by Mono.
        /// </remarks>
        static KFPersistenceManager()
        {
            ReadConfig();
        }

        #region Read & write
        /// <summary>
        /// Retrieves the settings which are stored in the configuration file and are auto-loaded by KSP.
        /// </summary>
        private static void ReadConfig()
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

            KFLog.Log(string.Format("isDustEnabled = {0}", isDustEnabled), strClassName);
            KFLog.Log(string.Format("isDustCameraEnabled = {0}", isDustCameraEnabled), strClassName);
            KFLog.Log(string.Format("isMarkerEnabled = {0}", isMarkerEnabled), strClassName);

            // dust colors
            configFile = ConfigNode.Load(string.Format("{0}GameData/KerbalFoundries/DustColors.cfg", KSPUtil.ApplicationRootPath));
            configNode = configFile.GetNode("DustColorDefinitions");

            DustColors = new Dictionary<string, Dictionary<string, Color>>();
            foreach (ConfigNode celestialNode in configNode.GetNodes()) // for each celestial
            {
                Dictionary<string, Color> biomes = new Dictionary<string, Color>();

                foreach (ConfigNode biomeNode in celestialNode.GetNodes()) // for each biome of that celestial
                {
                    float r = 0f; float.TryParse(biomeNode.GetValue("Color").Split(',')[0], out r);
                    float g = 0f; float.TryParse(biomeNode.GetValue("Color").Split(',')[1], out g);
                    float b = 0f; float.TryParse(biomeNode.GetValue("Color").Split(',')[2], out b);
                    float a = 0f; float.TryParse(biomeNode.GetValue("Color").Split(',')[3], out a);

                    biomes.Add(biomeNode.name, new Color(r, g, b, a));
                }


                DustColors.Add(celestialNode.name, biomes);

                if (biomes.Count == 0)
                    KFLog.Error("No biome colors found for " + celestialNode.name + "!", strClassName);
                else
                    KFLog.Log("Found " + biomes.Count + " biome color definitions for " + celestialNode.name + ".", strClassName);
            }
        }

        /// <summary>
        /// Saves the settings to the configuration file.
        /// </summary>
        internal static void SaveConfig()
        {
            ConfigNode configFile = ConfigNode.Load(string.Format("{0}GameData/KerbalFoundries/KFGlobals.cfg", KSPUtil.ApplicationRootPath));
            ConfigNode configNode = configFile.GetNode("KFGlobals");

            configNode.SetValue("isDustEnabled", string.Format("{0}", isDustEnabled), true);
            configNode.SetValue("isDustCameraEnabled", string.Format("{0}", isDustCameraEnabled), true);
            configNode.SetValue("isMarkerEnabled", string.Format("{0}", isMarkerEnabled), true);
            configFile.Save(string.Format("{0}GameData/KerbalFoundries/KFGlobals.cfg", KSPUtil.ApplicationRootPath));

            KFLog.Log("Settings Saved.", strClassName);
        }
        #endregion

        #region Global configuration properties
        /// <summary>
        /// If dust is displayed.
        /// </summary>
        public static bool isDustEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// If a camera is used to identify ground color for setting the correct dust color.
        /// </summary>
        public static bool isDustCameraEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// If orientation markers on wheels are displayed in the VAB/SPH.
        /// </summary>
        public static bool isMarkerEnabled
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Dust colors for each biome.
        /// </summary>
        /// <remarks>
        /// Key = celestial name
        /// Value(s) = { Key = biome name
        ///              Value = color }
        /// </remarks>
        public static Dictionary<string, Dictionary<string, Color>> DustColors
        {
            get;
            set;
        }

        /// <summary>
        /// Use this color of there's no biome dust color defined.
        /// </summary>
        public static readonly Color DefaultDustColor = new Color(0.75f, 0.75f, 0.75f, 0.007f);

    }
}
