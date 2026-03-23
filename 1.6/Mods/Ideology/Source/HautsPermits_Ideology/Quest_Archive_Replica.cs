using HautsFramework;
using HautsPermits;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace HautsPermits_Ideology
{
    /*This predates the creation of the QuestNode_[insertFactionHere]Intermediary nodes, hence why it does their work and is a root
     * Build a monument. Some of the usual mechanisms of such a quest are altered by the mutators.
     * This was written before the invention of QuestNode_ArchiveIntermediary (or, really, RoverIntermediary since this was originally a Rover Branch quest), hence why it does its job.
     * replica3: The Test of Time: guarantees that punishmentOnDestroy is true, and passes a reference in the form of storeTTOT. (In the XML, QuestNode_Replica_TTOT is configured to pick up this reference)*/
    public class QuestNode_Root_Replica : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (BranchQuestSetupUtility.TryFindArchiveFaction(out Faction faction))
            {
                Slate slate = QuestGen.slate;
                Quest quest = QuestGen.quest;
                slate.Set<Faction>("faction", faction, false);
                slate.Set<Pawn>("asker", faction.leader, false);
                bool TTOT_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.replica3, HVMP_Mod.settings.replicaX);
                slate.Set<bool>("punishmentOnDestroy", TTOT_on || Rand.Chance(0.5f), false);
                if (TTOT_on)
                {
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_TTOT_info", this.TTOT_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TTOT_info", " ") });
                }
                slate.Set<bool>(this.storeTTOT.GetValue(slate), TTOT_on, false);
                Map map = QuestSetupUtility.Quest_TryGetMap();
                slate.Set<Map>("map", map, false);
                PlanetTile tile = QuestSetupUtility.Quest_TryGetPlanetTile();
                slate.Set<PlanetTile>("pTile", tile, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = faction;
                QuestGen.quest.AddPart(qpbgfh);
				BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
			BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            return BranchQuestSetupUtility.TryFindCommerceFaction(out Faction commerceFaction) && base.TestRunInt(slate);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        [NoTranslate]
        public SlateRef<string> storeTTOT;
        [MustTranslate]
        public string TTOT_description;
    }
    /*sets the time you must guard the monument for (valueTimeUnits * a random value within valueTime), and the goodwill you lose if any part of the monument is destroyed (valueGoodwill). if TTOT_enabled is true...
     * timer gets multiplied by TTOT_enabled
     * goodwill loss gets multiplied by goodwillFactorTTOT*/
    public class QuestNode_Replica_TTOT : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            this.SetVars(slate);
            return true;
        }
        protected override void RunInt()
        {
            this.SetVars(QuestGen.slate);
        }
        private void SetVars(Slate slate)
        {
            slate.Set<int>(this.nameTime.GetValue(slate), (int)(this.valueTime.RandomInRange * this.valueTimeUnits * (this.TTOT_enabled.GetValue(slate) ? this.timeFactorTTOT : 1f)), false);
            slate.Set<int>(this.nameGoodwill.GetValue(slate), (int)(this.valueGoodwill * (this.TTOT_enabled.GetValue(slate) ? this.goodwillFactorTTOT : 1f)), false);
        }
        [NoTranslate]
        public SlateRef<string> nameTime;
        public IntRange valueTime;
        public int valueTimeUnits;
        public float timeFactorTTOT;
        [NoTranslate]
        public SlateRef<string> nameGoodwill;
        public int valueGoodwill;
        public float goodwillFactorTTOT;
        public SlateRef<bool> TTOT_enabled;
    }
    //handles the effect of the mutator replica2: Operation CWAL is used to multiply the amount of time you have to finish the monument by OCWAL_factor
    public class QuestNode_Multiply_OCWAL : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return !this.storeAs.GetValue(slate).NullOrEmpty();
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            slate.Set<double>(this.storeAs.GetValue(slate), this.value1.GetValue(slate) * this.value2.GetValue(slate) * (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.replica2, HVMP_Mod.settings.replicaX) ? this.OCWAL_factor : 1d), false);
        }
        public SlateRef<double> value1;
        public SlateRef<double> value2;
        public double OCWAL_factor;
        [NoTranslate]
        public SlateRef<string> storeAs;
    }
    //handles the effect of replica1: Magnify... Enhance alters the size of the monument, multiplying its largest dimension by HC_factor, or adding HC_minBonus to it, whichever would be larger. It treats any original value > max as max.
    public class QuestNode_Replica_HC : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            int largestSize = this.GetLargestSize(slate);
            slate.Set<int>(this.storeAs.GetValue(slate), largestSize, false);
            return largestSize >= this.failIfSmaller.GetValue(slate);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            int largestSize = this.GetLargestSize(slate);
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.replica1, HVMP_Mod.settings.replicaX))
            {
                largestSize = (int)Math.Max(largestSize * this.HC_factor, largestSize + this.HC_minBonus);
                slate.Set<float>(this.HC_scalarSaveAs.GetValue(slate), this.HC_scalarIfOn, false);
            } else {
                slate.Set<float>(this.HC_scalarSaveAs.GetValue(slate), this.HC_scalarIfOff, false);
            }
            slate.Set<int>(this.storeAs.GetValue(slate), largestSize, false);
        }
        private int GetLargestSize(Slate slate)
        {
            Map mapResolved = this.map.GetValue(slate) ?? slate.Get<Map>("map", null, false);
            if (mapResolved == null)
            {
                return 0;
            }
            int value = this.max.GetValue(slate);
            CellRect cellRect = LargestAreaFinder.FindLargestRect(mapResolved, (IntVec3 x) => this.IsClear(x, mapResolved), value);
            return Mathf.Min(new int[] { cellRect.Width, cellRect.Height, value });
        }
        private bool IsClear(IntVec3 c, Map map)
        {
            if (!c.GetAffordances(map).Contains(TerrainAffordanceDefOf.Heavy))
            {
                return false;
            }
            List<Thing> thingList = c.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i].def.IsBuildingArtificial && thingList[i].Faction == Faction.OfPlayer)
                {
                    return false;
                }
                if (thingList[i].def.mineable)
                {
                    bool flag = false;
                    for (int j = 0; j < 8; j++)
                    {
                        IntVec3 intVec = c + GenAdj.AdjacentCells[j];
                        if (intVec.InBounds(map) && intVec.GetFirstMineable(map) == null)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public SlateRef<Map> map;
        [NoTranslate]
        public SlateRef<string> storeAs;
        public SlateRef<int> failIfSmaller;
        public SlateRef<int> max;
        public int HC_minBonus;
        public float HC_factor = 1f;
        [NoTranslate]
        public SlateRef<string> HC_scalarSaveAs;
        public float HC_scalarIfOff;
        public float HC_scalarIfOn;
    }
}
