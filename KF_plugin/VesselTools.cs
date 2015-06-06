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
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class RepulsorSkim : MonoBehaviour
    {
        void Start()
        {
            print("RepulsorSkim Start");
            int partCount = 0;
            int repulsorCount = 0;
            
            foreach (Part PA in FlightGlobals.ActiveVessel.Parts)
            {
                partCount++;
                foreach (RepulsorTest RA in PA.GetComponentsInChildren<RepulsorTest>())
                    repulsorCount++;

                foreach (KFRepulsor RA in PA.GetComponentsInChildren<KFRepulsor>())
                    repulsorCount++;

                foreach (RepulsorWheel RA in PA.GetComponentsInChildren<RepulsorWheel>())
                    repulsorCount++;
                }
            
            if (repulsorCount > 0)
                FlightGlobals.ActiveVessel.rootPart.AddModule("ModuleWaterSlider"); 
            }
        }

    public class ModuleWaterSlider : PartModule
    {
		readonly GameObject _collider = new GameObject("ModuleWaterSlider.Collider", typeof(BoxCollider), typeof(Rigidbody));
		const float triggerDistance = 25f;
		// Avoid moving every frame

        public float colliderHeight = -2.5f;

        void Start()
        {
            print("WaterSlider start");

            var box = _collider.collider as BoxCollider;
			box.size = new Vector3(300f, .5f, 300f); // Probably should encapsulate other colliders in real code
			/*
			The line above reports that a NullReferenceException will occur when "using memver of a null reference."
			Might want to look into this.  I's the "box.size" part that it doesn't like, and gives no suggestions
			on how to fix. - Gaalidas
			 */

            var rb = _collider.rigidbody;
            rb.isKinematic = true;

            _collider.SetActive(true);

            var visible = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visible.transform.parent = _collider.transform;
            visible.transform.localScale = box.size;
            visible.renderer.enabled = false; // enable to see collider
            //currentColliderHeight = 3;
            UpdatePosition();
        }

        void UpdatePosition()
        {
            Vector3d oceanNormal = this.part.vessel.mainBody.GetSurfaceNVector(vessel.latitude, vessel.longitude);
                
            //print(colliderHeight);
			Vector3 newPosition = (this.part.vessel.ReferenceTransform.position - oceanNormal * (FlightGlobals.getAltitudeAtPos(this.part.vessel.ReferenceTransform.position) - colliderHeight));
            //newPosition.x -= colliderHeight;
            _collider.rigidbody.position = newPosition;
            _collider.rigidbody.rotation = Quaternion.LookRotation(oceanNormal) * Quaternion.AngleAxis(90f, Vector3.right);
        }

        void FixedUpdate()
        {
            if (Vector3.Distance(_collider.transform.position, this.part.transform.position) > triggerDistance)
                UpdatePosition();
            colliderHeight = Mathf.Clamp((colliderHeight -= 0.1f), -10, 2.5f);
            //print(colliderHeight);
        }
    }
}
