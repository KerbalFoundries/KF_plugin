﻿using UnityEngine;
using KerbalFoundries;

namespace KerbalFoundries
{
	/// <summary>Manager class for everything having to do with the KF Config GUI.</summary>
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class KFGUIManager : MonoBehaviour
	{
		#region Initialization
		// AppLauncher Elements.
		// Found in another mod that making appButton static could keep it
		//  from loading multiple instances of itself.
		static ApplicationLauncherButton appButton;
		static Texture2D appTextureGrey, appTextureColor;
		// static Texture2D appTextureColor;
		
		// Icon Constants
		const string ICONBASEPATH = "KerbalFoundries/Assets";
		const string ICONGREY = "KFIconGrey";
		const string ICONCOLOR = "KFIconColor";
		
		// GUI height constants
		const float TITLEHEIGHT = 12f;
		const float TOGGLEHEIGHT = 24f;
		const float SLIDERHEIGHT = 16f;
		const float SPACERHEIGHT = 8f;
		const float LABELHEIGHT = 24f;
		const float ENDHEIGHT = 8f;
		
		// GUI Constants
		public Rect settingsRect;
		const int GUI_ID = 1222;
		
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
			if (KFPersistenceManager.isGUIEnabled)
			{
				DontDestroyOnLoad(this); // makes sure this MonoBehavior doesn't get destroyed on game scene switch.
				
				GameEvents.onGUIApplicationLauncherReady.Add(OnGUIReady);
				GameEvents.onGUIApplicationLauncherUnreadifying.Add(OnGUIUnready);
			}
		}
		
		/// <summary>
		/// Called when the ApplicationLauncher is unreadifying, just before a scene switch.
		/// </summary>
		/// <param name="data"></param>
		void OnGUIUnready(GameScenes data)
		{
			if (KFPersistenceManager.isGUIEnabled)
			{
				#if DEBUG
            	KFLog.Log("OnGUIUnready()");
				#endif
				
				DestroyAppButton();
			}
		}
		
