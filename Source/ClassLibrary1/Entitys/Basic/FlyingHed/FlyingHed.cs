using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;
using UnityEngine;

namespace VoidAwake
{
    internal class VoidAwake_FlyingHed:Pawn
    {

        public override void TickLong()
        {
            List<Thing> things = this.Position.GetThingList(this.Map);
        }
        
    }
}
