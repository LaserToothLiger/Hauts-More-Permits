using HautsFramework;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace HautsPermits
{
    /*accepting this quest turns off the e-branch's tiesEstablished AND tieQuestOffered in its PeriodicBranchQuests comp. The former prevents it from periodically sending goodwill/standing accrual quests,
     * the latter lets it send a new Making Ties quest the next time it should send a quest, allowing you to reverse your decision at a later date.*/
    public class QuestNode_ForsakeBranchTies : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_ForsakeBranchTies qpfbt = new QuestPart_ForsakeBranchTies();
            qpfbt.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            qpfbt.faction = slate.Get<Faction>("faction", null, false);
            QuestGen.quest.AddPart(qpfbt);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
    }
    public class QuestPart_ForsakeBranchTies : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal && faction != null && !faction.defeated)
            {
                WorldComponent_HautsFactionComps wcfc = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                if (wcfc != null)
                {
                    Hauts_FactionCompHolder fch = wcfc.FindCompsFor(this.faction);
                    if (fch != null)
                    {
                        HautsFactionComp_PeriodicBranchQuests pbq = fch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                        if (pbq != null)
                        {
                            pbq.tiesEstablished = false;
                            pbq.tieQuestOffered = false;
                        }
                    }
                }
                Faction.OfPlayer.TryAffectGoodwillWith(this.faction, -100, true, true, HVMPDefOf.HVMP_CutTiesWithBranch, null);
                if (Faction.OfPlayer.RelationKindWith(this.faction) != FactionRelationKind.Hostile)
                {
                    Faction.OfPlayer.TryAffectGoodwillWith(this.faction, -100, true, true, HVMPDefOf.HVMP_CutTiesWithBranch, null);
                }
                foreach (Map m in Find.Maps)
                {
                    List<Thing> toDestroy = new List<Thing>();
                    List<Pawn> toFlee = new List<Pawn>();
                    foreach (Thing t in m.spawnedThings)
                    {
                        if (t.Faction != null && t.Faction == this.faction)
                        {
                            if (t is Pawn p)
                            {
                                p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, null, true);
                            }
                            else if (t.def.HasModExtension<HVMP_ItsOkToHarmThis>())
                            {
                                toDestroy.Add(t);
                            }
                        }
                    }
                    foreach (Thing t in toDestroy)
                    {
                        t.Destroy();
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
        }
        public string inSignal;
        public Faction faction;
    }
}
