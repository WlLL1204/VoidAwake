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
    public class VoidAwake_InpactCheer:Building
    {
        public override void TickLong()
        {
            base.TickLong();
            List<Thing> things = this.Position.GetThingList(this.Map);
            foreach(Thing thing in things)
            {
                Pawn pawn = thing as Pawn;
                if(pawn!=null)
                {
                    
                }
            }
        }
    }
}
