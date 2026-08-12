using System.Collections.Generic;

using RimWorld;

using Verse;

using Verse.AI;



namespace VoidAwake

{

	public enum VoidAwake_TrapperCombatState : byte

	{

		Stealth = 0,

		Combat = 1,

		Kidnap = 2

	}



	public class VoidAwake_CompProperties_Trapper : CompProperties

	{

		public int placeCooldownTicks = 2500; // ~1 in-game hour

		public int stealthReturnDelayTicks = 180; // ~3 seconds after no reachable colonists

		public int combatMinDurationTicks = 2500; // combat lasts at least ~1 in-game hour

		public int footprintIntervalCells = 2; // stealth footprints every N cells moved

		public float footprintScale = 0.65f;

		public int passageSearchRetryTicks = 600; // wait before retrying a failed rabbit passage scan

		public int passageUseTicks = 120; // time to slip through a rabbit passage

		public int kidnapPassageUseTicks = 240; // time to slip through a rabbit passage while carrying a kidnapped pawn
		public int kidnapRetryTicks = 120; // wait before retrying a failed kidnap job

		public VoidAwake_CompProperties_Trapper()

		{

			compClass = typeof(VoidAwake_CompTrapper);

		}

	}



	public class VoidAwake_CompTrapper : ThingComp

	{

		private int ticksUntilNextPlace;

		private VoidAwake_TrapperCombatState state = VoidAwake_TrapperCombatState.Stealth;

		private bool chainPlacing;

		private IntVec3 lastTrapCell = IntVec3.Invalid;

		private IntVec3 chainDoorCell = IntVec3.Invalid;

		private int ticksUntilStealthReturn;

		private int ticksUntilCombatUnlock;

		private IntVec3 lastFootprintCell = IntVec3.Invalid;

		private int cellsSinceFootprint;

		private bool wantsEscapeOutside;

		private IntVec3 waitAnchorCell = IntVec3.Invalid;

		private int nextPassageSearchTick;
		private int nextKidnapJobTick;

		private Pawn kidnapTarget;

		private VoidAwake_TrapperCombatState stateBeforeKidnap = VoidAwake_TrapperCombatState.Stealth;



		public VoidAwake_CompProperties_Trapper Props => (VoidAwake_CompProperties_Trapper)props;



		public Pawn Pawn => (Pawn)parent;



		public Pawn KidnapTarget => kidnapTarget;



		public VoidAwake_TrapperCombatState State => state;



		public bool IsStealth => state == VoidAwake_TrapperCombatState.Stealth;



		public bool IsCombat => state == VoidAwake_TrapperCombatState.Combat;



		public bool IsKidnap => state == VoidAwake_TrapperCombatState.Kidnap;



		/// <summary>Legacy alias for <see cref="IsKidnap"/>.</summary>

		public bool IsKidnapping => IsKidnap;



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



		public bool WantsEscapeOutside => wantsEscapeOutside;



		public IntVec3 WaitAnchorCell => waitAnchorCell;



		/// <summary>Throttles the passage search so a failed scan does not rerun every think tick.</summary>

		public bool CanSearchPassageNow => Find.TickManager.TicksGame >= nextPassageSearchTick;

		/// <summary>Throttles kidnap job re-assignment after a failed attempt.</summary>
		public bool CanGiveKidnapJobNow => Find.TickManager.TicksGame >= nextKidnapJobTick;

		public void Notify_PassageSearchFailed()

		{

			nextPassageSearchTick = Find.TickManager.TicksGame + Props.passageSearchRetryTicks;

		}

		public void Notify_KidnapJobFailed()
		{
			int now = Find.TickManager.TicksGame;
			if (now >= nextKidnapJobTick)
			{
				nextKidnapJobTick = now + Props.kidnapRetryTicks;
			}
		}

		public void Notify_KidnapJobStarted()
		{
			int blockUntil = Find.TickManager.TicksGame + 1;
			if (nextKidnapJobTick < blockUntil)
			{
				nextKidnapJobTick = blockUntil;
			}
		}

		public void Notify_PassageCreated(IntVec3 outerCell)

		{

			if (outerCell.IsValid)

			{

				waitAnchorCell = outerCell;

			}

		}



		public void Notify_UsedPassage(IntVec3 exitCell)

		{

			if (wantsEscapeOutside && exitCell.IsValid)

			{

				waitAnchorCell = exitCell;

			}

		}



		public void Notify_EscapedOutside(IntVec3 anchor)

		{

			wantsEscapeOutside = false;

			if (anchor.IsValid)

			{

				waitAnchorCell = anchor;

			}

		}



		public void BeginDoorChain(IntVec3 doorCell)

