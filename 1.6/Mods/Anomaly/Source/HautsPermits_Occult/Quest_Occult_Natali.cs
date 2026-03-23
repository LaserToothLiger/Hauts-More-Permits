using HautsPermits;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace HautsPermits_Occult
{
    /*just sticking the one quest node in here because of how godawfully long and stupid the actual world object code is. ANOMAAAAAAAAAAAAAAAAAALY
     * obviously, as the only unique node, this not only generates the world object, it handles all three mutators:
     * natali1: Absence of Energy and Happiness assigns AOEAH_condition to the WorldObject_Hypercube field of the same name. This condition will just be there, in its map, permanently.
     * natali2: Chaos of Limbo flicks on COL_on for WorldObject_Hypercube
     * natali3: Don't Forget makes the WorldObject_Hypercube's DF_monsterKind a random pawn kind from DF_pawnKinds. Load this up with evil scary boss monsters*/
    public class QuestNode_GenerateHypercube : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            WorldObject worldObject = WorldObjectMaker.MakeWorldObject(this.def.GetValue(slate));
            worldObject.Tile = this.tile.GetValue(slate);
            if (worldObject is WorldObject_Hypercube wohc)
            {
                bool mayhemMode = HVMP_Mod.settings.nataliX;
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.natali1, mayhemMode))
                {
                    wohc.AOEAH_condition = this.AOEAH_condition;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_AOEAH_info", this.AOEAH_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_AOEAH_info", " ") });
                }
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.natali2, mayhemMode))
                {
                    wohc.COL_on = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_COL_info", this.COL_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_COL_info", " ") });
                }
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.natali3, mayhemMode))
                {
                    wohc.DF_monsterKind = this.DF_pawnKinds.RandomElement();
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("DF_monster", wohc.DF_monsterKind.label),
                        new Rule_String("mutator_DF_info", this.DF_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DF_info", " ") });
                }
            }
            if (this.faction.GetValue(slate) != null)
            {
                worldObject.SetFaction(this.faction.GetValue(slate));
            }
            if (this.storeAs.GetValue(slate) != null)
            {
                QuestGen.slate.Set<WorldObject>(this.storeAs.GetValue(slate), worldObject, false);
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        public SlateRef<WorldObjectDef> def;
        public SlateRef<PlanetTile> tile;
        public SlateRef<Faction> faction;
        [NoTranslate]
        public SlateRef<string> storeAs;
        public GameConditionDef AOEAH_condition;
        [MustTranslate]
        public string AOEAH_description;
        [MustTranslate]
        public string COL_description;
        [MustTranslate]
        public string DF_description;
        public List<PawnKindDef> DF_pawnKinds;
    }
}
