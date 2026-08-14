using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
	public class VoidAwake_Building_GhostShip : MapPortal
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

		public override bool IsEnterable(out string reason)
		{
			if (!CanEnter && !Prefs.DevMode)
			{
				reason = "VoidAwake_GhostShip_EnterLocked".Translate();
				return false;
			}

			return base.IsEnterable(out reason);
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo g in base.GetGizmos())
			{
				yield return g;
			}

			if (!Prefs.DevMode)
			{
				yield break;
			}

			yield return new Command_Action
			{
				defaultLabel = "VoidAwake_GhostShip_DevViewInterior".Translate(),
				defaultDesc = "VoidAwake_GhostShip_DevViewInteriorDesc".Translate(),
				action = DevJumpToInterior,
			};
		}

		public void DevJumpToInterior()
		{
			Map other = GetOtherMap();
			if (other == null)
			{
				Messages.Message("VoidAwake_GhostShip_DevViewInteriorFailed".Translate(), this, MessageTypeDefOf.RejectInput, false);
				return;
			}

			IntVec3 cell = GetDestinationLocation();
			if (!cell.IsValid)
			{
				cell = other.Center;
			}

			CameraJumper.TryJump(cell, other, CameraJumper.MovementMode.Pan);
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

		protected override Map GeneratePocketMapInt()
		{
			int fallback = def?.portal?.pocketMapSize ?? 40;
			IntVec3 size = VoidAwake_GhostShipMapUtility.MapSizeOrFallback(fallback);
			return PocketMapUtility.GeneratePocketMap(size, def.portal.pocketMapGenerator, GetExtraGenSteps(), Map);
		}
	}
}