		{

			if (!VoidAwake_TrapperUtility.TryReserveDoor(Pawn.Map, doorCell, Pawn))

			{

				return;

			}



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

			// Keep door reservation so other trappers do not start on the same door.

			chainPlacing = false;

			chainDoorCell = IntVec3.Invalid;

			lastTrapCell = IntVec3.Invalid;

			ticksUntilNextPlace = Props.placeCooldownTicks;

			wantsEscapeOutside = true;

		}



		public void EnterKidnap(Pawn target)

		{

			if (state != VoidAwake_TrapperCombatState.Kidnap)
			{
				stateBeforeKidnap = state;
			}

			state = VoidAwake_TrapperCombatState.Kidnap;

			kidnapTarget = target;

			chainPlacing = false;

			chainDoorCell = IntVec3.Invalid;

			lastTrapCell = IntVec3.Invalid;

			wantsEscapeOutside = false;

			waitAnchorCell = IntVec3.Invalid;

			nextPassageSearchTick = 0;

			ResetFootprintTracking();

			RemoveStealthHediff();

			ApplyKidnappingHediff(target);

		}

		public void SetKidnapTarget(Pawn target)
		{
			kidnapTarget = target;
			ApplyKidnappingHediff(target);
		}

		public void ExitKidnap()

		{

			kidnapTarget = null;

			RemoveKidnappingHediff();

			VoidAwake_TrapperCombatState resume = stateBeforeKidnap;
			stateBeforeKidnap = VoidAwake_TrapperCombatState.Stealth;

			if (resume == VoidAwake_TrapperCombatState.Combat)
			{
				RestoreCombatAfterFailedKidnap();
			}
			else
			{
				EnterStealth();
			}

		}

		private void RestoreCombatAfterFailedKidnap()
		{
			state = VoidAwake_TrapperCombatState.Combat;
			ticksUntilStealthReturn = 0;

			if (ticksUntilCombatUnlock <= 0)
			{
				ticksUntilCombatUnlock = Props.combatMinDurationTicks;
			}

			RemoveStealthHediff();
		}

		/// <summary>Clears kidnap target before map exit. Keeps kidnapping hediff so the Trapper stays visible and slow until despawn.</summary>
		public void PrepareExitAfterKidnap()
		{
			kidnapTarget = null;
		}



		/// <summary>Legacy alias for <see cref="EnterKidnap"/>.</summary>

		public void BeginKidnapping(Pawn target) => EnterKidnap(target);



		/// <summary>Clear kidnapping hediff only; keep Kidnap mode for retry.</summary>

		public void EndKidnapping()
		{
			kidnapTarget = null;

			if (IsKidnap)
			{
				return;
			}

			RemoveKidnappingHediff();

			if (IsStealth)
			{
				EnsureStealthHediff();
			}
		}



		/// <summary>Enter combat and pull every other Trapper on the map into combat too.</summary>

		public void EnterCombat()

		{

			Map map = Pawn?.Map;

			if (map != null)

			{

				VoidAwake_TrapperUtility.RevealAllTrappersOnMap(map);

			}

			else

			{

				ApplyEnterCombat();

			}

		}



		/// <summary>Apply combat state to this pawn only (used by map-wide reveal).</summary>

		internal void ApplyEnterCombat()

		{

			kidnapTarget = null;

			RemoveKidnappingHediff();

			VoidAwake_TrapperUtility.ReleaseAllDoorsFor(Pawn);

			state = VoidAwake_TrapperCombatState.Combat;

			ticksUntilStealthReturn = 0;

			ticksUntilCombatUnlock = Props.combatMinDurationTicks;

			chainPlacing = false;

			chainDoorCell = IntVec3.Invalid;

			lastTrapCell = IntVec3.Invalid;

			wantsEscapeOutside = false;

			waitAnchorCell = IntVec3.Invalid;

			nextPassageSearchTick = 0;
			nextKidnapJobTick = 0;

			ResetFootprintTracking();

			RemoveStealthHediff();

		}

		/// <summary>Legacy name used by older call sites.</summary>

		public void Reveal() => EnterCombat();



		public void EnterStealth()

		{

			state = VoidAwake_TrapperCombatState.Stealth;

			ticksUntilStealthReturn = 0;

			ticksUntilCombatUnlock = 0;

			chainPlacing = false;

			chainDoorCell = IntVec3.Invalid;

			lastTrapCell = IntVec3.Invalid;

			wantsEscapeOutside = false;

			nextPassageSearchTick = 0;

			ResetFootprintTracking();

			EnsureStealthHediff();

		}



		public override void PostDestroy(DestroyMode mode, Map previousMap)

		{

			RemoveStealthHediff();

			RemoveKidnappingHediff();

			if (previousMap != null)

			{

				previousMap.GetComponent<VoidAwake_MapComponent_TrapperTraps>()?.ReleaseAllDoorsFor(Pawn);

				VoidAwake_RabbitPassageUtility.DestroyPassagesOwnedBy(previousMap, Pawn.thingIDNumber);

			}

			else

			{

				VoidAwake_TrapperUtility.ReleaseAllDoorsFor(Pawn);

			}



			base.PostDestroy(mode, previousMap);

		}



