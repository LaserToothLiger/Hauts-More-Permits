using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;

namespace HautsPermits
{
    /*a pawn lend quest, but due to the following mutator, we need to write up a custom substitute for the normal node.
     * laelaps1: Eenie Meenie Meinee Mo forces one of the colonists you have to put on the shuttle to be a specific colonist*/
    public class QuestNode_GenerateShuttle_EMMM : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            if (!ModLister.CheckRoyaltyOrIdeology("Shuttle"))
            {
                return;
            }
            Slate slate = QuestGen.slate;
            Thing thing = ThingMaker.MakeThing(ThingDefOf.Shuttle, null);
            if (this.owningFaction.GetValue(slate) != null)
            {
                thing.SetFaction(this.owningFaction.GetValue(slate), null);
            }
            CompShuttle compShuttle = thing.TryGetComp<CompShuttle>();
            if (this.requiredPawns.GetValue(slate) != null)
            {
                compShuttle.requiredPawns.AddRange(this.requiredPawns.GetValue(slate));
            }
            if (this.requiredItems.GetValue(slate) != null)
            {
                compShuttle.requiredItems.AddRange(this.requiredItems.GetValue(slate));
            }
            compShuttle.acceptColonists = this.acceptColonists.GetValue(slate);
            compShuttle.acceptChildren = this.acceptChildren.GetValue(slate) ?? true;
            compShuttle.onlyAcceptColonists = this.onlyAcceptColonists.GetValue(slate);
            compShuttle.onlyAcceptHealthy = this.onlyAcceptHealthy.GetValue(slate);
            compShuttle.requiredColonistCount = this.requireColonistCount.GetValue(slate);
            compShuttle.permitShuttle = this.permitShuttle.GetValue(slate);
            compShuttle.minAge = this.minAge.GetValue(slate).GetValueOrDefault();
            bool EMMO_colonistFound = false;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.laelaps1, HVMP_Mod.settings.laelapsX))
            {
                Map map = slate.Get<Map>("map", null, false);
                if (map != null)
                {
                    Pawn colonist = map.mapPawns.FreeColonists.Where((Pawn p) => compShuttle.IsAllowed(p)).RandomElement();
                    if (colonist != null)
                    {
                        compShuttle.requiredPawns.Add(colonist);
                        EMMO_colonistFound = true;
                        slate.Set<Thing>("EMMO_target", colonist, false);
                        QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_EMMO_info", this.EMMO_description.Formatted())
                        });
                    }
                }
            }
            if (!EMMO_colonistFound)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_EMMO_info", " ") });
            }
            if (this.overrideMass.TryGetValue(slate, out float num) && num > 0f)
            {
                compShuttle.Transporter.massCapacityOverride = num;
            }
            QuestGen.slate.Set<Thing>(this.storeAs.GetValue(slate), thing, false);
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public SlateRef<IEnumerable<Pawn>> requiredPawns;
        public SlateRef<IEnumerable<ThingDefCount>> requiredItems;
        public SlateRef<int> requireColonistCount;
        public SlateRef<bool> acceptColonists;
        public SlateRef<bool?> acceptChildren;
        public SlateRef<bool> onlyAcceptColonists;
        public SlateRef<bool> onlyAcceptHealthy;
        public SlateRef<Faction> owningFaction;
        public SlateRef<bool> permitShuttle;
        public SlateRef<float> overrideMass;
        public SlateRef<float?> minAge;
        [MustTranslate]
        public string EMMO_description;
    }
    //runs the node if laelaps2: Since Sun Tzu Said So is on
    public class QuestNode_SSTSS : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.laelaps2, HVMP_Mod.settings.laelapsX))
            {
                if (this.node != null)
                {
                    this.node.Run();
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_SSTSS_info", this.SSTSS_description.Formatted())
                        });
                    return;
                }
            } else {
                if (this.elseNode != null)
                {
                    this.elseNode.Run();
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_SSTSS_info", " ") });
            }
        }
        public QuestNode node;
        public QuestNode elseNode;
        [MustTranslate]
        public string SSTSS_description;
    }
    /*multiplies value1 ($lendForDays, in this case) by TLWR_factor if laelaps3: The Long Way Round is on
     * (the one with the prettiest of views/it's got mountains it's got rivers it's got sihgts to give you shivers/but it sure would be prettier with you)*/
    public class QuestNode_Multiply_TLWR : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return !this.storeAs.GetValue(slate).NullOrEmpty();
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            slate.Set<int>(this.storeAs.GetValue(slate), (int)(this.value1.GetValue(slate) * (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.laelaps3, HVMP_Mod.settings.laelapsX) ? this.TLWR_factor : 1f)), false);
        }
        public SlateRef<int> value1;
        public float TLWR_factor;
        [NoTranslate]
        public SlateRef<string> storeAs;
    }
}
