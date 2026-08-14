using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
	public class VoidAwake_Building_GhostShip : Building
	{
		public bool CanEnter;
		private Vector3 orbitDrawPos;
		private bool useOrbitDraw;

		public override Vector3 DrawPos
		{
			get
			{
				if (!useOrbitDraw)
				{
					return base.DrawPos;
				}

				Vector3 p = orbitDrawPos;
				p.y = base.DrawPos.y;
				return p;
			}
		}

		public void SetOrbitDraw(Vector3 drawPos, Rot4 rot)
		{
			orbitDrawPos = drawPos;
			useOrbitDraw = true;
			Rotation = rot;
		}

		public void ClearOrbitDraw()
		{
			useOrbitDraw = false;
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo g in base.GetGizmos())
			{
				yield return g;
			}

			Command_Action enter = new Command_Action
			{
				defaultLabel = "VoidAwake_GhostShip_Enter".Translate(),
				defaultDesc = "VoidAwake_GhostShip_EnterDesc".Translate(),
				action = TryEnterStub,
			};
			if (!CanEnter)
			{
				enter.Disable("VoidAwake_GhostShip_EnterLocked".Translate());
			}

			yield return enter;
		}

		private void TryEnterStub()
		{
			if (!CanEnter)
			{
				return;
			}

			Log.Message("[VoidAwake] Ghost ship enter stub: portal not implemented yet.");
			Messages.Message("VoidAwake_GhostShip_EnterStub".Translate(), this, MessageTypeDefOf.NeutralEvent, false);
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			string status = CanEnter
				? "VoidAwake_GhostShip_StatusUnlocked".Translate()
				: "VoidAwake_GhostShip_StatusOrbiting".Translate();
			return text.NullOrEmpty() ? status : text + "\n" + status;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref CanEnter, "canEnter", false);
		}
	}
}
