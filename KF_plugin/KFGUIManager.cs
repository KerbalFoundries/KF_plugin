using System;
using UnityEngine;
using KerbalFoundries;

namespace KerbalFoundries
{
	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class KFGUIManager : MonoBehaviour
	{
		#region Initialization
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
		
		// Boolean for the visible states of GUI elements.
		public static bool isGUIEnabled;
		public static bool settingsLoaded;
		
		/// <summary>Local name of the KFLogUtil class.</summary>
		readonly KFLogUtil KFLog = new KFLogUtil();
		/// <summary>Name of the class for logging purposes.</summary>
		public string strClassName = "KFGUIManager";
		
		#endregion Initialization
		
		#region Startup
		
		void Awake()
		{
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onGUIApplicationLauncherReady.Add(SetupAppButton);
                GameEvents.onGUIApplicationLauncherDestroyed.Add(DestroyAppButton);
            }
		}
		
		void Start()
		{
			// LoadConfigs(); - Now handled by KFPersistenceManager
		}
		
		/// <summary>Initializing all the GUI elements we are using.</summary>
		void InitGUIElements()
		{
			centerLabel = new GUIStyle();
			centerLabel.alignment = TextAnchor.UpperCenter;
			centerLabel.normal.textColor = Color.white;
			
			appTextureGrey = GameDatabase.Instance.GetTexture(string.Format("{0}/{1}", strIconBasePath, strIconGrey), false);
			appTextureColor = GameDatabase.Instance.GetTexture(string.Format("{0}/{1}", strIconBasePath, strIconColor), false);
		}
		
		#endregion Startup
		
		#region AppLauncher Button
		
		/// <summary>Called when the AppLauncher reports that it is awake.</summary>
		void SetupAppButton()
		{
			InitGUIElements();
			if (appButton == null)
			{
				KFLog.Log("Adding button to AppLauncher.", strClassName);
				
                bool isThere;
				ApplicationLauncher.Instance.Contains(appButton, out isThere);
				if (isThere)
					ApplicationLauncher.Instance.RemoveModApplication(appButton);

				appButton = ApplicationLauncher.Instance.AddModApplication(onTrue, onFalse, onHover, onNotHover, null, null, ApplicationLauncher.AppScenes.FLIGHT, appTextureGrey);
			}
		}

        void DestroyAppButton()
        {
            if (appButton != null)
                ApplicationLauncher.Instance.RemoveModApplication(appButton);
        }
		
		/// <summary>Called when the button is put into a "true" state, or when it is activated.</summary>
		void onTrue()
		{
			KFLog.Log("onTrue() method called.", strClassName);
			appButton.SetTexture(appTextureColor);
            // LoadConfigs(); - Now handled by KFPersistenceManager
			isGUIEnabled = true;
		}
		
		/// <summary>Called when the button is in a "false" state.</summary>
		void onFalse()
		{
			//KFLog.Log("onFalse() method called.", strClassName);
			appButton.SetTexture(appTextureGrey);
			isGUIEnabled = false;
            KFPersistenceManager.SaveConfig();
		}
		
		/// <summary>Called when the cursor enters a hovered state over the button.</summary>
		void onHover()
		{
			//KFLog.Log("onHover() method called.", strClassName);
			appButton.SetTexture(appTextureColor);
		}
		
		/// <summary>Called when the cursor leaves the hovering state over the button.</summary>
		void onNotHover()
		{
			//KFLog.Log("onNotHover() method called.", strClassName);
			if (!isGUIEnabled)
				appButton.SetTexture(appTextureGrey);
		}		
		#endregion AppLauncher Button
		
		#region GUI Setup
		
		void OnGUI()
		{
            if (isGUIEnabled)
				DrawWindow(GUI_ID);
		}
		
		// disable UnusedParameter
		void DrawWindow(int windowID)
		{
			KFLog.Log("KFGUI() method called.", strClassName);
			// disable ConvertToConstant.Local
			const float width = 360;
			const float height = 250;
			float left = Screen.width / 2 - width / 2;
			float top = Screen.height / 2 - height / 2;
			const float spacer = 24;
			float leftMargin = left + 18;
			float line = 2;
			
			GUI.DragWindow(new Rect(left, top, width, height));
			GUI.Box(new Rect(left, top, width, height), "");
			GUI.Box(new Rect(left, top, width, height), "Kerbal Foundries Settings");

            KFPersistenceManager.isDustEnabled = GUI.Toggle(new Rect(leftMargin, top + line * spacer, width - 2 * spacer, spacer), KFPersistenceManager.isDustEnabled, "Dust Particles");
			line++;

            if (KFPersistenceManager.isDustEnabled)
			{
                KFPersistenceManager.isDustCameraEnabled = GUI.Toggle(new Rect(leftMargin, top + line * spacer, width - 2 * spacer, spacer), KFPersistenceManager.isDustCameraEnabled, "Dust Color Camera");
				line++;
			}
			else
                KFPersistenceManager.isDustCameraEnabled = false;

            KFPersistenceManager.isMarkerEnabled = GUI.Toggle(new Rect(leftMargin, top + line * spacer, width - 2 * spacer, spacer), KFPersistenceManager.isMarkerEnabled, "Orientation Markers");
			line++;
			
			if (GUI.Button(new Rect(leftMargin, top + line * spacer + 26, width / 2 - 2 * spacer + 8, spacer), "Save and Close"))
			{
                KFPersistenceManager.SaveConfig();
				appButton.SetFalse();
				isGUIEnabled = false;
			}
		}
		
		#endregion GUI Setup
	}
}
