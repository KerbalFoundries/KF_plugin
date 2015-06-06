using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	public class KFDustColorFileWriter : MonoBehaviour
	{
		/// <summary>Prefix the logs with this to identify it.</summary>
		public string logprefix = "[DustFX - DustColorFileWriter]: ";
		
		readonly KFDustColorDefinitions dustDefinitions = new KFDustColorDefinitions ();
		
		/// <summary>Are we awake yet?</summary>
		void Awake ()
		{
			// const string locallog = "Awake(): ";
			// save default config
			// ConfigNode.Save sometimes strips out the root node which could be a problem if it's empty,
			// leading to a blank cfg = crashes KSP loader
			var defaultConfig = ConfigNode.CreateConfigFromObject(dustDefinitions);
			defaultConfig.name = "DustColorDefinitions";
			System.IO.File.WriteAllText(string.Format("{0}GameData/KerbalFoundries/DustColorDefinitions.cfg", KSPUtil.ApplicationRootPath), defaultConfig.ToString());
			var definition = GameDatabase.Instance.GetConfigNodes("DustColorDefinitions").Single();
		}
	}
}
