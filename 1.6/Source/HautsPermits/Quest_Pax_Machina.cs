using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;

namespace HautsPermits
{
    /*Deal with a mech cluster. This node not only makes the mech cluster, but also handles two of the mutators
     * machina2: Rise and Shine makes the mech cluster generate awake, instead of dormant
     * machina3: The Harder They Fall multiplies the threat points of the mech cluster by THTF_multi*/
    public class QuestNode_RS_THTF : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_MechCluster questPart_MechCluster = new QuestPart_MechCluster();
            questPart_MechCluster.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            questPart_MechCluster.tag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(this.tag.GetValue(slate));
            questPart_MechCluster.mapParent = slate.Get<Map>("map", null, false).Parent;
            questPart_MechCluster.sketch = this.GenerateSketch(slate);
            questPart_MechCluster.dropSpot = this.dropSpot.GetValue(slate) ?? IntVec3.Invalid;
            QuestGen.quest.AddPart(questPart_MechCluster);
            string text = "";
            if (questPart_MechCluster.sketch.pawns != null)
            {
                text += PawnUtility.PawnKindsToLineList(questPart_MechCluster.sketch.pawns.Select((MechClusterSketch.Mech m) => m.kindDef), "  - ", ColoredText.ThreatColor);
            }
            string[] array = (from t in questPart_MechCluster.sketch.buildingsSketch.Things
                              where GenHostility.IsDefMechClusterThreat(t.def)
                              group t by t.def.label).Select(delegate (IGrouping<string, SketchThing> grp)
                              {
                                  int num = grp.Count<SketchThing>();
                                  return num.ToString() + " " + ((num > 1) ? Find.ActiveLanguageWorker.Pluralize(grp.Key, num) : grp.Key);
                              }).ToArray<string>();
            if (array.Any<string>())
            {
                if (text != "")
                {
                    text += "\n";
                }
                text += array.ToLineList(ColoredText.ThreatColor, "  - ");
            }
            if (text != "")
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("allThreats", text)
                });
            }
        }
        public MechClusterSketch GenerateSketch(Slate slate)
        {
            float points = this.points.GetValue(slate) ?? slate.Get<float>("points", 0f, false);
            bool mayhemMode = HVMP_Mod.settings.machinaX;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.machina3, mayhemMode))
            {
                points *= this.THTF_multi;
            }
            bool shouldBeAsleep = true;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.machina2, mayhemMode))
            {
                shouldBeAsleep = false;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_RS_info", this.RS_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_RS_info", this.nonRS_description.Formatted()) });
            }
            return MechClusterGenerator.GenerateClusterSketch(points, slate.Get<Map>("map", null, false), shouldBeAsleep, false);
        }
        protected override bool TestRunInt(Slate slate)
        {
            return Find.Storyteller.difficulty.allowViolentQuests && Faction.OfMechanoids != null && slate.Get<Map>("map", null, false) != null;
        }

        [NoTranslate]
        public SlateRef<string> inSignal;
        [NoTranslate]
        public SlateRef<string> tag;
        public SlateRef<float?> points;
        public SlateRef<IntVec3?> dropSpot;
        [MustTranslate]
        public string RS_description;
        [MustTranslate]
        public string nonRS_description;
        public float THTF_multi;
    }
    //this just handles the mutator machina1: One-two Combo runs its node with its storeAs slate ref set to the mechanoid faction
    public class QuestNode_MachinaOTC : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Faction.OfMechanoids != null;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.machina1, HVMP_Mod.settings.machinaX))
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_OTC_info", this.OTC_description.Formatted())
                });
                if (this.node != null)
                {
                    slate.Set<Faction>(this.storeAs.GetValue(slate), Faction.OfMechanoids, false);
                    this.node.Run();
                }
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_OTC_info", " ") });
            }
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public QuestNode node;
        [MustTranslate]
        public string OTC_description;
    }
}
