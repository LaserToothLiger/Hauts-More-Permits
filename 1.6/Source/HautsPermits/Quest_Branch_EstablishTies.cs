using HautsFramework;
using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace HautsPermits
{
    //allows the description of Making Ties quests to show the description of the relevant e-branch
    public class QuestNode_GetFactionDesc : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Find.FactionManager.AllFactionsListForReading.Any((Faction f) => f == QuestGen.slate.Get<Faction>("faction", null, false));
        }
        protected override void RunInt()
        {
            QuestGen.slate.Set<string>(this.storeAs.GetValue(QuestGen.slate), QuestGen.slate.Get<Faction>("faction", null, false).def.description, false);
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
    }
    //accepting this quest toggles on the e-branch's tiesEstablished in its PeriodicBranchQuests faction comp, enabling it to periodically send its unique favor/standing-granting quests to you
    public class QuestNode_EstablishBranchTies : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_EstablishBranchTies qpebt = new QuestPart_EstablishBranchTies();
            qpebt.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            qpebt.faction = slate.Get<Faction>("faction", null, false);
            QuestGen.quest.AddPart(qpebt);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
    }
    public class QuestPart_EstablishBranchTies : QuestPart
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
                            pbq.tiesEstablished = true;
                            Slate slate = new Slate();
                            slate.Set<Faction>("faction", this.faction, false);
                            slate.Set<Thing>("asker", this.faction.leader, false);
                            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(HVMPDefOf.HVMP_BranchOutro, slate);
                        }
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
