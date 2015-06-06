using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	public class KFDustFXController : MonoBehaviour
	{
		/// <summary>Prefix the logs with this to identify it.</summary>
		public string logprefix = "[DustFX - DustFXController]: ";
		/// <summary>Constant definition of the class name for use in a string format.</summary>
		public const string strKFDustFXController = "KFDustFXController";
		
		static KFDustFXController _instance;
		readonly KFDustColorDefinitions _dustDefinitions = new KFDustColorDefinitions ();
		
		/// <summary>Game state "awake" event.</summary>
		void Awake ()
		{
			const string locallog = "Awake(): ";
			_instance = this;
			ConfigNode[] definition = GameDatabase.Instance.GetConfigNodes("DustColorDefinitions");
			if (!definition.Any() || !ConfigNode.LoadObjectFromConfig(_dustDefinitions, definition.Single()))
				Debug.LogError(string.Format("{0}{1}Failed to load dust color definitions!", logprefix, locallog));
			else
				Debug.Log(string.Format("{0}{1}Color definitions loaded successfully.", logprefix, locallog));
		}
		
		/// <summary>Object destruction event.</summary>
		void OnDestroy ()
		{
			_instance = null;
		}
		
		/// <summary>Definition for the dust colors.</summary>
		public static KFDustColorDefinitions DustColors
		{
			get
			{
				_instance = _instance ?? new GameObject(strKFDustFXController).AddComponent<KFDustFXController>();
				return _instance._dustDefinitions;
			}
		}
	}
}
