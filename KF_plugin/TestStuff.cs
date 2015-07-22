using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFoundries
{
    public class TestStuff : PartModule
    {
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            this.part.GetPartRendererBound();
            Vector3 size = ShipConstruction.CalculateCraftSize(EditorLogic.fetch.ship);
        }
    }
}
