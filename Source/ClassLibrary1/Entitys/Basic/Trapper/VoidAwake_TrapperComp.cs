using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	public enum TrapperCombatState : byte
	{
		Stealth = 0,
		Combat = 1
	}

	public class CompProperties_VoidAwake_Trapper : CompProperties
	{
		public int placeCooldownTicks = 2500; // ~1 in-game hour
		public int stealthReturnDelayTicks = 180; // ~3 seconds after no reachable colonists
		public int combatMinDurationTicks = 2500; // combat lasts at least ~1 in-game hour

		public CompProperties_VoidAwake_Trapper()
		{
			compClass = typeof(VoidAwake_TrapperComp);
		}
	}

	public class VoidAwake_TrapperComp : ThingComp
	{
		private int ticksUntilNextPlace;
		private TrapperCombatState state = TrapperCombatState.Stealth;
		private bool chainPlacing;
		private IntVec3 lastTrapCell = IntVec3.Invalid;
		private IntVec3 chainDoorCell = IntVec3.Invalid;
		private int ticksUntilStealthReturn;
		private int ticksUntilCombatUnlock;

		public CompProperties_VoidAwake_Trapper Props => (CompProperties_VoidAwake_Trapper)props;

		public Pawn Pawn => (Pawn)parent;

		public TrapperCombatState State => state;

		public bool IsStealth => state == TrapperCombatState.Stealth;

		public bool IsCombat => state == TrapperCombatState.Combat;

		/// <summary>Legacy alias: true while in Combat.</summary>
		public bool Revealed => IsCombat;

		public bool ChainPlacing => chainPlacing;

		public IntVec3 LastTrapCell => lastTrapCell;

		public IntVec3 ChainDoorCell => chainDoorCell;

		public bool CanPlaceTrapNow
		{
			get
			{
				if (!IsStealth)
				{
					return false;
				}

				if (!chainPlacing && ticksUntilNextPlace > 0)
				{
					return false;
				}

				return true;
			}
		}

		public void BeginDoorChain(IntVec3 doorCell)
		{
			chainDoorCell = doorCell;
			chainPlacing = true;
		}

		public void Notify_TrapPlaced(IntVec3 cell)
		{
			lastTrapCell = cell;
			if (IsStealth)
			{
				EnsureStealthHediff();
			}
		}

		public void Notify_ChainContinue()
		{
			chainPlacing = true;
		}

		public void Notify_ChainEnded()
		{
			chainPlacing = false;
			chainDoorCell = IntVec3.Invalid;
			lastTrapCell = IntVec3.Invalid;
			ticksUntilNextPlace = Props.placeCooldownTicks;
		}

		public void EnterCombat()
		{
			state = TrapperCombatState.Combat;
			ticksUntilStealthReturn = 0;
			ticksUntilCombatUnlock = Props.combatMinDurationTicks;
			chainPlacing = false;
			chainDoorCell = IntVec3.Invalid;
			lastTrapCell = IntVec3.Invalid;
			RemoveStealthHediff();
		}

		/// <summary>Legacy name used by older call sites.</summary>
		public void Reveal() => EnterCombat();

		public void EnterStealth()
		{
			state = TrapperCombatState.Stealth;
			ticksUntilStealthReturn = 0;
			ticksUntilCombatUnlock = 0;
			chainPlacing = false;
			chainDoorCell = IntVec3.Invalid;
			lastTrapCell = IntVec3.Invalid;
			EnsureStealthHediff();
		}

		public override void CompTick()
		{
			if (ticksUntilNextPlace > 0)
			{
				ticksUntilNextPlace--;
			}

			if (!Pawn.Spawned || Pawn.Dead)
			{
				return;
			}

			if (IsStealth)
			{
				EnsureStealthHediff();
				return;
			}

			if (ticksUntilCombatUnlock > 0)
			{
				ticksUntilCombatUnlock--;
				ticksUntilStealthReturn = 0;
				return;
			}

			if (Find.TickManager.TicksGame % 30 != 0)
			{
				return;
			}

			if (HasReachableColonist())
			{
				ticksUntilStealthReturn = 0;
				return;
			}

			ticksUntilStealthReturn += 30;
			if (ticksUntilStealthReturn >= Props.stealthReturnDelayTicks)
			{
				EnterStealth();
			}
		}

		private bool HasReachableColonist()
		{
			Map map = Pawn.Map;
			if (map == null)
			{
				return false;
			}

			IReadOnlyList<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
			for (int i = 0; i < colonists.Count; i++)
			{
				Pawn colonist = colonists[i];
				if (colonist == null || colonist.Dead || colonist.Downed || !colonist.RaceProps.Humanlike)
				{
					continue;
				}

				if (Pawn.CanReach(colonist, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
				{
					return true;
				}
			}

			return false;
		}

		public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
		{
			if (totalDamageDealt > 0f && !Pawn.Dead)
			{
				EnterCombat();
			}
		}

		public void EnsureStealth() => EnsureStealthHediff();

		private void EnsureStealthHediff()
		{
			if (!IsStealth || Pawn.health?.hediffSet == null)
			{
				return;
			}

			if (Pawn.health.hediffSet.HasHediff(VoidAwake_TrapperDefOf.VoidAwake_TrapperStealth))
			{
				return;
			}

			Pawn.health.AddHediff(VoidAwake_TrapperDefOf.VoidAwake_TrapperStealth);
		}

		private void RemoveStealthHediff()
		{
			if (Pawn.health?.hediffSet == null)
			{
				return;
			}

			Hediff hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(VoidAwake_TrapperDefOf.VoidAwake_TrapperStealth);
			if (hediff != null)
			{
				Pawn.health.RemoveHediff(hediff);
			}
		}

		public override void PostExposeData()
		{
			Scribe_Values.Look(ref ticksUntilNextPlace, "ticksUntilNextPlace", 0);
			Scribe_Values.Look(ref chainPlacing, "chainPlacing", false);
			Scribe_Values.Look(ref lastTrapCell, "lastTrapCell", IntVec3.Invalid);
			Scribe_Values.Look(ref chainDoorCell, "chainDoorCell", IntVec3.Invalid);
			Scribe_Values.Look(ref ticksUntilStealthReturn, "ticksUntilStealthReturn", 0);
			Scribe_Values.Look(ref ticksUntilCombatUnlock, "ticksUntilCombatUnlock", 0);
			Scribe_Values.Look(ref state, "trapperState", TrapperCombatState.Stealth);

			bool revealed = IsCombat;
			Scribe_Values.Look(ref revealed, "revealed", false);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && revealed)
			{
				state = TrapperCombatState.Combat;
			}
		}
	}
}
