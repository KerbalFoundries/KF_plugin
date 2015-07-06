using System;
using UnityEngine;
using KerbalFoundries;

namespace KerbalFoundries
{
	[KSPAddon(KSPAddon.Startup.Instantly, false)]
	public class KFConfigManager : MonoBehaviour
	{
		[Persistent]
		public bool isDustEnabled = true;
        [Persistent]
        public bool isDustCameraEnabled = true;
        [Persistent]
		public bool isMarkerEnabled = true;

		
		public static KFConfigManager KFConfig;
		KFConfigManager()
		{
			KFConfig = this;
		}
		
		readonly KFLogUtil KFLog = new KFLogUtil();
		
		/// <summary>Name of the class for logging purposes.</summary>
		public string strClassName = "KFConfigManager";
		
		void Start()
		{
			UrlDir.UrlConfig[] configNodes = GameDatabase.Instance.GetConfigs("KFGlobals");
			if (configNodes.Length > 0)
			{
				KFLog.Log("Loading global variables.", strClassName);
				ConfigNode node = configNodes[0].config;
				ConfigNode.LoadObjectFromConfig(this, node);
				
				isDustEnabled = KFConfigManager.KFConfig.isDustEnabled;
                isDustCameraEnabled = KFConfigManager.KFConfig.isDustCameraEnabled;
                isMarkerEnabled = KFConfigManager.KFConfig.isMarkerEnabled;
                
				KFLog.Log(string.Format("Param \"isDustEnabled\" = {0}", isDustEnabled), strClassName);
			}
			else
				KFLog.Error("Error loading global variables.", strClassName);
		}
	}
}
