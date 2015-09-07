/* KSP [0.23.5] Anti-Grav Repulsor plugin by Lo-Fi
 * HUGE thanks to xEvilReeperx for this water code, along with gerneral coding help along the way!
 * Still works in 1.0.4!
 */

using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>Controls the ability of the Repulsor to repulse against a water surface.</summary>
	public class ModuleWaterSlider : PartModule
	{
		// disable ConvertToConstant.Local
		
		public float colliderHeight = -2.5f;
		GameObject _collider;

		float triggerDistance = 25f;

		Vessel _vessel;
		Vector3 boxSize = new Vector3(300f, .5f, 300f);

		bool isReady;

		/// <summary>Local name of the KFLogUtil class.</summary>
		readonly KFLogUtil KFLog = new KFLogUtil("ModuleWaterSlider");

		GameObject waterSliderSurface;

		public void StartUp()
		{
			#if DEBUG
			KFLog.Log("WaterSlider start.");
			#endif
			
			_vessel = GetComponent<Vessel>();
			_collider = new GameObject("ModuleWaterSlider.Collider");
			
			//KFLog.Log("Continuing...");
			
			waterSliderSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
			waterSliderSurface.transform.parent = _collider.transform;
			waterSliderSurface.transform.localScale = boxSize;
			waterSliderSurface.renderer.enabled = false;
			
			var box = (BoxCollider)_collider.AddComponent("BoxCollider");
			box.size = boxSize;
			
			// Probably should encapsulate other colliders in real code.
			
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
			waterSliderSurface.renderer.enabled = KFPersistenceManager.debugIsWaterColliderVisible; // NEW: Enabled and disabled via a debug option in the GUI settings window.  Turns off if debug is turned off, otherwise stays persistent.
		}
	}
}


