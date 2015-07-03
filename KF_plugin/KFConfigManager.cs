using System;
using UnityEngine;
using KerbalFoundries;

namespace KerbalFoundries
{
	[KSPAddon(KSPAddon.Startup.Instantly, false)]
	public class KFConfigManager : MonoBehaviour
	{
		[Persistent]
		public bool globalDisableDust;
		
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
				KFLog.Debug("Loading global variables.", strClassName);
				globalDisableDust = KFConfigManager.KFConfig.globalDisableDust;
				KFLog.Debug(string.Format("Param \"globalDisableDust\" = {0}", globalDisableDust), strClassName);
				ConfigNode node = configNodes[0].config;
				ConfigNode.LoadObjectFromConfig(this, node);
			}
			else
				KFLog.Debug("Error loading global variables.", strClassName);
		}
	}
}
