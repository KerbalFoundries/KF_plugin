using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class RockSpawner : PartModule
    {
        List<WheelCollider> wcList = new List<WheelCollider>();
        List<Vector3> positionList = new List<Vector3>();
        Transform _spawnPosition;

        [KSPField]
        public string spawnObject; // "NASAmission/Parts/PotatoRoid/PotatoRoid" (kinda obsolete now)

        [KSPField]
        public string spawnPoint;
        [KSPField]
        public float scale = 1f;

        public override void OnStart(PartModule.StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
            	//Nothing here?
            }
            base.OnStart(state);
        }

        [KSPEvent(active = true, guiActive = true, name = "Spawn Rock")]
        public void SpawnRock()
        {
            _spawnPosition = this.part.FindModelTransform(spawnPoint);
            Debug.LogWarning("RockSpawner");

            GameObject _rockPrefab = GameDatabase.Instance.GetModel(spawnObject);
            _rockPrefab.SetActive(true);
			Debug.LogWarning(string.Format("Rock scale is{0}", _rockPrefab.transform.localScale));
            _rockPrefab.transform.localScale = new Vector3(scale, scale, scale); 
			Debug.LogWarning(string.Format("Rock scale is{0}", _rockPrefab.transform.localScale));

            var rb = _rockPrefab.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
            rb.angularDrag = 5f;
            
            Debug.LogWarning("rigidbody added");
            /*
            var _rockCollider = _rockPrefab.transform.Search("potatoroid");
            MeshCollider _meshC = _rockCollider.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
            if(_meshC != null)
                Debug.LogWarning("transform found");

            _meshC.collider.attachedRigidbody.detectCollisions = true;
            _meshC.convex = true;
            
            _meshC.collider.material = new PhysicMaterial
            {
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Average,
                bounciness = 0.45f,
                dynamicFriction = 0.05f,
                staticFriction = 0.25f
            };
            */
 
            _rockPrefab.SetActive(false);
            Debug.LogWarning("Friction set");
            //Part rock = FlightIntegrator.Instantiate(_rockPrefab, _spawnPosition.transform.position, new Quaternion(0, 0, 0, 0)) as Part;
            var rock = (GameObject)Instantiate(_rockPrefab, _spawnPosition.transform.position, new Quaternion(0, 0, 0, 0));
            Debug.LogWarning("Object instantiated");
            rock.gameObject.AddComponent<physicalObject>();
            //var _rockEnhancer = rock.gameObject.AddComponent<CollisionEnhancer>();
            //_rockEnhancer.part = rock.;
            Debug.LogWarning("rock set");
            
            rock.gameObject.SetActive(true);
            //Debug.LogWarning("Object activated");

            UnityEngine.Object.Destroy(_rockPrefab);
            Debug.LogWarning("Prefab destroyed");
        }
    }
}
