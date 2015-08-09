/*
 * KSP [0.23.5] Anti-Grav Repulsor plugin by Lo-Fi
 * HUGE thanks to xEvilReeperx for this water code, along with gerneral coding help along the way!
 * Still works in 1.0.2!
 */

using System;
using System.Linq;
using UnityEngine;
using System.Collections;

namespace KerbalFoundries
{
	public class ModuleWaterSlider : VesselModule
	{
        GameObject _collider;
		const float triggerDistance = 25f;
		bool isActive;
		Vessel _vessel;
		Vector3 boxSize = new Vector3(300f, .5f, 300f);
		public float colliderHeight = -2.5f;
		bool isReady;
		
		/// <summary>Local name of the KFLogUtil class.</summary>
		readonly KFLogUtil KFLog = new KFLogUtil("ModuleWaterSlider");

		void Start()
		{
			KFLog.Log("WaterSlider start.");
			_vessel = GetComponent<Vessel>();
		
			float repulsorCount = 0;
			foreach (Part PA in _vessel.parts)
			{
				foreach (KFRepulsor RA in PA.GetComponentsInChildren<KFRepulsor>())
					repulsorCount++;
				foreach (RepulsorWheel RA in PA.GetComponentsInChildren<RepulsorWheel>())
					repulsorCount++;
			}

			isActive |= repulsorCount > 0;

            if (isActive && _vessel.isCommandable)
            {
                _collider = new GameObject("ModuleWaterSlider.Collider");
                Debug.LogError("Continuing...");

                var visible = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visible.transform.parent = _collider.transform;
                visible.transform.localScale = boxSize;
                visible.renderer.enabled = true; // enable to see collider

                BoxCollider box = _collider.AddComponent("BoxCollider") as BoxCollider;
                box.size = boxSize; // Probably should encapsulate other colliders in real code

                Rigidbody rb = _collider.AddComponent("Rigidbody") as Rigidbody;
                rb.rigidbody.isKinematic = true;

                _collider.SetActive(true);

                UpdatePosition();
                isReady = true;
            }

            else
            {
                Destroy(_collider);
                KFLog.Log("Setting size to zero and returning.");
                return;
            }
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
			if (Vector3.Distance(_collider.transform.position, _vessel.transform.position) > triggerDistance && isReady)
				UpdatePosition();
			colliderHeight = Mathf.Clamp((colliderHeight -= 0.1f), -10, 2.5f);
		}
	}

	public class ModuleCameraShot : VesselModule
	{
		// disable RedundantDefaultFieldInitializer
		
		const int resWidth = 6;
		const int resHeight = 6;
		public Color _averageColour = new Color(1f, 1f, 1f, 1f);
		int frameCount = 0;
		const int threshHold = 1;
		Vessel _vessel;
		GameObject _cameraObject;
		Camera _camera;
		Texture2D groundShot;
		RenderTexture renderTexture;
		bool dustCam;

		/// <summary>Local definition of the KFLogUtil class.</summary>
		readonly KFLogUtil KFLog = new KFLogUtil("ModuleCameraShot");

		/// <summary>The layers that the camera will render.</summary>
		public int cameraMask;

		void Start()
		{
			_vessel = GetComponent<Vessel>();
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
		}

		public void Update()
		{
			dustCam = KFPersistenceManager.isDustCameraEnabled;
			if (frameCount >= threshHold && _vessel == FlightGlobals.ActiveVessel && dustCam)
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
				const float alpha = 0.014f;

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
