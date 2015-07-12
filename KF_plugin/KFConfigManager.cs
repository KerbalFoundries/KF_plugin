using System;
using UnityEngine;
using KerbalFoundries;

namespace KerbalFoundries
{
	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class KFConfigManager : MonoBehaviour
	{
		#region Initialization
		
		[Persistent]
		public bool isDustEnabled = true;
        [Persistent]
        public bool isDustCameraEnabled = true;
        [Persistent]
		public bool isMarkerEnabled = true;
		
		//public UrlDir.UrlConfig[] configNodes;
		public ConfigNode configFile;
		public ConfigNode configNode;
		
		// AppLauncher Elements.
		ApplicationLauncherButton appButton;
		Texture2D appTextureGrey;
		Texture2D appTextureColor;
		
		// Icon Constants
		const string strIconBasePath = "KerbalFoundries/Assets";
		const string strIconGrey = "KFIconGrey";
		const string strIconColor = "KFIconColor";
		
		// GUI Constants
		public Rect settingsRect;
		const float toolWindowWidth = 300;
		const float toolWindowHeight = 100;
		const int GUI_ID = 1200;
		
		// GUI Elements
		GUIStyle centerLabel;
		//GUIStyle leftLabel;
		
		// Boolean for the visible states of GUI elements.
		public bool isGUIEnabled;
		public bool iconAdded;
		public bool settingsLoaded;
		
		public static KFConfigManager KFConfig;
		KFConfigManager()
		{
			KFConfig = this;
		}
		
		/// <summary>Local name of the KFLogUtil class.</summary>
		readonly KFLogUtil KFLog = new KFLogUtil();
		/// <summary>Name of the class for logging purposes.</summary>
		public string strClassName = "KFConfigManager";
		
		#endregion Initialization
		
		#region Startup
		
		void Start()
		{
			InitGUIElements();
			LoadConfigs();
		}
		
		/// <summary>Initializing all the GUI elements we are using.</summary>
		void InitGUIElements()
		{
			centerLabel = new GUIStyle();
			centerLabel.alignment = TextAnchor.UpperCenter;
			centerLabel.normal.textColor = Color.white;
			
			//leftLabel = new GUIStyle();
			//leftLabel.alignment = TextAnchor.UpperLeft;
			//leftLabel.normal.textColor = Color.white;
			
			appTextureGrey = GameDatabase.Instance.GetTexture(string.Format("{0}/{1}", strIconBasePath, strIconGrey), false);
			appTextureColor = GameDatabase.Instance.GetTexture(string.Format("{0}/{1}", strIconBasePath, strIconColor), false);
			GameEvents.onGUIApplicationLauncherReady.Add(SetupAppButton);
			settingsRect = GUI.Window(GUI_ID, settingsRect, KFGUI, string.Empty);
		}
		
		#endregion Startup
		
		#region Configs

		/// <summary>Loading the global varriables from the loaded config node.</summary>
		void LoadConfigs()
		{
			configFile = ConfigNode.Load(string.Format("{0}GameData/KerbalFoundries/KFGlobals.cfg", KSPUtil.ApplicationRootPath));
			configNode = configFile.GetNode("KFGlobals");
			
			isDustEnabled = bool.Parse(configNode.GetValue("isDustEnabled"));
			isDustCameraEnabled = bool.Parse(configNode.GetValue("isDustCameraEnabled"));
			isMarkerEnabled = bool.Parse(configNode.GetValue("isMarkerEnabled"));
			
			KFLog.Log(string.Format("isDustEnabled = {0}", isDustEnabled), strClassName);
			KFLog.Log(string.Format("isDustCameraEnabled = {0}", isDustCameraEnabled), strClassName);
			KFLog.Log(string.Format("isMarkerEnabled = {0}", isMarkerEnabled), strClassName);
			
//			configNodes = GameDatabase.Instance.GetConfigs("KFGlobals");
//			if (configNodes.Length > 0)
//			{
//				KFLog.Log("Loading global variables.", strClassName);
//				ConfigNode node = configNodes[0].config;
//				ConfigNode.LoadObjectFromConfig(this, node);
//				
//				isDustEnabled = KFConfigManager.KFConfig.isDustEnabled;
//				isDustCameraEnabled = KFConfigManager.KFConfig.isDustCameraEnabled;
//				isMarkerEnabled = KFConfigManager.KFConfig.isMarkerEnabled;
//                
//				settingsLoaded = true;
//			}
//			else
//			{
//				KFLog.Error("Error loading global variables.", strClassName);
//				settingsLoaded = false;
//			}
		}
		
		/// <summary>A config node save method.</summary>
		void SaveConfigs()
		{
			configNode.SetValue("isDustEnabled", string.Format("{0}", isDustEnabled), true);
			configNode.SetValue("isDustCameraEnabled", string.Format("{0}", isDustCameraEnabled), true);
			configNode.SetValue("isMarkerEnabled", string.Format("{0}", isMarkerEnabled), true);
			configFile.Save(string.Format("{0}GameData/KerbalFoundries/KFGlobals.cfg", KSPUtil.ApplicationRootPath));
			KFLog.Log("Settings Saved.", strClassName);
		}
		
		#endregion Configs
		
		#region AppLauncher Button
		
		/// <summary>Called when the AppLauncher reports that it is awake.</summary>
		void SetupAppButton()
		{
			//KFLog.Log("SetupAppButton() method called.", strClassName);
			if (Equals(appButton, null) && !iconAdded && settingsLoaded)
			{
				KFLog.Log("Adding button to AppLauncher.", strClassName);
				appButton = ApplicationLauncher.Instance.AddModApplication(onTrue, onFalse, onHover, onNotHover, Dummy, Dummy, ApplicationLauncher.AppScenes.ALWAYS, appTextureGrey);
				iconAdded = true;
			}
		}
		
		/// <summary>Called when the button is put into a "true" state, or when it is activated.</summary>
		void onTrue()
		{
			//KFLog.Log("onTrue() method called.", strClassName);
			ToggleGUI();
			return;
		}
		
		/// <summary>Called when the button is in a "false" state.</summary>
		void onFalse()
		{
			//KFLog.Log("onFalse() method called.", strClassName);
			appButton.SetTexture(appTextureGrey);
			ToggleGUI();
			return;
		}
		
		/// <summary>Called when the cursor enters a hovered state over the button.</summary>
		void onHover()
		{
			//KFLog.Log("onHover() method called.", strClassName);
			appButton.SetTexture(appTextureColor);
			return;
		}
		
		/// <summary>Called when the cursor leaves the hovering state over the button.</summary>
		void onNotHover()
		{
			//KFLog.Log("onNotHover() method called.", strClassName);
			if (isGUIEnabled)
				appButton.SetTexture(appTextureColor);
			else
				appButton.SetTexture(appTextureGrey);
			return;
		}
		
		/// <summary>Toggles the GUI state on/off.</summary>
		void ToggleGUI()
		{
			//KFLog.Log("ToggleGUI() method called.", strClassName);
			isGUIEnabled = !isGUIEnabled;
			return;
		}
		
		/// <summary>An empty method, totally on purpose.</summary>
		void Dummy()
		{
			//KFLog.Log("Dummy() method called.", strClassName);
			// Totally empty, on purpose.
			return;
		}

		#endregion AppLauncher Button
		
		#region GUI Setup
		
		public void OnGUI()
		{
			//KFLog.Log("OnGUI() method called.", strClassName);
			if (isGUIEnabled)
			{
				KFGUI(GUI_ID);
				appButton.SetTexture(appTextureColor);
			}
			else
				appButton.SetTexture(appTextureGrey);
			return;
		}
		
		void KFGUI(int windowID)
		{
			//KFLog.Log("KFGUI() method called.", strClassName);
			// disable ConvertToConstant.Local
			const float width = 360;
			const float height = 450;
			float left = Screen.width / 2 - width / 2;
			float top = Screen.height / 2 - height / 2;
			const float spacer = 24;
			float leftMargin = left + 18;
			float line = 2;
			
			isDustEnabled = KFConfigManager.KFConfig.isDustEnabled;
			isDustCameraEnabled = KFConfigManager.KFConfig.isDustCameraEnabled;
			isMarkerEnabled = KFConfigManager.KFConfig.isMarkerEnabled;
			
			GUI.DragWindow(new Rect(left, top, width, height));
			GUI.Label(new Rect(left, top, width, height), "Kerbal Foundries Settings", centerLabel);
			
			isDustEnabled = GUI.Toggle(new Rect(leftMargin, top + line * spacer, width - 2 * spacer, spacer), isDustEnabled, "Enable Dust");
			line++;
			
			if (isDustEnabled)
			{
				isDustCameraEnabled = GUI.Toggle(new Rect(leftMargin, top + line * spacer, width - 2 * spacer, spacer), isDustCameraEnabled, "Dust Camera");
				line++;
			}
			else
				isDustCameraEnabled = false;
			
			isMarkerEnabled = GUI.Toggle(new Rect(leftMargin, top + line * spacer, width - 2 * spacer, spacer), isMarkerEnabled, "Orientation Markers");
			line++;
			
			if (GUI.Button(new Rect(leftMargin, top + line * spacer + 26, width / 2 - 2 * spacer + 8, spacer), "Save and Close"))
			{
				SaveConfigs();
				isGUIEnabled = false;
			}
			
			settingsRect = new Rect(settingsRect.position.x, settingsRect.position.y, width, height);
		}
		
		#endregion GUI Setup
	}
}
