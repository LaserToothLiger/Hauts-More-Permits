using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;

namespace HautsPermits
{
    /*bandit camp mission, but it's also a RoverIntermediary and we need to handle the mutators here.
     * theseus1: A Quality All Its Own multiplies the points of the bandit camp by a random value within AQAIO_pointFactor
     * theseus2: Grave Goods flicks GG_on for the Mutator_GG_SD comp of the site, modifying the contents of its map
     * theseus3: Static Defense flicks SD_on for the same*/
    public class QuestNode_Theseus : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            QuestGenUtility.TestRunAdjustPointsForDistantFight(slate);
            this.ResolveParameters(slate, out int num, out int num2, out Map map);
            return num != -1 && BranchQuestSetupUtility.TryFindRoverFaction(out Faction roverFaction) && this.TryGetSiteFaction(out Faction faction);
        }
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            string text = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("BanditCamp");
            QuestGenUtility.RunAdjustPointsForDistantFight();
            int num = slate.Get<int>("points", 0, false);
            if (num <= 0)
            {
                num = Rand.RangeInclusive(200, 2000);
            }
            this.ResolveParameters(slate, out int num2, out int num3, out Map map);
            this.TryFindSiteTile(out PlanetTile num4, false);
            BranchQuestSetupUtility.TryFindRoverFaction(out Faction roverFaction);
            slate.Set<Faction>("askerFaction", roverFaction, false);
            slate.Set<int>("requiredPawnCount", num2, false);
            slate.Set<Map>("map", map, false);
            QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
            qpbgfh.faction = roverFaction;
            QuestGen.quest.AddPart(qpbgfh);
            Site site = this.GenerateSite(roverFaction.leader, (float)num, num2, num3, num4);
            string text2 = QuestGenUtility.HardcodedSignalWithQuestID("askerFaction.BecameHostileToPlayer");
            string text3 = QuestGenUtility.QuestTagSignal(text, "AllEnemiesDefeated");
            string signalSentSatisfied = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.SentSatisfied");
            string text4 = QuestGenUtility.QuestTagSignal(text, "MapRemoved");
            string signalChosenPawn = QuestGen.GenerateNewSignal("ChosenPawnSignal", true);
            this.parms.giverFaction = roverFaction;
            this.parms.allowGoodwill = true;
            this.parms.allowRoyalFavor = true;
            this.parms.thingRewardDisallowed = true;
            slate.Set<Thing>("asker", roverFaction.leader, false);
            BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            QuestGen.slate.Set<Faction>("faction", roverFaction, false);
            quest.GiveRewards(new RewardsGeneratorParams
            {
                allowGoodwill = true,
                allowRoyalFavor = true,
                giverFaction = roverFaction,
                thingRewardDisallowed = true,
                rewardValue = slate.Get<float>("rewardValue", 200f, false),
                chosenPawnSignal = signalChosenPawn
            }, text3, null, null, null, null, null, delegate
            {
                Quest quest2 = quest;
                LetterDef choosePawn = LetterDefOf.ChoosePawn;
                string text8 = null;
                string royalFavorLabel = roverFaction.def.royalFavorLabel;
                string text9 = "LetterTextHonorAward_BanditCamp".Translate(roverFaction.def.royalFavorLabel);
                quest2.Letter(choosePawn, text8, signalChosenPawn, null, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, text9, null, royalFavorLabel, null, signalSentSatisfied);
            }, null, true, roverFaction.leader, false, false, null);
            Thing shuttle = QuestGen_Shuttle.GenerateShuttle(null, null, null, true, true, false, num2, true, true, false, true, site, map.Parent, num2, null, false, false, false, false, true);
            slate.Set<Thing>("shuttle", shuttle, false);
            QuestUtility.AddQuestTag(ref shuttle.questTags, text);
            quest.SpawnWorldObject(site, null, null);
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.theseus2, HVMP_Mod.settings.theseusX))
            {
                Mutator_GG_SDComp component = site.GetComponent<Mutator_GG_SDComp>();
                if (component != null)
                {
                    component.GG_on = true;
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_GG_info", this.GG_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_GG_info", " ") });
            }
            TransportShip transportShip = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, null, shuttle, null).transportShip;
            slate.Set<TransportShip>("transportShip", transportShip, false);
            QuestUtility.AddQuestTag(ref transportShip.questTags, text);
            quest.SendTransportShipAwayOnCleanup(transportShip, true, TransportShipDropMode.None);
            quest.AddShipJob_Arrive(transportShip, map.Parent, null, null, ShipJobStartMode.Instant, Faction.OfEmpire, null);
            quest.AddShipJob_WaitSendable(transportShip, site, true, false, null);
            quest.AddShipJob(transportShip, ShipJobDefOf.Unload, ShipJobStartMode.Queue, null);
            quest.AddShipJob_WaitSendable(transportShip, map.Parent, true, false, null);
            quest.AddShipJob(transportShip, ShipJobDefOf.Unload, ShipJobStartMode.Queue, null);
            quest.AddShipJob_FlyAway(transportShip, -1, null, TransportShipDropMode.None, null);
            quest.TendPawns(null, shuttle, signalSentSatisfied);
            quest.RequiredShuttleThings(shuttle, site, QuestGenUtility.HardcodedSignalWithQuestID("transportShip.FlewAway"), true, -1);
            quest.ShuttleLeaveDelay(shuttle, 60000, null, Gen.YieldSingle<string>(signalSentSatisfied), null, delegate
            {
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true, false);
            });
            string text5 = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Killed");
            quest.FactionGoodwillChange(roverFaction, BranchQuestSetupUtility.ExpectationBasedGoodwillLoss(map, true, false, roverFaction), text5, true, true, true, HistoryEventDefOf.ShuttleDestroyed, QuestPart.SignalListenMode.OngoingOnly, true);
            quest.End(QuestEndOutcome.Fail, 0, null, text5, QuestPart.SignalListenMode.OngoingOnly, true, false);
            quest.SignalPass(delegate
            {
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true, false);
            }, text2, null);
            quest.FeedPawns(null, shuttle, signalSentSatisfied);
            QuestUtility.AddQuestTag(ref site.questTags, text);
            slate.Set<Site>("site", site, false);
            quest.SignalPassActivable(delegate
            {
                quest.Message("MessageMissionGetBackToShuttle".Translate(site.Faction.Named("FACTION")), MessageTypeDefOf.PositiveEvent, false, null, new LookTargets(shuttle), null);
                quest.Notify_PlayerRaidedSomeone(null, site, null);
            }, signalSentSatisfied, text3, null, null, null, false);
            quest.SignalPassAllSequence(delegate
            {
                quest.End(QuestEndOutcome.Success, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true, false);
            }, new List<string> { signalSentSatisfied, text3, text4 }, null);
            Quest quest3 = quest;
            Action action = delegate
            {
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true, false);
            };
            string text6 = null;
            string text7 = text3;
            quest3.SignalPassActivable(action, text6, text4, null, null, text7, false);
            int num5 = (int)(this.timeLimitDays.RandomInRange * 60000f);
            slate.Set<int>("timeoutTicks", num5, false);
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.theseus3, HVMP_Mod.settings.theseusX))
            {
                Mutator_GG_SDComp component = site.GetComponent<Mutator_GG_SDComp>();
                if (component != null)
                {
                    component.SD_on = true;
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_SD_info", this.SD_description.Formatted())
                });
            }
            else
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_SD_info", " ") });
            }
            quest.WorldObjectTimeout(site, num5, null, null, false, null, true);
            List<Rule> list = new List<Rule>();
            list.AddRange(GrammarUtility.RulesForWorldObject("site", site, true));
            QuestGen.AddQuestDescriptionRules(list);
        }
        protected bool TryFindSiteTile(out PlanetTile tile, bool exitOnFirstTileFound = false)
        {
            return TileFinder.TryFindNewSiteTile(out tile, 80, 85, false, null, 0.5f, true, TileFinderMode.Near, exitOnFirstTileFound, this.canBeSpace, null, null);
        }
        private void ResolveParameters(Slate slate, out int requiredPawnCount, out int population, out Map colonyMap)
        {
            try
            {
                foreach (Map map in Find.Maps)
                {
                    if (map.IsPlayerHome)
                    {
                        QuestNode_Theseus.tmpMaps.Add(map);
                    }
                }
                colonyMap = QuestNode_Theseus.tmpMaps.RandomElementWithFallback(null);
                if (colonyMap == null)
                {
                    population = -1;
                    requiredPawnCount = -1;
                }
                else
                {
                    population = (slate.Exists("population", false) ? slate.Get<int>("population", 0, false) : colonyMap.mapPawns.FreeColonists.Count);
                    requiredPawnCount = Math.Max(this.GetRequiredPawnCount(population, (float)slate.Get<int>("points", 0, false)), 1);
                }
            }
            finally
            {
                QuestNode_Theseus.tmpMaps.Clear();
            }
        }
        protected int GetRequiredPawnCount(int population, float threatPoints)
        {
            if (population == 0)
            {
                return -1;
            }
            int num = -1;
            for (int i = 1; i <= population; i++)
            {
                if (this.GetSiteThreatPoints(threatPoints, population, i) >= 200f)
                {
                    num = i;
                    break;
                }
            }
            if (num == -1)
            {
                return -1;
            }
            return Math.Max(0, Rand.Range(num, population));
        }
        private float GetSiteThreatPoints(float threatPoints, int population, int pawnCount)
        {
            return threatPoints * (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.theseus1, HVMP_Mod.settings.theseusX) ? this.AQAIO_pointFactor.RandomInRange : 1f);
        }
        protected Site GenerateSite(Pawn asker, float threatPoints, int pawnCount, int population, int tile)
        {
            this.TryGetSiteFaction(out Faction faction);
            Site site = QuestGen_Sites.GenerateSite(new SitePartDefWithParams[]
            {
                new SitePartDefWithParams(SitePartDefOf.BanditCamp, new SitePartParams
                {
                    threatPoints = Math.Max(this.GetSiteThreatPoints(Math.Max(threatPoints,200), population, pawnCount),500)
                })
            }, tile, faction, false, null);
            site.factionMustRemainHostile = true;
            site.desiredThreatPoints = site.ActualThreatPoints;
            return site;
        }
        private bool TryGetSiteFaction(out Faction faction)
        {
            faction = BranchQuestSetupUtility.GetAnEnemyFaction();
            return faction != null;
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        private static List<Map> tmpMaps = new List<Map>();
        public SlateRef<string> customLetterLabel;
        public SlateRef<string> customLetterText;
        public QuestNode nodeIfChosenPawnSignalUsed;
        public RewardsGeneratorParams parms;
        public SlateRef<int?> variants;
        public bool canBeSpace;
        public FloatRange timeLimitDays = new FloatRange(2f, 5f);
        public FloatRange AQAIO_pointFactor = new FloatRange(2f, 3f);
        [MustTranslate]
        public string GG_description;
        [MustTranslate]
        public string SD_description;
    }
    /*if GG_on, give up to GG_maxBnnditsBuffed of the bandits a random-per-pawn buff from GG_buffs
     * if SD_on, haphazardly plop down extra buildings from SD_turrets in the bandit camp, each on a different bandit's location (up to SD_turretsPerPawn such buildings are created)*/
    public class WorldObjectCompProperties_Mutator_GG_SD : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_Mutator_GG_SD()
        {
            this.compClass = typeof(Mutator_GG_SDComp);
        }
        public int GG_maxBanditsBuffed;
        public List<HediffDef> GG_buffs;
        public List<ThingDef> SD_turrets;
        public float SD_turretsPerPawn;
    }
    public class Mutator_GG_SDComp : WorldObjectComp
    {
        public override void Initialize(WorldObjectCompProperties props)
        {
            base.Initialize(props);
            WorldObjectCompProperties_Mutator_GG_SD proper = props as WorldObjectCompProperties_Mutator_GG_SD;
            if (proper != null)
            {
                this.GG_buffs = proper.GG_buffs;
                this.GG_maxBanditsBuffed = proper.GG_maxBanditsBuffed;
                this.SD_turretsPerPawn = proper.SD_turretsPerPawn;
                this.SD_turrets = proper.SD_turrets;
            }
        }
        public override void PostMapGenerate()
        {
            if (this.parent is MapParent mp)
            {
                Map m = mp.Map;
                if (this.GG_on && !this.GG_buffs.NullOrEmpty())
                {
                    int GG_count = this.GG_maxBanditsBuffed;
                    List<Pawn> pawns = m.mapPawns.PawnsInFaction(this.parent.Faction).InRandomOrder().ToList();
                    foreach (Pawn p in pawns)
                    {
                        if (p.Spawned)
                        {
                            GG_count--;
                            if (GG_count >= 0)
                            {
                                if (p.RaceProps.Humanlike)
                                {
                                    p.health.AddHediff(this.GG_buffs.RandomElement());
                                }
                            }
                        }
                    }
                }
                if (this.SD_on && !this.SD_turrets.NullOrEmpty())
                {
                    List<Pawn> pawns = m.mapPawns.PawnsInFaction(this.parent.Faction).Where((Pawn paw) => paw.RaceProps.Humanlike).InRandomOrder().ToList();
                    int SD_count = Math.Max(1, (int)(pawns.Count * this.SD_turretsPerPawn));
                    foreach (Pawn p in pawns)
                    {
                        if (p.Spawned)
                        {
                            if (SD_count > 0)
                            {
                                Thing t = ThingMaker.MakeThing(this.SD_turrets.RandomElement());
                                GenPlace.TryPlaceThing(t, p.Position, m, ThingPlaceMode.Near, null, null, null, 1);
                                t.SetFactionDirect(p.Faction);
                                SD_count--;
                            }
                        }
                    }
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.GG_on, "GG_on", false, false);
            Scribe_Values.Look<int>(ref this.GG_maxBanditsBuffed, "GG_maxBanditsBuffed", 10, false);
            Scribe_Collections.Look<HediffDef>(ref this.GG_buffs, "GG_buffs", LookMode.Undefined, LookMode.Undefined);
            Scribe_Values.Look<bool>(ref this.SD_on, "SD_on", false, false);
            Scribe_Values.Look<float>(ref this.SD_turretsPerPawn, "SD_turretsPerPawn", 0.5f, false);
            Scribe_Collections.Look<ThingDef>(ref this.SD_turrets, "SD_turrets", LookMode.Undefined, LookMode.Undefined);
        }
        public bool GG_on;
        public int GG_maxBanditsBuffed;
        public List<HediffDef> GG_buffs;
        public bool SD_on;
        public List<ThingDef> SD_turrets;
        public float SD_turretsPerPawn;
    }
}