		public override void CompTick()

		{

			if (ticksUntilNextPlace > 0)

			{

				ticksUntilNextPlace--;

			}



			if (!Pawn.Spawned || Pawn.Dead)

			{

				ResetFootprintTracking();

				return;

			}



			if (IsKidnap

				&& !VoidAwake_TrapperKidnapUtility.HasKidnapTargets(Pawn)

				&& Pawn.jobs?.curJob == null

				&& Pawn.carryTracker?.CarriedThing == null)

			{

				ExitKidnap();

				return;

			}



			if (IsStealth)

			{

				EnsureStealthHediff();

				TrySpawnStealthFootprint();

				return;

			}



			if (IsKidnap)

			{

				EnsureKidnappingHediff();

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



		private void TrySpawnStealthFootprint()

		{

			IntVec3 cell = Pawn.Position;

			if (!lastFootprintCell.IsValid)

			{

				lastFootprintCell = cell;

				return;

			}



			if (cell == lastFootprintCell)

			{

				return;

			}



			lastFootprintCell = cell;

			cellsSinceFootprint++;



			int interval = Props.footprintIntervalCells;

			if (interval < 1)

			{

				interval = 1;

			}



			if (cellsSinceFootprint < interval)

			{

				return;

			}



			cellsSinceFootprint = 0;

			Map map = Pawn.Map;

			if (map == null)

			{

				return;

			}



			FleckCreationData data = FleckMaker.GetDataStatic(

				cell.ToVector3Shifted(),

				map,

				VoidAwake_TrapperDefOf.VoidAwake_TrapperFootstep,

				Props.footprintScale);

			data.rotation = Pawn.Rotation.AsAngle;

			map.flecks.CreateFleck(data);

		}



		private void ResetFootprintTracking()

		{

			lastFootprintCell = IntVec3.Invalid;

			cellsSinceFootprint = 0;

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



		private void EnsureKidnappingHediff()

		{

			if (!IsKidnap || Pawn.health?.hediffSet == null)

			{

				return;

			}



			if (Pawn.health.hediffSet.HasHediff(VoidAwake_TrapperDefOf.VoidAwake_TrapperKidnapping))

			{

				return;

			}



			ApplyKidnappingHediff(kidnapTarget);

		}



		private void ApplyKidnappingHediff(Pawn target)

		{

			if (Pawn.health?.hediffSet == null)

			{

				return;

			}



			if (Pawn.health.hediffSet.HasHediff(VoidAwake_TrapperDefOf.VoidAwake_TrapperKidnapping))

			{

				Hediff existing = Pawn.health.hediffSet.GetFirstHediffOfDef(VoidAwake_TrapperDefOf.VoidAwake_TrapperKidnapping);

				existing?.TryGetComp<VoidAwake_HediffComp_TrapperKidnapping>()?.SetTarget(target);

				return;

			}



			Hediff hediff = Pawn.health.AddHediff(VoidAwake_TrapperDefOf.VoidAwake_TrapperKidnapping);

			hediff?.TryGetComp<VoidAwake_HediffComp_TrapperKidnapping>()?.SetTarget(target);

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



		private void RemoveKidnappingHediff()

		{

			if (Pawn.health?.hediffSet == null)

			{

				return;

			}



			Hediff hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(VoidAwake_TrapperDefOf.VoidAwake_TrapperKidnapping);

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

			Scribe_Values.Look(ref state, "trapperState", VoidAwake_TrapperCombatState.Stealth);

			Scribe_Values.Look(ref wantsEscapeOutside, "wantsEscapeOutside", false);

			Scribe_Values.Look(ref waitAnchorCell, "waitAnchorCell", IntVec3.Invalid);

			Scribe_Values.Look(ref nextPassageSearchTick, "nextPassageSearchTick", 0);
			Scribe_Values.Look(ref nextKidnapJobTick, "nextKidnapJobTick", 0);

			Scribe_References.Look(ref kidnapTarget, "kidnapTarget");

			Scribe_Values.Look(ref stateBeforeKidnap, "stateBeforeKidnap", VoidAwake_TrapperCombatState.Stealth);



			bool legacyKidnapping = false;

			Scribe_Values.Look(ref legacyKidnapping, "isKidnapping", false);



			bool revealed = IsCombat;

			Scribe_Values.Look(ref revealed, "revealed", false);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)

			{

				if (revealed)

				{

					state = VoidAwake_TrapperCombatState.Combat;

				}

				else if (legacyKidnapping && state == VoidAwake_TrapperCombatState.Stealth)

				{

					state = VoidAwake_TrapperCombatState.Kidnap;

				}

			}

		}

	}

}

