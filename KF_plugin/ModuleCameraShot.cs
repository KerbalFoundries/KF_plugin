using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>Controls the DustFX camera-based color picker.</summary>
	public class ModuleCameraShot : PartModule
	{
		// disable RedundantDefaultFieldInitializer
		// disable ConvertToConstant.Local
		
		int resWidth = 6;
		int resHeight = 6;
		public Color _averageColour = new Color(1f, 1f, 1f, 1f);
		
		int frameCount = 0;
		int frameThreshHold = 10;
		
		Vessel _vessel;
		GameObject _cameraObject;
		Camera _camera;
		Texture2D groundShot;
		RenderTexture renderTexture;
		
		bool dustCamEnabled;
		int kFPartCount;
		bool isReady;
		
		/// <summary>Local definition of the KFLogUtil class.</summary>
		readonly KFLogUtil KFLog = new KFLogUtil("ModuleCameraShot");
		
		/// <summary>The layers that the camera will render.</summary>
		public int cameraMask;
		
		public void StartUp()
		{
			#if DEBUG
			KFLog.Warning("ModuleCamerashot Start");
			#endif
			
			_vessel = GetComponent<Vessel>();
			foreach (Part PA in _vessel.parts)
			{
				// disable UnusedVariable.Compiler
				foreach (KFRepulsor RA in PA.GetComponentsInChildren<KFRepulsor>())
					kFPartCount++;
				foreach (KFModuleWheel RA in PA.GetComponentsInChildren<KFModuleWheel>())
					kFPartCount++;
			}
			if (kFPartCount > 1)
			{
				KFLog.Warning("Starting camera");
				
				_cameraObject = new GameObject("ColourCam");
				_cameraObject.transform.parent = _vessel.transform;
				_cameraObject.transform.LookAt(_vessel.mainBody.transform.position);
				_cameraObject.transform.Translate(new Vector3(0, 0, -10));
				_camera = _cameraObject.AddComponent<Camera>();
				_camera.targetTexture = renderTexture;
				cameraMask = 32784;
				_camera.cullingMask = cameraMask;
				
				_camera.enabled = false;
				renderTexture = new RenderTexture(resWidth, resHeight, 24);
				groundShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
				dustCamEnabled = KFPersistenceManager.isDustCameraEnabled;
				isReady = true;
			}
			if (!Equals(KFPersistenceManager.cameraRes, null))
			{
				resWidth = KFPersistenceManager.cameraRes;
				resHeight = KFPersistenceManager.cameraRes;
			}
			if (!Equals(KFPersistenceManager.cameraFramerate, null))
				frameThreshHold = KFPersistenceManager.cameraFramerate;
		}
		
		public void Update()
		{
			dustCamEnabled = KFPersistenceManager.isDustCameraEnabled;
			if (frameCount >= frameThreshHold && Equals(_vessel, FlightGlobals.ActiveVessel) && dustCamEnabled && isReady)
			{
				frameCount = 0;

				_cameraObject.transform.LookAt(_vessel.mainBody.transform.position);
				_camera.targetTexture = renderTexture;
				_camera.enabled = true;
                
				_camera.Render();
				RenderTexture.active = renderTexture;
				groundShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
				_camera.targetTexture = null;
				_camera.enabled = false;
				RenderTexture.active = null;

				Color[] texColors = groundShot.GetPixels();
				int total = texColors.Length;

				float r = texColors[0].r;
				float g = texColors[0].g;
				float b = texColors[0].b;
				float alpha = 0.014f;

				for (int i = 0; i < total; i++)
				{
					r += texColors[i].r;
					g += texColors[i].g;
					b += texColors[i].b;
				}
				_averageColour = new Color(r / total, g / total, b / total, alpha);
			}
			frameCount++;
		}
	}
}