		/// <summary>
		/// Called when the ApplicationLauncher is ready, just after a scene switch.
		/// </summary>
		void OnGUIReady()
		{
			if (KFPersistenceManager.isGUIEnabled)
			{
				#if DEBUG
            	KFLog.Log("OnGUIReady()");
				#endif
				
				if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor || Equals(HighLogic.LoadedScene, GameScenes.SPACECENTER))
					SetupAppButton();
			}
		}
		
		#endregion MonoBehavior life cycle events
		
		#region AppLauncher Button
		
		/// <summary>Called when the AppLauncher reports that it is awake.</summary>
		void SetupAppButton()
		{
			// We only need to retrieve the textures once
			if (Equals(appTextureGrey, null))
				appTextureGrey = GameDatabase.Instance.GetTexture(string.Format("{0}/{1}", ICONBASEPATH, ICONGREY), false);
			
			if (Equals(appTextureColor, null))
				appTextureColor = GameDatabase.Instance.GetTexture(string.Format("{0}/{1}", ICONBASEPATH, ICONCOLOR), false);
			
			appButton = ApplicationLauncher.Instance.AddModApplication(onTrue, onFalse, onHover, onNotHover, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB, appTextureGrey);
		}
		
		/// <summary>Called when the ApplicationLauncher gets destroyed.</summary>
		void DestroyAppButton()
		{
			if (!Equals(appButton, null))
			{
				ApplicationLauncher.Instance.RemoveModApplication(appButton);
				appButton = null;
				isGUIEnabled = false;
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
			if (isGUIEnabled && KFPersistenceManager.isGUIEnabled)
			{
				/* Depending on the scene the cfg window displays 1, 2 or all 3 toggles.
                 * Each toggle needs a height of 24 units (= pixels?) and there needs to
                 *  be a space of 8 units between the toogles.
                 * 
                 * Window height calculates like this:
                 *   16 units - space for title bar (automatically drawn) (titleHeight)
                 *  +24 units - space for first toggle. (toggleHeight)
                 * (+ 8 units - space between the elements) (spacerHeight)
                 *  +24 units - space for second toggle. (toggleHeight)
                 * (+ 8 units - space between the elements) (spacerHeight)
                 *  +24 units - space for third toggle. (toggleHeight)
                 * (+ 8 units - space between the elements) (spacerHeight)
                 *  +24 units - space for the fourth toggle. (toggleHeight)
				 * (+ 8 units - space between the elements) (spacerHeight)
				 *  +16 units - space for the slider. (sliderHeight)
				 * (+ 8 units - space between the elements) (spacerHeight)
				 *  +16 units - space for another slider. (sliderHeight)
				 *   +8 units - space till end of window. (endHeight)
                 * 
                 * =>
                 *   Window height (1 toogle) :  40 (titleHeight(12) + toggleHeight(24) + spacerHeight(8))
                 *   Window height (2 toogles):  76 (titleHeight(12) + (toggleHeight(24) * 2) + (spacerHeight(8) * 2))
                 *   Window height (3 toogles): 106 (titleHeight(12) + (toggleHeight(24) * 3) + (spacerHeight(8) * 3))
                 *   Window height (4 toggles): 140 (titleHeight(12) + (toggleHeight(24) * 4) + (spacerHeight(8) * 4))
                 *   Window height (1 slider) : 164 (titleHeight(12) + (toggleHeight(24) * 4) + (spacerHeight(8) * 5) + sliderHeight(16))
                 *   Window height (ALL ELEMENTS): 188 (titleHeight(12) + (toggleHeight(24) * 4) + (spacerHeight(8) * 6) + (sliderHeight(16) * 2))
				 *
				 * Basically +32 for each toggle, and +38 for each slider.
				 * Or do it the long way with floats and multiplication to
				 *  get the exact number of elements and their height requirements 
				 *  added together.
                 */
				
				float windowTop = 42f;
				float windowLeft = -260f;
				float windowHeight = 240; // assume 1 title, 4 toggles, 2 sliders, 2 labels, 5 spacers, and 1 end.  (ALL ELEMENTS) (Never shown)
				
				if (HighLogic.LoadedSceneIsFlight)
					windowHeight = 240f; // assume 1 title, 4 toggles, 2 sliders, 2 labels, 5 spacers, and 1 end. (Flight Only - includes debug menu)
				
				if (HighLogic.LoadedSceneIsEditor)
				{
					windowHeight = 48f; // assume 1 title, 1 toggle, and 1 end. (Editor Only)
					
					// In the editor the toolbar is at the bottom of the screen, so let's move it down
					windowTop = Screen.height - 42f - windowHeight; // 42f is the height of the toolbar buttons + 2 units of space
				}
                
				if (Equals(HighLogic.LoadedScene, GameScenes.SPACECENTER))
					windowHeight = 192f; // assume 1 top, 4 toggles, 1 slider. 1 label, 4 spacers, and 1 end. (Space Center Only)
				
				if (KFPersistenceManager.isDebugEnabled && Equals(HighLogic.LoadedScene, GameScenes.FLIGHT))
				{ // Add the height of any additional element you add to the debug menu, plus a spacer height for each element.
					windowHeight += 32f;
					if (KFPersistenceManager.isDustEnabled && KFPersistenceManager.isDustCameraEnabled)
						windowHeight += 48f; // Should be 80f
				}
				
				// Shift the window to the left until the left window border has the same x value as the button sprite or at least so far it won't clip out the edge of the monitor
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
			
			float localEndHeight = 240f; // Initial setting only, is overridden for each separate layout.
			
			const float LOCALWIDTH = 240f;
			const float DEBUGWIDTH = 234f;
			const float LOCALLEFT = 8f;
			const float DEBUGLEFT = 14f;
			
			// Reference:
			//	titleHeight = 16f;
			//	toggleHeight = 24f;
			//	sliderHeight = 16f;
			//	spacerHeight = 8f;
			//	labelHeight = 24f;
			//	endHeight = 8f;
			//	Format: Rect(left, top, width, height)
			
			if (HighLogic.LoadedSceneIsEditor) // No longer combined with space center. Was causing headaches when trying to position the elements for that scene and causing the editor window to be too high.
			{
				// Non-element title (top: 0)
				KFPersistenceManager.isMarkerEnabled = GUI.Toggle(new Rect(LOCALLEFT, 16f, LOCALWIDTH, TOGGLEHEIGHT), KFPersistenceManager.isMarkerEnabled, "Enable Orientation Markers");
				// Non-element spacer (top: 40)
				//GUI.Label(new Rect(localLeft, 48f, 240f, labelHeight), string.Format("<color=#ffffffff>Suspension Increment:</color> {0}", Extensions.RoundToNearestValue(KFPersistenceManager.suspensionIncrement, 5f)));
				//KFPersistenceManager.suspensionIncrement = GUI.HorizontalSlider(new Rect(localLeft, 72f, 240f, sliderHeight), Extensions.RoundToNearestValue(KFPersistenceManager.suspensionIncrement, 5f), 5f, 20f);
				// Non-element end (top: 88)
				localEndHeight = 48f; // end top height + spacerHeight.
			}
			
			if (HighLogic.LoadedSceneIsFlight)
			{
				// Non-element title (top: 0)
				KFPersistenceManager.isDustEnabled = GUI.Toggle(new Rect(LOCALLEFT, 16f, LOCALWIDTH, TOGGLEHEIGHT), KFPersistenceManager.isDustEnabled, "Enable DustFX");
				// Non-element spacer (top: 40)
				KFPersistenceManager.isDustCameraEnabled = GUI.Toggle(new Rect(LOCALLEFT, 48f, LOCALWIDTH, TOGGLEHEIGHT), KFPersistenceManager.isDustCameraEnabled, "Enable DustFX Camera");
				// Non-element spacer (top: 72)
				KFPersistenceManager.isRepLightEnabled = GUI.Toggle(new Rect(LOCALLEFT, 80f, LOCALWIDTH, TOGGLEHEIGHT), KFPersistenceManager.isRepLightEnabled, "Enable Repulsor Lights");
				// Non-element spacer (top: 104)
				GUI.Label(new Rect(LOCALLEFT, 112f, LOCALWIDTH, LABELHEIGHT), string.Format("<color=#ffffffff>Dust Amount:</color> {0}", KFPersistenceManager.dustAmount.RoundToNearestValue(0.25f)));
				KFPersistenceManager.dustAmount = GUI.HorizontalSlider(new Rect(LOCALLEFT, 136f, LOCALWIDTH, SLIDERHEIGHT), KFPersistenceManager.dustAmount.RoundToNearestValue(0.25f), 0f, 3f);
				// Non-element spacer (top: 152)
				GUI.Label(new Rect(LOCALLEFT, 160f, LOCALWIDTH, LABELHEIGHT), string.Format("<color=#ffffffff>Suspension Increment:</color> {0}", KFPersistenceManager.suspensionIncrement.RoundToNearestValue(5f)));
				KFPersistenceManager.suspensionIncrement = GUI.HorizontalSlider(new Rect(LOCALLEFT, 184f, LOCALWIDTH, SLIDERHEIGHT), KFPersistenceManager.suspensionIncrement.RoundToNearestValue(5f), 5f, 20f);
				// Non-element spacer (top: 200)
				KFPersistenceManager.isDebugEnabled = GUI.Toggle(new Rect(LOCALLEFT, 208f, LOCALWIDTH, TOGGLEHEIGHT), KFPersistenceManager.isDebugEnabled, "Enable Debug Options");
				// Non-element end (top: 232)
				localEndHeight = LOCALWIDTH; // end top height + spacerHeight.
			}
			
			if (Equals(HighLogic.LoadedScene, GameScenes.SPACECENTER))
			{
				// Non-element title (top: 0)
				KFPersistenceManager.isMarkerEnabled = GUI.Toggle(new Rect(LOCALLEFT, 16f, LOCALWIDTH, TOGGLEHEIGHT), KFPersistenceManager.isMarkerEnabled, "Enable Orientation Markers");
				// Non-element spacer (top: 40)
				KFPersistenceManager.isDustEnabled = GUI.Toggle(new Rect(LOCALLEFT, 48f, LOCALWIDTH, TOGGLEHEIGHT), KFPersistenceManager.isDustEnabled, "Enable DustFX");
				// Non-element spacer (top: 72)
				KFPersistenceManager.isDustCameraEnabled = GUI.Toggle(new Rect(LOCALLEFT, 80f, LOCALWIDTH, TOGGLEHEIGHT), KFPersistenceManager.isDustCameraEnabled, "Enable DustFX Camera");
				// Non-element spacer (top: 104)
				KFPersistenceManager.isRepLightEnabled = GUI.Toggle(new Rect(LOCALLEFT, 112f, LOCALWIDTH, TOGGLEHEIGHT), KFPersistenceManager.isRepLightEnabled, "Enable Repulsor Lights");
				// Non-element spacer (top: 136)
				GUI.Label(new Rect(LOCALLEFT, 144f, LOCALWIDTH, LABELHEIGHT), string.Format("<color=#ffffffff>Dust Amount:</color> {0}", KFPersistenceManager.dustAmount.RoundToNearestValue(0.25f)));
				KFPersistenceManager.dustAmount = GUI.HorizontalSlider(new Rect(LOCALLEFT, 168f, LOCALWIDTH, SLIDERHEIGHT), KFPersistenceManager.dustAmount.RoundToNearestValue(0.25f), 0f, 3f);
				// Non-element end (top: 184)
				localEndHeight = 192f; // end top height + spacerHeight.
			}
			
			if (KFPersistenceManager.isDebugEnabled && Equals(HighLogic.LoadedScene, GameScenes.FLIGHT)) // Only during flight for now.
			{
				// Add debug option here and indent them by 6, while shortening them by 6 as well.
				KFPersistenceManager.isWaterColliderVisible = GUI.Toggle(new Rect(DEBUGLEFT, localEndHeight, DEBUGWIDTH, TOGGLEHEIGHT), KFPersistenceManager.isWaterColliderVisible, "Waterslider Visible");
				if (KFPersistenceManager.isDustEnabled && KFPersistenceManager.isDustCameraEnabled)
				{
					GUI.Label(new Rect(DEBUGLEFT, (localEndHeight + SPACERHEIGHT), DEBUGWIDTH, LABELHEIGHT), string.Format("<color=#ffffffff>Camera Resolution:</color> {0}", Mathf.Clamp(KFPersistenceManager.cameraRes, 2f, 10f)));
					KFPersistenceManager.cameraRes = GUI.HorizontalSlider(new Rect(DEBUGLEFT, (localEndHeight + SPACERHEIGHT + LABELHEIGHT), DEBUGWIDTH, SLIDERHEIGHT), Mathf.Clamp(KFPersistenceManager.cameraRes, 2f, 10f), 2f, 10f);
					GUI.Label(new Rect(DEBUGLEFT, (localEndHeight + (SPACERHEIGHT * 2f) + LABELHEIGHT + SLIDERHEIGHT), DEBUGWIDTH, LABELHEIGHT), string.Format("<color=#ffffffff>Camera Framerate:</color> {0}", Mathf.Clamp(KFPersistenceManager.cameraFramerate, 5f, 20f)));
					KFPersistenceManager.cameraFramerate = GUI.HorizontalSlider(new Rect(DEBUGLEFT, (localEndHeight + (SPACERHEIGHT * 2f) + (LABELHEIGHT * 2f) + SLIDERHEIGHT), DEBUGWIDTH, SLIDERHEIGHT), Mathf.Clamp(KFPersistenceManager.cameraFramerate, 5f, 20f), 5f, 20f);
				}
			}
			else if (!KFPersistenceManager.isDebugEnabled) // Add an entry for each debug toggle option so that they will disable themselves when debug mode is disabled. Sliders do not need to be reset, however.
				KFPersistenceManager.isWaterColliderVisible = false;
		}
		
		#endregion GUI Setup
	}
}