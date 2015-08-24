/*
 * KSP [0.23.5] Anti-Grav Repulsor plugin by Lo-Fi
 * HUGE thanks to xEvilReeperx for this water code, along with gerneral coding help along the way!
 * Still works in 1.0.2!
 */

using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	public class ModuleWaterSlider : PartModule
	{
        public float colliderHeight = -2.5f;

        GameObject _collider;
		// disable once ConvertToConstant.Local
		float triggerDistance = 25f;
		//bool isActive; // never used.
		Vessel _vessel;
		Vector3 boxSize = new Vector3(300f, .5f, 300f);
		
		bool isReady;
		
		/// <summary>Local name of the KFLogUtil class.</summary>
		readonly KFLogUtil KFLog = new KFLogUtil("ModuleWaterSlider");

		GameObject visibleSliderSurface;
		
		public void StartUp()
		{
			KFLog.Log("WaterSlider start.");
			_vessel = GetComponent<Vessel>();

            _collider = new GameObject("ModuleWaterSlider.Collider");
            KFLog.Log("Continuing...");

			visibleSliderSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visibleSliderSurface.transform.parent = _collider.transform;
            visibleSliderSurface.transform.localScale = boxSize;
			visibleSliderSurface.renderer.enabled = false;

            var box = (BoxCollider)_collider.AddComponent("BoxCollider");
            box.size = boxSize; // Probably should encapsulate other colliders in real code

            var rb = (Rigidbody)_collider.AddComponent("Rigidbody");
            rb.rigidbody.isKinematic = true;

            _collider.SetActive(true);

            UpdatePosition();
     	       isReady = true;
		}

		void UpdatePosition()
		{
			Vector3d oceanNormal = _vessel.mainBody.GetSurfaceNVector(_vessel.latitude, _vessel.longitude);
			Vector3 newPosition = (_vessel.ReferenceTransform.position - oceanNormal * (FlightGlobals.getAltitudeAtPos(_vessel.ReferenceTransform.position) - colliderHeight));
			_collider.rigidbody.position = newPosition;
			_collider.rigidbody.rotation = Quaternion.LookRotation(oceanNormal) * Quaternion.AngleAxis(90f, Vector3.right);
		}
		
		void FixedUpdate()
		{
			if (!isReady)
				return;
			if (Vector3.Distance(_collider.transform.position, _vessel.transform.position) > triggerDistance)
				UpdatePosition();
			colliderHeight = Mathf.Clamp((colliderHeight -= 0.1f), -10, 2.5f);
			visibleSliderSurface.renderer.enabled = KFPersistenceManager.debugIsWaterColliderVisible; // NEW: Enabled and disabled via a debug option in the GUI settings window.  Turns off if debug is turned off, otherwise stays persistent.
		}
    }

	public class ModuleCameraShot : PartModule
	{
		// disable RedundantDefaultFieldInitializer
		// disable ConvertToConstant.Local
		
		int resWidth = 6;
		int resHeight = 6;
		public Color _averageColour = new Color(1f, 1f, 1f, 1f);
		
        int frameCount = 0;
		int threshHold = 10;
		Vessel _vessel;
		GameObject _cameraObject;
		Camera _camera;
		Texture2D groundShot;
		RenderTexture renderTexture;
		bool dustCam;
        int kFPartCount;
        bool isReady;

		/// <summary>Local definition of the KFLogUtil class.</summary>
		readonly KFLogUtil KFLog = new KFLogUtil("ModuleCameraShot");

		/// <summary>The layers that the camera will render.</summary>
		public int cameraMask;

		public void StartUp()
		{
            KFLog.Warning("ModuleCamerashot Start");
            _vessel = GetComponent<Vessel>();
            foreach (Part PA in _vessel.parts)
            {
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
                cameraMask = 32784;	// Layers 4 and 15, or water and local scenery.
                // Generated from the binary place value output of 4 and 15 added to each other.
                // (1 << 4) | (1 << 15) = (16) | (32768) = 32784
                _camera.cullingMask = cameraMask;

                _camera.enabled = false;
                renderTexture = new RenderTexture(resWidth, resHeight, 24);
                groundShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
                dustCam = KFPersistenceManager.isDustCameraEnabled;
                isReady = true;
            }
		}

		public void Update()
		{
			dustCam = KFPersistenceManager.isDustCameraEnabled;
			if (frameCount >= threshHold && Equals(_vessel, FlightGlobals.ActiveVessel) && dustCam && isReady)
			{
				frameCount = 0;

				_cameraObject.transform.LookAt(_vessel.mainBody.transform.position);
				_camera.targetTexture = renderTexture;
				//Extensions.DebugLine(_cameraObject.transform.position, _cameraObject.transform.eulerAngles);
				_camera.enabled = true;
                
				_camera.Render();
				//KFLog.Error("Rendered something...");
				RenderTexture.active = renderTexture;
				groundShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
				_camera.targetTexture = null;
				_camera.enabled = false;
				RenderTexture.active = null; // JC: added to avoid errors

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
				//KFLog.Log(string.Format("Color: {0}r {1}g {2}b ", r, g, b));
				_averageColour = new Color(r / total, g / total, b / total, alpha);
				//KFLog.Log(string.Format("Variable \"_averageColour\" = \"{0}.\"", _averageColour));
			}
			frameCount++;
		}
	}
}
