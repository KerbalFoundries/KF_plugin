using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>Controls the DustFX camera-based color picker.</summary>
	public class KFModuleCameraShot : PartModule
	{
		// disable RedundantDefaultFieldInitializer
		// disable ConvertToConstant.Local
		
		float fResWidth = 6f;
		float fResHeight = 6f;
		float fFrameCount = 0f;
		float fFrameThreshHold = 10f;
		
		public Color _averageColour = new Color(1f, 1f, 1f, 1f);
		
		Vessel _vessel;
		GameObject _cameraObject;
		Camera _camera;
		Texture2D groundShot;
		RenderTexture renderTexture;
		
		bool dustCamEnabled, isReady;
		int kFPartCount;
		
		/// <summary>Local definition of the KFLogUtil class.</summary>
		readonly KFLogUtil KFLog = new KFLogUtil("ModuleCameraShot");
		
		/// <summary>The layers that the camera will render.</summary>
		public int cameraMask = 32784;
		
		public void StartUp()
		{
			#if DEBUG
			KFLog.Warning("ModuleCamerashot Start");
			#endif
			
			if (!Equals(KFPersistenceManager.cameraRes, null))
			{
				fResWidth = KFPersistenceManager.cameraRes;
				fResHeight = KFPersistenceManager.cameraRes;
			}
			if (!Equals(KFPersistenceManager.cameraFramerate, null))
				fFrameThreshHold = KFPersistenceManager.cameraFramerate;
			
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
				#if DEBUG
				KFLog.Log("Starting camera");
				#endif
				
				_cameraObject = new GameObject("ColourCam");
				_cameraObject.transform.parent = _vessel.transform;
				_cameraObject.transform.LookAt(_vessel.mainBody.transform.position);
				_cameraObject.transform.Translate(new Vector3(0, 0, -10));
				_camera = _cameraObject.AddComponent<Camera>();
				_camera.targetTexture = renderTexture;
				_camera.cullingMask = cameraMask;
				
				_camera.enabled = false;
				renderTexture = new RenderTexture(Convert.ToInt32(fResWidth), Convert.ToInt32(fResHeight), 24);
				groundShot = new Texture2D(Convert.ToInt32(fResWidth), Convert.ToInt32(fResHeight), TextureFormat.RGB24, false);
				dustCamEnabled = KFPersistenceManager.isDustCameraEnabled;
				isReady = true;
			}
		}
		
		public void Update()
		{
			dustCamEnabled = KFPersistenceManager.isDustCameraEnabled;
			if (fFrameCount >= fFrameThreshHold && Equals(_vessel, FlightGlobals.ActiveVessel) && dustCamEnabled && isReady)
			{
				fFrameCount = 0;
				
				_cameraObject.transform.LookAt(_vessel.mainBody.transform.position);
				_camera.targetTexture = renderTexture;
				_camera.enabled = true;
                
				_camera.Render();
				RenderTexture.active = renderTexture;
				groundShot.ReadPixels(new Rect(0, 0, fResWidth, fResHeight), 0, 0);
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
			fFrameCount++;
		}
	}
}
