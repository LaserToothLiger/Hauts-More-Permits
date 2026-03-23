using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace HautsPermits_Occult
{
    /*On accepting a Barker quest, it issues (and automatically accepts) one quest per generated Void item. Each one requires slightly different generation.
     * No fuckin' wonder people rarely make new anomalous entity mods, it's arduous just working with the ones that already exist
     * Unnatural Corpse has to target a pawn that an unnatural corpse could be made of, and link the corpse to it.
     * If LGTH is on, we tell the custom version of QuestPart_LinkUnnaturalCorpse to set its activation time to a random number of days within LGTH_daysToAwaken.*/
    public class QuestNode_Root_BarkerCorpse : QuestNode_Root_BarkerBox
    {
        protected override Thing GenerateThing(Pawn pawn)
        {
            return AnomalyUtility.MakeUnnaturalCorpse(pawn);
        }
        protected override bool ValidatePawn(Pawn pawn)
        {
            return base.ValidatePawn(pawn) && pawn.RaceProps.unnaturalCorpseDef != null && !Find.Anomaly.PawnHasUnnaturalCorpse(pawn);
        }
        protected override void AddPostDroppedQuestParts(Pawn pawn, Thing thing, Quest quest)
        {
            Slate slate = QuestGen.slate;
            quest.PawnDestroyed(pawn, null, delegate
            {
                QuestPart_LinkUnnaturalCorpse_LGTH questPart_LinkUnnaturalCorpse = new QuestPart_LinkUnnaturalCorpse_LGTH
                {
                    aboutPawn = pawn,
                    corpse = thing as UnnaturalCorpse,
                    inSignal = QuestGen.slate.Get<string>("inSignal", null, false),
                    LGTH_daysToAwaken = slate.Get<bool>("LGTH_on", false, false) ? this.LGTH_daysToAwaken.RandomInRange : -1
                };
                quest.AddPart(questPart_LinkUnnaturalCorpse);
            }, null, null, null, QuestPart.SignalListenMode.OngoingOnly);
        }
        public IntRange LGTH_daysToAwaken;
    }
    public class QuestPart_LinkUnnaturalCorpse_LGTH : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == this.inSignal && !this.aboutPawn.DestroyedOrNull())
            {
                Find.Anomaly.RegisterUnnaturalCorpse(this.aboutPawn, this.corpse);
                if (this.corpse.Tracker != null && this.LGTH_daysToAwaken > 0)
                {
                    this.corpse.Tracker.GetType().GetField("awakenTick", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this.corpse.Tracker, GenTicks.TicksGame + Mathf.CeilToInt(this.LGTH_daysToAwaken * 60000f));
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Pawn>(ref this.aboutPawn, "aboutPawn", false);
            Scribe_References.Look<UnnaturalCorpse>(ref this.corpse, "corpse", false);
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Values.Look<int>(ref this.LGTH_daysToAwaken, "LGTH_daysToAwaken", 7, false);
        }
        public Pawn aboutPawn;
        public UnnaturalCorpse corpse;
        public string inSignal;
        public int LGTH_daysToAwaken = -1;
    }
    //the cube needs to give its target pawn cube interest out of the gate. If LGTH is on, it also gets up to a random number within LGTH_bonusInterests of bonus worshippers in your colony.
    public class QuestNode_Root_BarkerCube : QuestNode_Root_BarkerBox
    {
        protected override Thing GenerateThing(Pawn pawn)
        {
            return ThingMaker.MakeThing(ThingDefOf.GoldenCube, null);
        }
        protected override void AddPostDroppedQuestParts(Pawn pawn, Thing thing, Quest quest)
        {
            quest.PawnDestroyed(pawn, null, delegate
            {
                quest.GiveHediff(pawn, HediffDefOf.CubeInterest, null);
            }, null, null, null, QuestPart.SignalListenMode.OngoingOnly);
            if (QuestGen.slate.Get<bool>("LGTH_on", false, false) && pawn.MapHeld != null)
            {
                int pawnsToAffect = Math.Min(pawn.MapHeld.mapPawns.SlavesAndPrisonersOfColonySpawnedCount + pawn.MapHeld.mapPawns.ColonistCount - 1, this.LGTH_bonusInterests.RandomInRange);
                for (int i = pawnsToAffect; i > 0; i--)
                {
                    Pawn pawn2 = null;
                    int tries = 100;
                    while (tries > 0)
                    {
                        if (QuestUtility.TryGetIdealColonist(out pawn2, pawn.MapHeld, new Func<Pawn, bool>(this.ValidatePawn)))
                        {
                            if (pawn2 != pawn)
                            {
                                quest.GiveHediff(pawn2, HediffDefOf.CubeInterest, null);
                                break;
                            }
                        }
                        tries--;
                    }
                }
            }
        }
        protected override bool ValidatePawn(Pawn pawn)
        {
            return base.ValidatePawn(pawn) && !pawn.health.hediffSet.HasHediff(HediffDefOf.CubeInterest, false) && !pawn.health.hediffSet.HasHediff(HediffDefOf.CubeComa, false);
        }
        public IntRange LGTH_bonusInterests;
    }
    //if LGTH is on, nociosphere gains a hediff that hastens its activation and makes it slightly harder to kill
    public class QuestNode_Root_BarkerSphere : QuestNode_Root_BarkerBox
    {
        protected override bool RequiresPawn { get; }
        protected override Thing GenerateThing(Pawn pawn)
        {
            Pawn thing = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Nociosphere, Faction.OfEntities, PawnGenerationContext.NonPlayer, -1, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
            if (QuestGen.slate.Get<bool>("LGTH_on", false, false))
            {
                thing.health.AddHediff(HVMPDefOf_A.HVMP_LGTH);
            }
            return thing;
        }
    }
    //if LGTH is on, nucleus gains the same hediff
    public class QuestNode_Root_BarkerNucleus : QuestNode_Root_BarkerBox
    {
        protected override Thing GenerateThing(Pawn pawn)
        {
            Pawn thing = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.FleshmassNucleus, Faction.OfEntities, PawnGenerationContext.NonPlayer, -1, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
            thing.health.AddHediff(this.hediff);
            if (QuestGen.slate.Get<bool>("LGTH_on", false, false))
            {
                thing.health.AddHediff(HVMPDefOf_A.HVMP_LGTH);
            }
            return thing;
        }
        public HediffDef hediff;
    }
    //unused, as the spine can just be crushed once you receive it? and you just? get a free shard??
    public class QuestNode_Root_BarkerSpine : QuestNode_Root_BarkerBox
    {
        protected override bool RequiresPawn { get; }
        protected override Thing GenerateThing(Pawn _)
        {
            return ThingMaker.MakeThing(ThingDefOf.RevenantSpine, null);
        }
    }
}
