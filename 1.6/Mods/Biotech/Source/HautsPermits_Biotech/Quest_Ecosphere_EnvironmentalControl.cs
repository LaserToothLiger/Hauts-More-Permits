using HautsPermits;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Grammar;

namespace HautsPermits_Biotech
{
    /*Handles two mutators:
     * ec1: Do Not Leave Unattended sets up a slate ref which gets used by QuestNode_MultiProblemCauserGenerator to turn the Mutator_DNLU comp of the problem causer site(s) on
     * ec2: Gee Bill runs node GB_LoopCount times (instead of nonGB_LoopCount times), and the node contains QuestNode_MultiProblemCauserGenerator, so this is basically how many problem causers you want to generate*/
    public class QuestNode_DNLU_SG : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            for (int i = 0; i < this.GB_LoopCount; i++)
            {
                if (this.storeLoopCounterAs.GetValue(slate) != null)
                {
                    slate.Set<int>(this.storeLoopCounterAs.GetValue(slate), i, false);
                }
                try
                {
                    if (!this.node.TestRun(slate))
                    {
                        return false;
                    }
                } finally {
                    slate.PopPrefix();
                }
            }
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            bool mayhemMode = HVMP_Mod.settings.ecX;
            bool DNLU_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.ec1, mayhemMode);
            bool GB_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.ec2, mayhemMode);
            int counter = GB_on ? this.GB_LoopCount : this.nonGB_LoopCount;
            if (DNLU_on)
            {
                QuestGen.slate.Set<bool>(this.DNLU_saveAs.GetValue(slate), true, false);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_DNLU_info", this.DNLU_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DNLU_info", " ") });
            }
            if (GB_on)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_GB_info", this.GB_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_GB_info", " ") });
            }
            for (int i = 0; i < counter; i++)
            {
                if (this.storeLoopCounterAs.GetValue(slate) != null)
                {
                    QuestGen.slate.Set<int>(this.storeLoopCounterAs.GetValue(slate), i, false);
                }
                try
                {
                    this.node.Run();
                } finally {
                    QuestGen.slate.PopPrefix();
                }
            }
        }
        public QuestNode node;
        public int nonGB_LoopCount;
        public int GB_LoopCount;
        [NoTranslate]
        public SlateRef<string> storeLoopCounterAs;
        [MustTranslate]
        public string GB_description;
        [NoTranslate]
        public SlateRef<string> DNLU_saveAs;
        [MustTranslate]
        public string DNLU_description;
    }
    //every time you run this, it creates one problem causer site and links it to this quest's victory condition. You gotta bust them all up to win.
    public class QuestNode_MultiProblemCauserGenerator : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            if (!this.TryFindTile(slate, out PlanetTile planetTile))
            {
                return false;
            }
            bool? value = this.clampRangeBySiteParts.GetValue(slate);
            if (((value.GetValueOrDefault()) & (value != null)) && this.sitePartDefs.GetValue(slate) == null)
            {
                return false;
            }
            this.SetVars(QuestGen.slate, planetTile, out List<SitePartDefWithParams> spdwp);
            if (!Find.Storyteller.difficulty.allowViolentQuests && !spdwp.NullOrEmpty())
            {
                using (IEnumerator<SitePartDefWithParams> enumerator = spdwp.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.def.wantsThreatPoints)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            PlanetTile planetTile;
            if (this.TryFindTile(QuestGen.slate, out planetTile))
            {
                this.SetVars(slate, planetTile, out List<SitePartDefWithParams> spdwp);
                Site site = QuestGen_Sites.GenerateSite(spdwp, planetTile, this.faction.GetValue(slate), this.hiddenSitePartsPossible.GetValue(slate), this.SingleSitePartRules, this.worldObjectDef.GetValue(slate));
                site.SetFaction(this.faction.GetValue(slate));
                quest.AddPart(new QuestPart_LookOverHere(site));
                IEnumerable<WorldObject> wosIEnum = this.worldObjects.GetValue(slate);
                List<WorldObject> wos = wosIEnum != null ? wosIEnum.ToList() : new List<WorldObject>();
                if (wos != null)
                {
                    wos.Add(site);
                } else {
                    wos = new List<WorldObject> { site };
                }
                slate.Set<List<WorldObject>>(this.storeAs.GetValue(slate), wos, false);
                Thing conditionCauser = slate.Get<Thing>("conditionCauser", null, false);
                if (conditionCauser != null)
                {
                    string text = QuestGen.GenerateNewSignal("AllConditionCausersDestroyed", false);
                    string text2 = QuestGen.GenerateNewSignal("ConditionCauserHacked", false);
                    IEnumerable<string> dsIEnum = this.destroyedStrings.GetValue(slate);
                    List<string> ds = dsIEnum != null ? dsIEnum.ToList() : new List<String>();
                    int i = this.iterator.GetValue(slate);
                    string text3 = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("terminal" + i.ToString());
                    QuestUtility.AddQuestTag(conditionCauser, text3);
                    string text4 = QuestGenUtility.HardcodedSignalWithQuestID(text3 + ".Destroyed");
                    ds.Add(text4);
                    slate.Set<int>(this.storeIteratorAs.GetValue(slate), i + 1);
                    slate.Set<List<string>>(this.storeStringsAs.GetValue(slate), ds, false);
                    QuestPart_PassAllActivable questPart_PassAllActivable;
                    if (!quest.TryGetFirstPartOfType<QuestPart_PassAllActivable>(out questPart_PassAllActivable))
                    {
                        questPart_PassAllActivable = quest.AddPart<QuestPart_PassAllActivable>();
                        questPart_PassAllActivable.inSignalEnable = QuestGen.slate.Get<string>("inSignal", null, false);
                    }
                    questPart_PassAllActivable.inSignals = ds;
                    questPart_PassAllActivable.outSignalsCompleted.Add(text);
                    questPart_PassAllActivable.outSignalAny = text2;
                }
                if (this.DNLU_flag.GetValue(slate))
                {
                    Mutator_DNLU component = site.GetComponent<Mutator_DNLU>();
                    if (component != null)
                    {
                        component.DNLU_on = true;
                    }
                }
            }
        }
        private RulePack SingleSitePartRules
        {
            get
            {
                Slate slate = QuestGen.slate;
                QuestScriptDef qsd = DefDatabase<QuestScriptDef>.GetNamed("Util_GenerateSite");
                if (qsd != null && qsd.root is QuestNode_GenerateSite qngs)
                {
                    return qngs.singleSitePartRules.GetValue(slate);
                }
                return this.singleSitePartRules.GetValue(slate);
            }
        }
        private bool TryFindTile(Slate slate, out PlanetTile tile)
        {
            bool value = this.canSelectSpace.GetValue(slate);
            Map map = slate.Get<Map>("map", null, false) ?? (value ? Find.RandomPlayerHomeMap : Find.RandomSurfacePlayerHomeMap);
            PlanetTile planetTile = ((map != null) ? map.Tile : PlanetTile.Invalid);
            if (planetTile.Valid && planetTile.LayerDef.isSpace && !value)
            {
                planetTile = PlanetTile.Invalid;
            }
            int num = int.MaxValue;
            bool? value2 = this.clampRangeBySiteParts.GetValue(slate);
            if (value2 != null && value2.Value)
            {
                foreach (SitePartDef sitePartDef in this.sitePartDefs.GetValue(slate))
                {
                    if (sitePartDef.conditionCauserDef != null)
                    {
                        num = Mathf.Min(num, sitePartDef.conditionCauserDef.GetCompProperties<CompProperties_CausesGameCondition>().worldRange);
                    }
                }
            }
            TileFinderMode tileFinderMode = (this.preferCloserTiles.GetValue(slate) ? TileFinderMode.Near : TileFinderMode.Random);
            float num2 = ((!ModsConfig.OdysseyActive) ? 0f : (this.selectLandmarkChance.GetValue(slate) ?? 0.5f));
            return TileFinder.TryFindNewSiteTile(out tile, planetTile, Mathf.Min(siteDistRange.min, num), Mathf.Min(siteDistRange.max, num), this.allowCaravans.GetValue(slate), this.allowedLandmarks.GetValue(slate), num2, this.canSelectComboLandmarks.GetValue(slate), tileFinderMode, false, value, null, null);
        }
        private void SetVars(Slate slate, PlanetTile planetTile, out List<SitePartDefWithParams> spdwp)
        {
            List<SitePartDefWithParams> list;
            SiteMakerHelper.GenerateDefaultParams(slate.Get<float>("points", 0f, false), planetTile, this.faction.GetValue(slate), this.sitePartDefs.GetValue(slate), out list);
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].def == SitePartDefOf.PreciousLump)
                    {
                        list[i].parms.preciousLumpResources = slate.Get<ThingDef>("targetMineable", null, false);
                    }
                }
            }
            slate.Set<List<SitePartDefWithParams>>(this.storeSitePartsParamsAs.GetValue(slate), list, false);
            spdwp = list;
        }
        public SlateRef<bool> preferCloserTiles;
        public SlateRef<bool> allowCaravans;
        public SlateRef<bool> canSelectSpace;
        public SlateRef<bool?> clampRangeBySiteParts;
        public SlateRef<IEnumerable<SitePartDef>> sitePartDefs;
        public SlateRef<List<LandmarkDef>> allowedLandmarks;
        public SlateRef<float?> selectLandmarkChance;
        public SlateRef<bool> canSelectComboLandmarks;
        public SlateRef<Faction> faction;
        [NoTranslate]
        public SlateRef<string> storeSitePartsParamsAs;
        public SlateRef<bool> hiddenSitePartsPossible;
        public SlateRef<RulePack> singleSitePartRules;
        public SlateRef<WorldObjectDef> worldObjectDef;
        public SlateRef<IEnumerable<WorldObject>> worldObjects;
        [NoTranslate]
        public SlateRef<string> storeAs;
        public SlateRef<IEnumerable<string>> destroyedStrings;
        [NoTranslate]
        public SlateRef<string> storeStringsAs;
        public SlateRef<int> iterator;
        [NoTranslate]
        public SlateRef<string> storeIteratorAs;
        public IntRange siteDistRange;
        public SlateRef<bool> DNLU_flag;
    }
    /*Patched in to be a comp of sites. If flipped on by QuestNode_MultiProblemCauserGenerator (which does so at the behest of QuestNode_DNLU_SG via the XML),
     * then a bonus group of combat or settlement pawns of the faction's site (mechanoids default) spawn around the problem causer and defend around it*/
    public class WorldObjectCompProperties_Mutator_DNLU : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_Mutator_DNLU()
        {
            this.compClass = typeof(Mutator_DNLU);
        }
        public float DNLU_pointFactor;
    }
    public class Mutator_DNLU : WorldObjectComp
    {
        public override void Initialize(WorldObjectCompProperties props)
        {
            base.Initialize(props);
            WorldObjectCompProperties_Mutator_DNLU proper = props as WorldObjectCompProperties_Mutator_DNLU;
            if (proper != null)
            {
                this.DNLU_pointFactor = proper.DNLU_pointFactor;
            }
        }
        public override void PostMapGenerate()
        {
            if (this.DNLU_on && this.parent is Site site)
            {
                Thing problemCauser = null;
                foreach (SitePart sp in site.parts)
                {
                    if (sp.conditionCauser != null && sp.conditionCauser.SpawnedOrAnyParentSpawned)
                    {
                        problemCauser = sp.conditionCauser;
                        break;
                    }
                }
                if (problemCauser != null)
                {
                    Faction faction = site.Faction ?? Faction.OfMechanoids;
                    PawnGroupMaker pgm = faction.def.pawnGroupMakers.Where((PawnGroupMaker pgm0) => pgm0.kindDef == PawnGroupKindDefOf.Settlement || pgm0.kindDef == PawnGroupKindDefOf.Combat).RandomElement();
                    if (pgm != null)
                    {
                        PawnGroupMakerParms pawnGroupMakerParms = new PawnGroupMakerParms
                        {
                            groupKind = pgm.kindDef,
                            tile = site.Tile,
                            faction = faction,
                            inhabitants = true,
                            generateFightersOnly = true,
                            seed = new int?(Rand.Int),
                            points = Math.Max(StorytellerUtility.DefaultThreatPointsNow(site.Map) * this.DNLU_pointFactor, faction.def.MinPointsToGeneratePawnGroup(pgm.kindDef, null))
                        };
                        IntVec3 iv3 = problemCauser.PositionHeld;
                        Map map = problemCauser.MapHeld;
                        List<Pawn> pawns = new List<Pawn>();
                        foreach (Pawn pawn in PawnGroupMakerUtility.GeneratePawns(pawnGroupMakerParms, true))
                        {
                            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(iv3, map, 10, null), map);
                            pawns.Add(pawn);
                        }
                        if (pawns.Count > 0)
                        {
                            LordMaker.MakeNewLord(faction, new LordJob_DefendPoint(iv3), map, pawns);
                        }
                    }
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.DNLU_on, "DNLU_on", false, false);
            Scribe_Values.Look<float>(ref this.DNLU_pointFactor, "DNLU_pointFactor", 0.5f, false);
        }
        public bool DNLU_on;
        public float DNLU_pointFactor;
    }
    /*a QuestNode_Delay derivative which handles the mutator ec3: Land and Sky assigns a faction to storeMechFactionAs,
     *   which is referenced by the contents of its node in the XML to instigate a raid of that faction unless you finish the quest first*/
    public class QuestNode_LAS : QuestNode_Delay
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.ec3, HVMP_Mod.settings.ecX))
            {
                base.RunInt();
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_LAS_info", this.LAS_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_LAS_info", " ") });
            }
            slate.Set<Faction>(this.storeMechFactionAs.GetValue(slate), (Faction.OfMechanoids != null && !Faction.OfMechanoids.deactivated) ? Faction.OfMechanoids : BranchQuestSetupUtility.GetAnEnemyFaction(), false);
        }
        [MustTranslate]
        public string LAS_description;
        [NoTranslate]
        public SlateRef<string> storeMechFactionAs;
    }
}
