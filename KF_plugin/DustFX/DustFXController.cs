using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	public class KFDustFXController : MonoBehaviour
	{
		static KFDustFXController _instance;
		readonly KFDustColorDefinitions _dustDefinitions = new KFDustColorDefinitions();
		readonly KFLogUtil KFLog = new KFLogUtil();
		
		public string strClassName = "KFDustFXController";
		
		/// <summary>Game state "awake" event.</summary>
		void Awake()
		{
			_instance = this;
			ConfigNode[] definition = GameDatabase.Instance.GetConfigNodes("DustColorDefinitions");
			if (!definition.Any() || !ConfigNode.LoadObjectFromConfig(_dustDefinitions, definition.Single()))
				KFLog.Error("Failed to load dust color definitions!", strClassName);
			else
				KFLog.Log("Color definitions loaded successfully.", strClassName);
		}
		
		/// <summary>Object destruction event.</summary>
		void OnDestroy()
		{
			_instance = null;
		}
		
		/// <summary>Definition for the dust colors.</summary>
		public static KFDustColorDefinitions DustColors
		{
			get
			{
				_instance = _instance ?? new GameObject("KFDustFXController").AddComponent<KFDustFXController>();
				return _instance._dustDefinitions;
			}
		}
	}
}
