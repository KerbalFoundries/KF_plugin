using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFRepulsorLook :PartModule
    {
        
        [KSPField]
        public string gridName;

        bool isReady;
        Transform _grid;
        KFRepulsor _repulsor;
        Vector3 _gridScale;

        public override void OnStart(PartModule.StartState state)
        {
 	        base.OnStart(state);
            _grid = transform.Search(gridName);
            _repulsor = this.part.GetComponentInChildren<KFRepulsor>();
            _gridScale = _grid.transform.localScale;
            isReady = true;
        }

        public void FixedUpdate()
        {
            if (!isReady)
                return;
            _grid.transform.LookAt(FlightGlobals.ActiveVessel.mainBody.transform.position);
            
        }

        public void UpdateSettings()
        {
            if(_repulsor.Rideheight < 0.1f)
                StartCoroutine("Shrink");
        }

        IEnumerator Shrink() //Coroutine for steering
        {
            while (_grid.transform.localScale.x > 0.1f)
            {
                _grid.transform.localScale -= (_gridScale / 50);
                yield return null;
            }
            Debug.LogWarning("Finished shrinking");
        }

        IEnumerator Grow() //Coroutine for steering
        {
            while (_grid.transform.localScale.x < _gridScale.x)
            {
                _grid.transform.localScale += (_gridScale / 50);
                yield return null;
            }
            Debug.LogWarning("Finished growing");
        }
        
        
    }
}
