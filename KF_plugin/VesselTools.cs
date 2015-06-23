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
    class ModuleWaterSlider : VesselModule
    {
		readonly GameObject _collider = new GameObject("ModuleWaterSlider.Collider", typeof(BoxCollider), typeof(Rigidbody));
		const float triggerDistance = 25f;
        bool isActive;
        Vessel _vessel;
        public float colliderHeight = -2.5f;

        void Start()
        {
            print("WaterSlider start");
            _vessel = GetComponent<Vessel>();

            float repulsorCount = 0;
            foreach (Part PA in _vessel.parts)
            {
                foreach (KFRepulsor RA in PA.GetComponentsInChildren<KFRepulsor>())
                    repulsorCount++;
                foreach (RepulsorWheel RA in PA.GetComponentsInChildren<RepulsorWheel>())
                    repulsorCount++;
            }
            if (repulsorCount > 0)
                isActive = true;
            
            if (!isActive)
                return;

            var box = _collider.collider as BoxCollider;
			box.size = new Vector3(300f, .5f, 300f); // Probably should encapsulate other colliders in real code

            var rb = _collider.rigidbody;
            rb.isKinematic = true;

            _collider.SetActive(true);

            var visible = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visible.transform.parent = _collider.transform;
            visible.transform.localScale = box.size;
            visible.renderer.enabled = true; // enable to see collider
            UpdatePosition();
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
            if (Vector3.Distance(_collider.transform.position, _vessel.transform.position) > triggerDistance)
                UpdatePosition();
            colliderHeight = Mathf.Clamp((colliderHeight -= 0.1f), -10, 2.5f);
        }
    }
}
