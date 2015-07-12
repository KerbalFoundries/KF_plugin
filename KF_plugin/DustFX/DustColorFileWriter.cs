using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    //[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    //uncomment above to get a fresh DustColorDefinitions.cfg generated when you enter the space centre.
	public class KFDustColorFileWriter : MonoBehaviour
	{
		readonly KFDustColorDefinitions dustDefinitions = new KFDustColorDefinitions();
		
		/// <summary>Are we awake yet?</summary>
		void Awake()
		{
			// save default config
			// ConfigNode.Save sometimes strips out the root node which could be a problem if it's empty,
			// leading to a blank cfg which crashes the KSP loader.
			var defaultConfig = ConfigNode.CreateConfigFromObject(dustDefinitions);
			var definition = GameDatabase.Instance.GetConfigNodes("DustColorDefinitions").Single();
			defaultConfig.name = "DustColorDefinitions";
			System.IO.File.WriteAllText(string.Format("{0}GameData/KerbalFoundries/DustColorDefinitions.cfg", KSPUtil.ApplicationRootPath), defaultConfig.ToString());
		}
	}
}
