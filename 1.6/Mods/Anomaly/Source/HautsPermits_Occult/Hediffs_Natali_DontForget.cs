using HautsFramework;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace HautsPermits_Occult
{
    /*several of the boss monsters that can spawn when finishing a Natali labyrinth with the Don't Forget mutator are variant pawn kinds of classic Anomaly entities with some bonus startingHediffs.
     * Some of those hediffs do unique stuff. These are their stories.
     * ScyllaMortar: it's a turret that can only fire while currently devouring something
     * while digesting, every 3s generate filthCount filthType at random spots w/in filthRadius, and accrue stacks of digestingHediff (this hsould be a Hediff_RapidRegenerationStackable, which gains bonusRegenPerProc hp per accrual)
     * Also has a visual effect with minor mechanical effect: every filthPeriodicity ticks, drop filthCount filthType in filthRadius*/
    public class HediffCompProperties_ScyllaMortar : HediffCompProperties_Turret
    {
        public HediffCompProperties_ScyllaMortar()
        {
            this.compClass = typeof(HediffComp_ScyllaMortar);
        }
        public HediffDef digestingHediff;
        public int bonusRegenPerProc;
        public IntRange filthCount;
        public int filthRadius;
        public ThingDef filthType;
    }
    public class HediffComp_ScyllaMortar : HediffComp_Turret
    {
        public new HediffCompProperties_ScyllaMortar Props
        {
            get
            {
                return (HediffCompProperties_ScyllaMortar)this.props;
            }
        }
        public CompDevourer DevourerComp
        {
            get
            {
                return this.Pawn.GetComp<CompDevourer>();
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(180) && this.DevourerComp != null && this.DevourerComp.Digesting)
            {
                int filthCount = this.Props.filthCount.RandomInRange;
                Map m = this.Pawn.Map;
                IntVec3 iv3 = this.Pawn.Position;
                while (filthCount > 0)
                {
                    IntVec3 loc = CellFinder.RandomClosewalkCellNear(iv3, m, this.Props.filthRadius, null);
                    FilthMaker.TryMakeFilth(loc, m, this.Props.filthType, 1, FilthSourceFlags.None, true);
                    filthCount--;
                }
                if (this.Props.digestingHediff != null)
                {
                    Hediff h = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.digestingHediff);
                    if (h == null)
                    {
                        h = HediffMaker.MakeHediff(this.Props.digestingHediff,this.Pawn);
                        this.Pawn.health.AddHediff(h);
                    }
                    if (h is Hediff_RapidRegenerationStackable hrr)
                    {
                        hrr.SetHpCapacity(this.Props.bonusRegenPerProc);
                    }
                }
            }
        }
        protected override bool CanShoot
        {
            get
            {
                return this.DevourerComp != null && this.DevourerComp.Digesting && base.CanShoot;
            }
        }
    }
    public class Hediff_RapidRegenerationStackable : Hediff
    {
        public override string SeverityLabel
        {
            get
            {
                return string.Format("{0:0} / {1:0}{2}", this.hpRemaining, this.hpCapacity, "HP".Translate());
            }
        }
        public override bool ShouldRemove
        {
            get
            {
                return this.hpRemaining <= 0f;
            }
        }
        public void SetHpCapacity(float amount)
        {
            this.hpCapacity += amount;
            this.hpRemaining += amount;
        }
        public override void Notify_Regenerated(float hp)
        {
            this.hpRemaining = Mathf.Max(this.hpRemaining - hp, 0f);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.hpRemaining, "hpRemaining", 0f, false);
            Scribe_Values.Look<float>(ref this.hpCapacity, "hpCapacity", 0f, false);
        }
        public float hpRemaining = 0f;
        public float hpCapacity = 0f;
    }
    //for the Grandsplice Chimera. Destroys the corpse on death, unleashing a viscera explosion and making fleshbeastCount pawns, each randomly drawn from the fleshbeastKinds list
    public class HediffCompProperties_FleshbeastsOnDeath : HediffCompProperties
    {
        public HediffCompProperties_FleshbeastsOnDeath()
        {
            this.compClass = typeof(HediffComp_FleshbeastsOnDeath);
        }
        public IntRange fleshbeastCount;
        public List<PawnKindDef> fleshbeastKinds;
    }
    public class HediffComp_FleshbeastsOnDeath : HediffComp
    {
        public HediffCompProperties_FleshbeastsOnDeath Props
        {
            get
            {
                return (HediffCompProperties_FleshbeastsOnDeath)this.props;
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (this.Pawn.SpawnedOrAnyParentSpawned)
            {
                Faction f = this.Pawn.Faction ?? Faction.OfEntities;
                Lord lord = this.Pawn.lord ?? LordMaker.MakeNewLord(f, new LordJob_AssaultColony(f, false, false, false, false, false, false, false), this.Pawn.MapHeld, null);
                int count = this.Props.fleshbeastCount.RandomInRange;
                for (int i = count; i > 0; i--)
                {
                    Pawn pawn2 = PawnGenerator.GeneratePawn(new PawnGenerationRequest(this.Props.fleshbeastKinds.RandomElement(), f, PawnGenerationContext.NonPlayer, null, true, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, 0f, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
                    this.SpawnPawn(pawn2, this.Pawn.PositionHeld, this.Pawn.MapHeld, lord);
                }
            }
            FleshbeastUtility.MeatSplatter(Rand.RangeInclusive(1, 3), this.Pawn.PositionHeld, this.Pawn.MapHeld, FleshbeastUtility.ExplosionSizeFor(this.Pawn));
            FilthMaker.TryMakeFilth(this.Pawn.PositionHeld, this.Pawn.MapHeld, ThingDefOf.Filth_TwistedFlesh, 1, FilthSourceFlags.None, true);
            if (this.Pawn.Corpse != null)
            {
                this.Pawn.Corpse.Destroy();
            }
        }
        private void SpawnPawn(Pawn child, IntVec3 position, Map map, Lord lord)
        {
            GenSpawn.Spawn(child, position, map, WipeMode.VanishOrMoveAside);
            if (lord != null)
            {
                lord.AddPawn(child);
            }
            FleshbeastUtility.SpawnPawnAsFlyer(child, map, position, 5, true);
        }
    }
    /*for the Noctol Umbranarch. Launches the darkness quest if the current map isn't in unnatural darkness and isn't a Natali labyrinth.
     * Also, on taking damage, if it's been at least noctolCooldown ticks since the last proc, spawn noctolCount pawns of noctolDef kind within 10c. They're of the same faction and they go attack you.
     * Also also constantly maintains effecter on the pawn.*/
    public class HediffCompProperties_Umbranarch : HediffCompProperties
    {
        public HediffCompProperties_Umbranarch()
        {
            this.compClass = typeof(HediffComp_Umbranarch);
        }
        public IntRange noctolCount;
        public int noctolCooldown;
        public PawnKindDef noctolDef;
        public EffecterDef effecter;
        public QuestScriptDef darkness;
    }
    public class HediffComp_Umbranarch : HediffComp
    {
        public HediffCompProperties_Umbranarch Props
        {
            get
            {
                return (HediffCompProperties_Umbranarch)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.effecter == null)
            {
                this.effecter = this.Props.effecter.Spawn();
            }
            this.effecter.EffectTick(this.Pawn, this.Pawn);
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.spawnCooldown > 0)
            {
                this.spawnCooldown -= delta;
            }
            if (this.Pawn.Spawned && this.Pawn.IsHashIntervalTick(360))
            {
                if (this.Pawn.Map.GetComponent<HVMP_HypercubeMapComponent>() == null)
                {
                    GameCondition gcud = this.Pawn.Map.gameConditionManager.GetActiveCondition(GameConditionDefOf.UnnaturalDarkness);
                    if (gcud == null)
                    {
                        Slate slate = new Slate();
                        slate.Set<TaggedString>("discoveryMethod", "QuestDiscoveredFromDebug".Translate(), false);
                        slate.Set<float>("points", StorytellerUtility.DefaultThreatPointsNow(this.Pawn.Map), false);
                        if (this.Props.darkness.CanRun(slate, this.Pawn.Map))
                        {
                            QuestUtility.GenerateQuestAndMakeAvailable(this.Props.darkness, slate);
                            EffecterDefOf.MonolithLevelChanged.Spawn().Trigger(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), -1);
                        }
                    }
                }
            }
        }
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (this.spawnCooldown <= 0 && this.Pawn.Spawned)
            {
                this.spawnCooldown = this.Props.noctolCooldown;
                int toSpawn = this.Props.noctolCount.RandomInRange;
                Faction f = this.Pawn.Faction ?? Faction.OfEntities;
                Lord lord = this.Pawn.lord ?? LordMaker.MakeNewLord(f, new LordJob_AssaultColony(f, false, false, false, false, false, false, false), this.Pawn.Map, null);
                while (toSpawn > 0)
                {
                    toSpawn--;
                    Pawn noctol = PawnGenerator.GeneratePawn(this.Props.noctolDef, f, null);
                    GenSpawn.Spawn(noctol, CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, 10), this.Pawn.Map, WipeMode.Vanish);
                    if (!noctol.Downed)
                    {
                        Lord lord2 = noctol.lord;
                        if (lord2 != null)
                        {
                            lord2.RemovePawn(noctol);
                        }
                        lord.AddPawn(noctol);
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.spawnCooldown, "spawnCooldown", 0, false);
        }
        public int spawnCooldown;
        private Effecter effecter;
    }
}
