using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace HautsPermits
{
    /*study book (not really a book, both so that pawns don't stuff it in a bookcase and so that it isn't a valid option for Item Stashes to generate) to win. Two mutators' effects are performed by the book itself:
     * Drier Than Salt: if DTS_on is enabled by the quest node, then saps DTS_boredomPerHour joy from the studier's joy need, and every DTS_thoughtGiverCooldown ticks inflicts DTS_thoughtToGive on the studier
     * Infodense: amount of progress that must be accrued to complete is multiplied by ID_multi*/
    public class CompProperties_StudiableAtlas : CompProperties_StudiableQuestItem
    {
        public CompProperties_StudiableAtlas()
        {
            this.compClass = typeof(CompStudiableAtlas);
        }
        public float DTS_boredomPerHour;
        public ThoughtDef DTS_thoughtToGive;
        public int DTS_thoughtGiverCooldown;
    }
    public class CompStudiableAtlas : CompStudiableQuestItem
    {
        public new CompProperties_StudiableAtlas Props
        {
            get
            {
                return (CompProperties_StudiableAtlas)this.props;
            }
        }
        public override void ExtraStudyEffects(int delta, Pawn researcher, Thing brb, Thing researchBench)
        {
            if (this.DTS_on)
            {
                if (this.Props.DTS_thoughtToGive != null && researcher.needs.mood != null && researcher.IsHashIntervalTick(this.Props.DTS_thoughtGiverCooldown, delta))
                {
                    researcher.needs.mood.thoughts.memories.TryGainMemory(this.Props.DTS_thoughtToGive);
                }
                Need_Joy nj = researcher.needs.joy;
                if (nj != null)
                {
                    nj.CurLevel = nj.CurLevel - (this.Props.DTS_boredomPerHour * delta / 2500);
                }
            }
        }
        public override float RequiredProgress => base.RequiredProgress * this.ID_multi;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.DTS_on, "DTS_on", false, false);
            Scribe_Values.Look<float>(ref this.ID_multi, "ID_multi", 1f, false);
        }
        public bool DTS_on;
        public float ID_multi = 1f;
    }
    /*when a pawn visits this site, add that pawn to the roster of pawns who can study the book. This world object also performs one of the mutator's effects
     * Tresspassing Punishable By Ultratech: if TPBU_on is true, any time you bring someone here, a problem causer incident occurs. 80% chance for this effect to be disabled after each visit.*/
    public class WorldObject_AtlasPoint : WorldObject
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption o in base.GetFloatMenuOptions(caravan))
            {
                yield return o;
            }
            foreach (FloatMenuOption f in CaravanArrivalAction_VisitAtlasPoint.GetFloatMenuOptions(caravan, this))
            {
                yield return f;
            }
            yield break;
        }
        public void Notify_CaravanArrived(Caravan caravan)
        {
            CompStudiableQuestItem cda = this.book.TryGetComp<CompStudiableQuestItem>();
            if (cda != null)
            {
                foreach (Pawn p in caravan.pawns)
                {
                    if (!cda.pawns.Contains(p))
                    {
                        cda.pawns.Add(p);
                    }
                }
            }
            if (this.TPBU_on)
            {
                Slate slate = new Slate();
                Map map = Find.AnyPlayerHomeMap;
                slate.Set<float>("points", StorytellerUtility.DefaultThreatPointsNow(map), false);
                slate.Set<Map>("map", map, false);
                Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(DefDatabase<QuestScriptDef>.GetNamed("ProblemCauser"), slate);
                if (!quest.hidden && quest.root.sendAvailableLetter)
                {
                    QuestUtility.SendLetterQuestAvailable(quest);
                }
                if (Rand.Chance(0.8f))
                {
                    this.TPBU_on = false;
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Thing>(ref this.book, "book", false);
            Scribe_Values.Look<bool>(ref this.TPBU_on, "TPBU_on", false, false);
        }
        public Thing book;
        public bool TPBU_on;
    }
    //bespoke world object, bespoke CAA
    public class CaravanArrivalAction_VisitAtlasPoint : CaravanArrivalAction
    {
        public override string Label
        {
            get
            {
                return "VisitPeaceTalks".Translate(this.atlasPoint.Label);
            }
        }
        public override string ReportString
        {
            get
            {
                return "CaravanVisiting".Translate(this.atlasPoint.Label);
            }
        }
        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, WorldObject_AtlasPoint atlasPoint)
        {
            return atlasPoint != null && atlasPoint.Spawned;
        }
        public CaravanArrivalAction_VisitAtlasPoint()
        {
        }
        public CaravanArrivalAction_VisitAtlasPoint(WorldObject_AtlasPoint atlasPoint)
        {
            this.atlasPoint = atlasPoint;
        }
        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
            if (floatMenuAcceptanceReport)
            {
                if (this.atlasPoint != null && this.atlasPoint.Tile != destinationTile)
                {
                    floatMenuAcceptanceReport = false;
                }
                else
                {
                    floatMenuAcceptanceReport = CaravanArrivalAction_VisitAtlasPoint.CanVisit(caravan, this.atlasPoint);
                }
            }
            return floatMenuAcceptanceReport;
        }
        public override void Arrived(Caravan caravan)
        {
            this.atlasPoint.Notify_CaravanArrived(caravan);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<WorldObject_AtlasPoint>(ref this.atlasPoint, "atlasPoint", false);
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, WorldObject_AtlasPoint atlasPoint)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_VisitAtlasPoint>(() => CaravanArrivalAction_VisitAtlasPoint.CanVisit(caravan, atlasPoint), () => new CaravanArrivalAction_VisitAtlasPoint(atlasPoint), "VisitPeaceTalks".Translate(atlasPoint.Label), caravan, atlasPoint.Tile, atlasPoint, null);
        }
        private WorldObject_AtlasPoint atlasPoint;
    }
    /*this was created before QuestNode_RoverIntermediary, hence why it does some of that node's work and is the root of its quest.
     * Create a WorldObject_AtlasPoint and pair it with a DatedAtlas so that anyone visiting the site becomes able to study the atlas.
     * Also assign the extra stats and skills of the book's CompStudiableQuestItem.
     * Also also flicks on the toggles for atlas1 (Drier Than Salt) and atlas3 (Trespassing Punishable By Ultratech), and assigns ID_multi if atlas2 (Infodense) is enabled.*/
    public class QuestNode_Root_Atlas : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            PlanetTile tile = QuestSetupUtility.Quest_TryGetPlanetTile();
            if (this.TryFindSiteTile(tile, out PlanetTile num) && BranchQuestSetupUtility.TryFindRoverFaction(out Faction roverFaction))
            {
                Slate slate = QuestGen.slate;
                Quest quest = QuestGen.quest;
                Map map = QuestSetupUtility.Quest_TryGetMap();
                bool mayhemMode = HVMP_Mod.settings.atlasX;
                this.TryFindSiteTile(tile, out PlanetTile num2);
                string text = QuestGenUtility.HardcodedSignalWithQuestID("worldObject.Destroyed");
                WorldObject_AtlasPoint worldObject_AtlasPoint = (WorldObject_AtlasPoint)WorldObjectMaker.MakeWorldObject(HVMPDefOf.HVMP_AtlasPoint);
                worldObject_AtlasPoint.Tile = num;
                worldObject_AtlasPoint.SetFaction(roverFaction);
                worldObject_AtlasPoint.book = ThingMaker.MakeThing(HVMPDefOf.HVMP_DatedAtlas);
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.atlas3, mayhemMode))
                {
                    worldObject_AtlasPoint.TPBU_on = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_TPBU_info", this.TPBU_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TPBU_info", " ") });
                }
                CompQuality cq = worldObject_AtlasPoint.book.TryGetComp<CompQuality>();
                if (cq != null)
                {
                    cq.SetQuality(QualityUtility.GenerateQualityTraderItem(), new ArtGenerationContext?(ArtGenerationContext.Outsider));
                }
                quest.SpawnWorldObject(worldObject_AtlasPoint, null, null);
                quest.End(QuestEndOutcome.Unknown, 0, null, text, QuestPart.SignalListenMode.OngoingOnly, false, false);
                slate.Set<Map>("map", map, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = roverFaction;
                QuestGen.quest.AddPart(qpbgfh);
                List<WorldObject> wos = new List<WorldObject>
                {
                    worldObject_AtlasPoint
                };
                int challengeRating = QuestGen.quest.challengeRating;
                CompStudiableQuestItem cda = worldObject_AtlasPoint.book.TryGetComp<CompStudiableQuestItem>();
                if (cda != null)
                {
                    cda.challengeRating = challengeRating;
                    cda.relevantSkills = new List<SkillDef>();
                    if (!cda.Props.requiredSkillDefs.NullOrEmpty())
                    {
                        cda.relevantSkills.AddRange(cda.Props.requiredSkillDefs);
                    }
                    if (!cda.Props.possibleExtraSkillDefs.NullOrEmpty())
                    {
                        SkillDef extraSkill = cda.Props.possibleExtraSkillDefs.RandomElement();
                        cda.relevantSkills.Add(extraSkill);
                        slate.Set<string>(this.storeExtraSkillAs.GetValue(slate), extraSkill.label, false);
                    }
                    cda.relevantStats = new List<StatDef>();
                    if (!cda.Props.requiredStatDefs.NullOrEmpty())
                    {
                        cda.relevantStats.AddRange(cda.Props.requiredStatDefs);
                    }
                    if (cda.Props.extraStat)
                    {
                        float secondStatDeterminer = Rand.Value;
                        StatDef secondStat;
                        if (secondStatDeterminer <= 0.6f)
                        {
                            secondStat = cda.Props.possibleExtraStatDefsLikely.RandomElement();
                        }
                        else if (secondStatDeterminer <= 0.9f)
                        {
                            secondStat = cda.Props.possibleExtraStatDefsProbable.NullOrEmpty() ? cda.Props.possibleExtraStatDefsLikely.RandomElement() : cda.Props.possibleExtraStatDefsProbable.RandomElement();
                        }
                        else
                        {
                            secondStat = cda.Props.possibleExtraStatDefsUnlikely.NullOrEmpty() ? cda.Props.possibleExtraStatDefsLikely.RandomElement() : cda.Props.possibleExtraStatDefsUnlikely.RandomElement();
                        }
                        cda.relevantStats.Add(secondStat);
                    }
                    if (cda is CompStudiableAtlas atlasComp)
                    {
                        if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.atlas1, mayhemMode))
                        {
                            atlasComp.DTS_on = true;
                            QuestGen.AddQuestDescriptionRules(new List<Rule>
                            {
                                new Rule_String("mutator_DTS_info", this.DTS_description.Formatted())
                            });
                        } else {
                            QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DTS_info", " ") });
                        }
                        if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.atlas2, mayhemMode))
                        {
                            atlasComp.ID_multi = this.ID_multi;
                        }
                    } else {
                        QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DTS_info", " ") });
                    }
                }
                slate.Set<List<WorldObject>>("worldObject", wos, false);
                BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
                QuestGenUtility.AddToOrMakeList(slate, "thingsToDrop", worldObject_AtlasPoint.book);
                QuestGen.slate.Set<Thing>(this.storeBookAs.GetValue(slate), worldObject_AtlasPoint.book, false);
                QuestGen.slate.Set<Faction>("faction", roverFaction, false);
                QuestUtility.AddQuestTag(ref worldObject_AtlasPoint.book.questTags, this.storeBookAs.GetValue(slate));
                quest.AddPart(new QuestPart_LookAtThis(worldObject_AtlasPoint.book));
            }
            base.RunInt();
        }
        private bool TryFindSiteTile(PlanetTile pTile, out PlanetTile tile)
        {
            return TileFinder.TryFindNewSiteTile(out tile, pTile, 2, 20, false, DefDatabase<LandmarkDef>.AllDefsListForReading, 0.5f, true, TileFinderMode.Near, true, false, null, null);
        }
        protected override bool TestRunInt(Slate slate)
        {
            BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            PlanetTile tile = QuestSetupUtility.Quest_TryGetPlanetTile();
            Map map = QuestSetupUtility.Quest_TryGetMap();
            slate.Set<Map>("map", map, false);
            return this.TryFindSiteTile(tile, out PlanetTile num) && BranchQuestSetupUtility.TryFindRoverFaction(out Faction roverFaction) && base.TestRunInt(slate);
        }
        [NoTranslate]
        public SlateRef<string> storeBookAs;
        [NoTranslate]
        public SlateRef<string> storeExtraSkillAs;
        [NoTranslate]
        public SlateRef<string> inSignal;
        [MustTranslate]
        public string DTS_description;
        public float ID_multi;
        [MustTranslate]
        public string TPBU_description;
    }
}
