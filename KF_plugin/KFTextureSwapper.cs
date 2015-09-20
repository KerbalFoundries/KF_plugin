using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KerbalFoundries.TextureTools
{
	public class KFTextureSwapper : PartModule
	{
		[KSPField]
		public int moduleID;
		
		[KSPField]
		public string textureRootFolder = string.Empty;
		
		[KSPField]
		public string objectNames = string.Empty;
		
		[KSPField]
		public string textureNames = string.Empty;
		
		[KSPField]
		public string mapNames = string.Empty;
		
		[KSPField]
		public string textureDisplayNames = "Default";
		
		[KSPField]
		public string nextButtonText = "Next Texture";
		
		[KSPField]
		public string prevButtonText = "Previous Texture";
		
		[KSPField]
		public string statusText = "Current Texture";
		
		[KSPField(isPersistant = true)]
		public int selectedTexture;
		
		[KSPField(isPersistant = true)]
		public string selectedTextureURL = string.Empty;
		
		[KSPField(isPersistant = true)]
		public string selectedMapURL = string.Empty;
		
		[KSPField]
		public bool showListButton;
		
		[KSPField]
		public bool debugMode;
		
		[KSPField]
		public bool switchableInFlight;
		
		[KSPField]
		public string secondaryMapType = "_BumpMap";
		
		[KSPField]
		public bool mapIsNormal = true;
		
		[KSPField]
		public bool repaintableEVA = true;
		
		[KSPField]
		public bool showPreviousButton = true;
		
		[KSPField]
		public bool showInfo = true;
		
		[KSPField]
		public bool updateSymmetry = true;
		
		List<Transform> targetObjectTransforms = new List<Transform>();
		readonly List<List<Material>> targetMats = new List<List<Material>>();
		
		List<string> texList = new List<string>();
		List<string> mapList = new List<string>();
		List<string> objectList = new List<string>();
		List<string> textureDisplayList = new List<string>();
		List<int> fuelTankSetupList = new List<int>();
		bool initialized;
		InterstellarDebugMessages debug;
		
		[KSPField(guiActiveEditor = true, guiName = "Current Texture")]
		public string currentTextureName = string.Empty;
		
		[KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Debug: Log Objects")]
		public void listAllObjects()
		{
			List<Transform> list = ListChildren(part.transform);
			foreach (Transform current in list)
				Debug.Log(string.Format("object: {0}", current.name));
		}

		List<Transform> ListChildren(Transform a)
		{
			var list = new List<Transform>();
			foreach (Transform transformRef in a)
			{
				list.Add(transformRef);
				list.AddRange(ListChildren(transformRef));
			}
			return list;
		}

		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Next Texture")]
		public void nextTextureEvent()
		{
			selectedTexture++;
			if (selectedTexture >= texList.Count && selectedTexture >= mapList.Count)
				selectedTexture = 0;
			useTextureAll(true);
		}

		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Previous Texture")]
		public void previousTextureEvent()
		{
			selectedTexture--;
			if (selectedTexture < 0)
				selectedTexture = Mathf.Max(texList.Count - 1, mapList.Count - 1);
			useTextureAll(true);
		}

		[KSPEvent(guiActiveUnfocused = true, unfocusedRange = 5f, guiActive = false, guiActiveEditor = false, guiName = "Repaint")]
		public void nextTextureEVAEvent()
		{
			nextTextureEvent();
		}

		public void useTextureAll(bool calledByPlayer)
		{
			applyTexToPart(calledByPlayer);
			if (!updateSymmetry)
				return;
			for (int i = 0; i < part.symmetryCounterparts.Count; i++)
			{
				InterstellarTextureSwitch2[] components = part.symmetryCounterparts[i].GetComponents<InterstellarTextureSwitch2>();
				for (int j = 0; j < components.Length; j++)
				{
					if (Equals(components[j].moduleID, moduleID))
					{
						components[j].selectedTexture = selectedTexture;
						components[j].applyTexToPart(calledByPlayer);
					}
				}
			}
		}

		void applyTexToPart(bool calledByPlayer)
		{
			initializeData();
			foreach (List<Material> current in targetMats)
			{
				foreach (Material current2 in current)
					useTextureOrMap(current2);
			}
		}

		public void useTextureOrMap(Material targetMat)
		{
			if (!Equals(targetMat, null))
			{
				useTexture(targetMat);
				useMap(targetMat);
				return;
			}
			debug.debugMessage("No target material in object.");
		}

		void useMap(Material targetMat)
		{
			debug.debugMessage(string.Concat(new object[] { "maplist count: ", mapList.Count, ", selectedTexture: ", selectedTexture, ", texlist Count: ", texList.Count }));
			if (mapList.Count > selectedTexture)
			{
				if (!GameDatabase.Instance.ExistsTexture(mapList[selectedTexture]))
				{
					debug.debugMessage(string.Format("map {0} does not exist in db", mapList[selectedTexture]));
					return;
				}
				debug.debugMessage(string.Format("map {0} exists in db", mapList[selectedTexture]));
				targetMat.SetTexture(secondaryMapType, GameDatabase.Instance.GetTexture(mapList[selectedTexture], mapIsNormal));
				selectedMapURL = mapList[selectedTexture];
				if (selectedTexture < textureDisplayList.Count && Equals(texList.Count, 0))
				{
					currentTextureName = textureDisplayList[selectedTexture];
					debug.debugMessage(string.Format("setting currentTextureName to {0}", textureDisplayList[selectedTexture]));
					return;
				}
				debug.debugMessage(string.Concat(new object[] { "not setting currentTextureName. selectedTexture is ", selectedTexture, ", texDispList count is", textureDisplayList.Count, ", texList count is ", texList.Count }));
				return;
			}
			if (mapList.Count > selectedTexture)
			{
				debug.debugMessage(string.Format("no such map: {0}", mapList[selectedTexture]));
				return;
			}
			debug.debugMessage(string.Concat(new object[] { "useMap, index out of range error, maplist count: ", mapList.Count, ", selectedTexture: ", selectedTexture }));
			for (int i = 0; i < mapList.Count; i++)
				debug.debugMessage(string.Concat(new object[] { "map ", i, ": ", mapList[i] }));
			return;
		}

		void useTexture(Material targetMat)
		{
			if (texList.Count <= selectedTexture)
				return;
			if (!GameDatabase.Instance.ExistsTexture(texList[selectedTexture]))
			{
				debug.debugMessage(string.Format("no such texture: {0}", texList[selectedTexture]));
				return;
			}
			debug.debugMessage(string.Format("assigning texture: {0}", texList[selectedTexture]));
			targetMat.mainTexture = GameDatabase.Instance.GetTexture(texList[selectedTexture], false);
			selectedTextureURL = texList[selectedTexture];
			if (selectedTexture > textureDisplayList.Count - 1)
			{
				currentTextureName = getTextureDisplayName(texList[selectedTexture]);
				return;
			}
			currentTextureName = textureDisplayList[selectedTexture];
		}

		public override string GetInfo()
		{
			if (showInfo)
			{
				List<string> list;
				list = textureNames.Length > 0 ? ParseTools.ParseNames(textureNames) : ParseTools.ParseNames(mapNames);
				textureDisplayList = ParseTools.ParseNames(textureDisplayNames);
				var stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("Alternate textures available:");
				if (Equals(list.Count, 0) && Equals(list.Count, 0))
					stringBuilder.AppendLine("None");
				for (int i = 0; i < list.Count; i++)
				{
					if (i > textureDisplayList.Count - 1)
						stringBuilder.AppendLine(getTextureDisplayName(list[i]));
					else
						stringBuilder.AppendLine(textureDisplayList[i]);
				}
				stringBuilder.AppendLine("\nUse the Next Texture button on the right click menu.");
				return stringBuilder.ToString();
			}
			return string.Empty;
		}

		string getTextureDisplayName(string longName)
		{
			string[] array = longName.Split(new char[]
			{
				'/'
			});
			return array[array.Length - 1];
		}

		public override void OnStart(PartModule.StartState state)
		{
			initializeData();
			useTextureAll(false);
			Events["nextTextureEvent"].guiActive |= switchableInFlight;
			Events["previousTextureEvent"].guiActive |= switchableInFlight && showPreviousButton;
			Events["listAllObjects"].guiActiveEditor |= showListButton;
			Events["nextTextureEVAEvent"].guiActiveUnfocused &= repaintableEVA;
			if (!showPreviousButton)
			{
				Events["previousTextureEvent"].guiActive = false;
				Events["previousTextureEvent"].guiActiveEditor = false;
			}
			Events["nextTextureEvent"].guiName = nextButtonText;
			Events["previousTextureEvent"].guiName = prevButtonText;
			Fields["currentTextureName"].guiName = statusText;
		}

		void initializeData()
		{
			if (initialized)
				return;
			debug = new InterstellarDebugMessages(debugMode, "InterstellarTextureSwitch2");
			objectList = ParseTools.ParseNames(objectNames, true);
			texList = ParseTools.ParseNames(textureNames, true, true, textureRootFolder);
			mapList = ParseTools.ParseNames(mapNames, true, true, textureRootFolder);
			textureDisplayList = ParseTools.ParseNames(textureDisplayNames);
			debug.debugMessage(string.Concat(new object[]
			{
				"found ",
				texList.Count,
				" textures, using number ",
				selectedTexture,
				", found ",
				objectList.Count,
				" objects, ",
				mapList.Count,
				" maps"
			}));
			foreach (string current in objectList)
			{
				Transform[] array = part.FindModelTransforms(current);
				var list = new List<Material>();
				Transform[] array2 = array;
				foreach (var transformRef in array2)
				{
					if (!Equals(transformRef, null) && !Equals(transformRef.gameObject.renderer, null))
					{
						Material material = transformRef.gameObject.renderer.material;
						if (!Equals(material, null) && !list.Contains(material))
							list.Add(material);
					}
				}
				targetMats.Add(list);
			}
			initialized = true;
		}
	}
}