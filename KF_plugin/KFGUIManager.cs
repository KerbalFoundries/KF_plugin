using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>This class adds a button to the stock toolbar and displays a configuration window.</summary>
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class KFGUIManager : MonoBehaviour
	{
		#region Initialization
		// AppLauncher Elements.
		// Found in another mod that making appButton static could keep it
		//  from loading multiple instances of itself.
		static ApplicationLauncherButton appButton;
		static Texture2D appTextureGrey;
		static Texture2D appTextureColor;
		
		// Icon Constants
		const string strIconBasePath = "KerbalFoundries/Assets";
		const string strIconGrey = "KFIconGrey";
		const string strIconColor = "KFIconColor";
		
		// GUI Constants
		public Rect settingsRect;
		const int GUI_ID = 1200;
		
		// Boolean for the visible state of the settings GUI.
		public static bool isGUIEnabled;
		
		/// <summary>Local name of the KFLogUtil class.</summary>
		readonly KFLogUtil KFLog = new KFLogUtil("KFGUIManager");
		
		#endregion Initialization
		
		#region MonoBehavior life cycle events
		
		/// <summary>Called when the Behavior wakes up.</summary>
        /// <remarks>
        /// The MonoBehavior only wakes up during the loading scene at the start of the game.
        /// This only happens once per game start.
        /// </remarks>
		void Awake()
		{
            //KFLog.Log("Awake()");
			
            DontDestroyOnLoad(this); // makes sure this MonoBehavior doesn't get destroyed on game scene switch.
			
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIReady);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(OnGUIUnready);
		}
		
        /// <summary>
        /// Called when the ApplicationLauncher is unreadifying, just before a scene switch.
        /// </summary>
        /// <param name="data"></param>
        void OnGUIUnready(GameScenes data)
        {
            //KFLog.Log("OnGUIUnready()");
            DestroyAppButton();
        }
		
        /// <summary>
        /// Called when the ApplicationLauncher is ready, just after a scene switch.
        /// </summary>
        void OnGUIReady()
        {
            KFLog.Log("OnGUIReady()");

            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                SetupAppButton();
        }
		
		#endregion MonoBehavior life cycle events
		
		#region AppLauncher Button
		
		/// <summary>Called when the AppLauncher reports that it is awake.</summary>
		void SetupAppButton()
		{
            // we only need to retrieve the textures once
			if (Equals(appTextureGrey, null))
				appTextureGrey = GameDatabase.Instance.GetTexture(string.Format("{0}/{1}", strIconBasePath, strIconGrey), false);

			if (Equals(appTextureColor, null))
				appTextureColor = GameDatabase.Instance.GetTexture(string.Format("{0}/{1}", strIconBasePath, strIconColor), false);

			appButton = ApplicationLauncher.Instance.AddModApplication(onTrue, onFalse, onHover, onNotHover, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB, appTextureGrey);
            KFLog.Log("App button created");
		}
		
		/// <summary>Called when the ApplicationLauncher gets destroyed.</summary>
		void DestroyAppButton()
		{
			if (!Equals(appButton, null))
			{
				ApplicationLauncher.Instance.RemoveModApplication(appButton); // removing appButton from toolbar
				appButton = null; // notifying garbage collector to delete this object
				isGUIEnabled = false; // close the cfg window if it's still open
				KFPersistenceManager.SaveConfig();
			}
		}
		
		/// <summary>Called when the button is put into a "true" state, or when it is activated.</summary>
		void onTrue()
		{
			appButton.SetTexture(appTextureColor);
			isGUIEnabled = true;
		}
		
		/// <summary>Called when the button is in a "false" state.  Saves configuration.</summary>
		void onFalse()
		{
			KFPersistenceManager.SaveConfig();
			appButton.SetTexture(appTextureGrey);
			isGUIEnabled = false;
		}
		
		/// <summary>Called when the cursor enters a hovered state over the button.</summary>
		void onHover()
		{
			appButton.SetTexture(appTextureColor);
		}
		
		/// <summary>Called when the cursor leaves the hovering state over the button.</summary>
		void onNotHover()
		{
			appButton.SetTexture(appTextureGrey);
		}
		
		#endregion AppLauncher Button
		
		#region GUI Setup
		
		/// <summary>Called by Unity when it's time to draw the GUI.</summary>
		void OnGUI()
		{
            //KFLog.Log(string.Format("OnGUI() - \"enabled\" = {0}", this.enabled));
			if (isGUIEnabled)
			{
                /* Depending on the scene the cfg window displays 1, 2 or all 3 toggles.
                 * Each toggle needs a height of 24 units (= pixels?) and there needs to
                 * be a space of 8 units between the toogles.
                 * 
                 * window height calculates like this:
                 *   24 units - space for title bar (automatically drawn)
                 *  +24 units - space for first toggle
                 * (+ 8 units - space between two toggles
                 *  +24 units - space for second toggle
                 * (+ 8 units - space between two toggles
                 *  +24 units - space for third toggle
                 * (+ 8 units - space between two toggles)
                 *  +24 units - space for the fourth toggle
                 *  +16 units - space till end of window
                 * 
                 * =>
                 *   Window height (1 toogle) :  64
                 *   Window height (2 toogles):  96
                 *   Window height (3 toogles): 128
                 *   Window height (4 toggles): 160
                 */

                float windowTop = 42f;
                float windowLeft = -260f;
                float windowHeight = 160f; // assume 4 toggles will be displayed

				if (HighLogic.LoadedSceneIsFlight)
					windowHeight -= 32f; // remove the space of 1 toggles, because only 3 toggles need to be displayed.
				
                if (HighLogic.LoadedSceneIsEditor)
                {
                    windowHeight -= 64f; // remove the space of 2 toggles, because only 1 toggle needs to be displayed
                    // in the editor the toolbar is at the bottom of the screen, so let's move it down
                    windowTop = Screen.height - 42f - windowHeight; // 42f is the height of the toolbar buttons + 2 units of space
                }
				
                // shift the window to the left until the left window border has the same x value as the button sprite or at least so far it won't clip out the edge of the monitor
                windowLeft = Screen.width + Mathf.Min(appButton.sprite.TopLeft.x - 260, windowLeft);

                settingsRect = new Rect(windowLeft, windowTop, 256f, windowHeight);
				GUI.Window(GUI_ID, settingsRect, DrawWindow, "Kerbal Foundries Settings");
			}
		}
		
		/// <summary>Creates the GUI content.</summary>
		/// <param name="windowID">ID of the window to create the content for.</param>
		void DrawWindow(int windowID)
		{
			GUI.skin = HighLogic.Skin;
			
            if (HighLogic.LoadedSceneIsEditor || Equals(HighLogic.LoadedScene, GameScenes.SPACECENTER))
                KFPersistenceManager.isMarkerEnabled = GUI.Toggle(new Rect(8f, 24f, 240f, 24f), KFPersistenceManager.isMarkerEnabled, "Enable Orientation Markers");
			
			if (HighLogic.LoadedSceneIsFlight)
			{
				KFPersistenceManager.isDustEnabled = GUI.Toggle(new Rect(8f, 24f, 240f, 24f), KFPersistenceManager.isDustEnabled, "Enable DustFX");
				KFPersistenceManager.isDustCameraEnabled = GUI.Toggle(new Rect(8f, 56f, 240f, 24f), KFPersistenceManager.isDustCameraEnabled, "Enable DustFX Camera");
				KFPersistenceManager.isRepLightEnabled = GUI.Toggle(new Rect(8f, 88f, 240f, 24f), KFPersistenceManager.isRepLightEnabled, "Enable Repulsor Lights");
                KFPersistenceManager.dustAmount = GUI.HorizontalSlider(new Rect(8f, 120f, 240f, 30), KFPersistenceManager.dustAmount, 0.0F, 3.0F); 
			}
			
			if (Equals(HighLogic.LoadedScene, GameScenes.SPACECENTER))
			{
				KFPersistenceManager.isDustEnabled = GUI.Toggle(new Rect(8f, 56f, 240f, 24f), KFPersistenceManager.isDustEnabled, "Enable DustFX");
				KFPersistenceManager.isDustCameraEnabled = GUI.Toggle(new Rect(8f, 88f, 240f, 24f), KFPersistenceManager.isDustCameraEnabled, "Enable DustFX Camera");
				KFPersistenceManager.isRepLightEnabled = GUI.Toggle(new Rect(8f, 120f, 240f, 24f), KFPersistenceManager.isRepLightEnabled, "Enable Repulsor Lights");
                KFPersistenceManager.dustAmount = GUI.HorizontalSlider(new Rect(8f, 152f, 240f, 30), KFPersistenceManager.dustAmount, 0.0F, 3.0F); 
			}
		}
		
		#endregion GUI Setup
	}
}
