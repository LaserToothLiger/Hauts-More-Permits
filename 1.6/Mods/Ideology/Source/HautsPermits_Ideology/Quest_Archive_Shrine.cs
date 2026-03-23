using HautsPermits;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace HautsPermits_Ideology
{
    /*veeery very like a Worshipped Terminal quest. But it doesn't have to be a gentle tribe protecting it; it can be any non-branch, non-hostile, non-hidden combat-capable faction type that can have settlements and quest sites
     * Also, it has to handle mutators.
     * shrine1: Population Boom multiplies the site's points by PB_populationFactor (as opposed to just being base_populationFactor)
     * shrine2: This Tape Will Self-destruct flicks a switch on the terminal's CompTTWSD
     * shrine3: Xenophobia multiplies the site's point by XP_populationFactor and sets the number of ticks until the defenders turn hostile to 30 (0.5s) instead of 25000 (10h)*/
    public class QuestNode_ArchiveShrine : QuestNode
    {
        protected override void RunInt()
        {
            if (!ModLister.CheckIdeology("Worshipped terminal"))
            {
                return;
            }
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Map map = slate.Get<Map>("map", null, false) ?? Find.AnyPlayerHomeMap;
            QuestGenUtility.RunAdjustPointsForDistantFight();
            float num = slate.Get<float>("points", 0f, false);
            slate.Set<Faction>("playerFaction", Faction.OfPlayer, false);
            slate.Set<bool>("allowViolentQuests", Find.Storyteller.difficulty.allowViolentQuests, false);
            bool mayhemMode = HVMP_Mod.settings.shrineX;
            this.TryFindSiteTile(out PlanetTile planetTile);
            FactionDef factionDef = DefDatabase<FactionDef>.AllDefsListForReading.Where((FactionDef fd) => !fd.hidden && fd.humanlikeFaction && !fd.permanentEnemy && !fd.naturalEnemy && !fd.HasModExtension<EBranchQuests>() && fd.canGenerateQuestSites && !fd.pawnGroupMakers.NullOrEmpty() && fd.pawnGroupMakers.ContainsAny((PawnGroupMaker pgm) => pgm.kindDef == PawnGroupKindDefOf.Combat) && fd.pawnGroupMakers.ContainsAny((PawnGroupMaker pgm) => pgm.kindDef == PawnGroupKindDefOf.Settlement)).RandomElement() ?? FactionDefOf.TribeCivil;
            List<FactionRelation> list = new List<FactionRelation>();
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (!faction.def.PermanentlyHostileTo(factionDef))
                {
                    list.Add(new FactionRelation
                    {
                        other = faction,
                        kind = FactionRelationKind.Neutral
                    });
                }
            }
            QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("minorFactionLabel", factionDef.label)
                });
            FactionGeneratorParms factionGeneratorParms = new FactionGeneratorParms(factionDef, default(IdeoGenerationParms), true);
            factionGeneratorParms.ideoGenerationParms = new IdeoGenerationParms(factionGeneratorParms.factionDef, false, null, null, null, false, false, false, false, "", null, null, false, "", false);
            Faction shrineFaction = FactionGenerator.NewGeneratedFactionWithRelations(factionGeneratorParms, list);
            shrineFaction.temporary = true;
            shrineFaction.factionHostileOnHarmByPlayer = true;
            shrineFaction.neverFlee = true;
            Find.FactionManager.Add(shrineFaction);
            quest.ReserveFaction(shrineFaction);
            string text = QuestGenUtility.HardcodedSignalWithQuestID("playerFaction.BuiltBuilding");
            string text2 = QuestGenUtility.HardcodedSignalWithQuestID("playerFaction.PlacedBlueprint");
            bool PB_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.shrine1, mayhemMode);
            bool TTWSD_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.shrine2, mayhemMode);
            bool XP_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.shrine3, mayhemMode);
            num = this.base_populationFactor * Mathf.Max(num, shrineFaction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Settlement, null));
            if (PB_on)
            {
                num *= this.PB_populationFactor;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_PB_info", this.PB_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_PB_info", " ") });
            }
            if (XP_on)
            {
                num *= this.XP_populationFactor;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_XP_info", this.XP_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_XP_info", this.nonXP_description.Formatted())
                });
            }
            QuestGen.AddQuestDescriptionRules(new List<Rule>
            {
                new Rule_String("mutator_XP_info_nonviolent", this.nonviolent_description.Formatted())
            });
            SitePartParams sitePartParams = new SitePartParams
            {
                points = num,
                threatPoints = num
            };
            Site site = QuestGen_Sites.GenerateSite(Gen.YieldSingle<SitePartDefWithParams>(new SitePartDefWithParams(HVMPDefOf.HVMP_Shrine, sitePartParams)), planetTile, shrineFaction, false, null, null);
            site.doorsAlwaysOpenForPlayerPawns = true;
            slate.Set<Site>("site", site, false);
            quest.SpawnWorldObject(site, null, null);
            int num2 = XP_on ? 30 : 25000;
            site.GetComponent<TimedMakeFactionHostile>().SetupTimer(num2, "WorshippedTerminalFactionBecameHostileTimed".Translate(shrineFaction.Named("FACTION")), null);
            Thing thing = site.parts[0].things.First((Thing t) => t.def == ThingDefOf.AncientTerminal_Worshipful);
            if (TTWSD_on)
            {
                CompTTWSD cttwsd = thing.TryGetComp<CompTTWSD>();
                if (cttwsd != null)
                {
                    cttwsd.TTWSD_on = true;
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TTWSD_info", this.TTWSD_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TTWSD_info", " ") });
            }
            slate.Set<Thing>("terminal", thing, false);
            string text3 = QuestGenUtility.HardcodedSignalWithQuestID("terminal.Hacked");
            string text4 = QuestGenUtility.HardcodedSignalWithQuestID("terminal.HackingStarted");
            string text6 = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
            string text7 = QuestGenUtility.HardcodedSignalWithQuestID("shrineFaction.FactionMemberArrested");
            CompHackable compHackable = thing.TryGetComp<CompHackable>();
            compHackable.hackingStartedSignal = text4;
            compHackable.defence = (float)QuestNode_ArchiveShrine.HackDefenceRange.RandomInRange;
            quest.Message("[terminalHackedMessage]", null, true, null, null, text3);
            quest.SetFactionHidden(shrineFaction, false, null);
            if (Find.Storyteller.difficulty.allowViolentQuests)
            {
                quest.FactionRelationToPlayerChange(shrineFaction, FactionRelationKind.Hostile, false, text4);
                quest.StartRecurringRaids(site, new FloatRange?(new FloatRange(24f, 24f)), new int?(2500), text4);
                quest.BuiltNearSettlement(shrineFaction, site, delegate
                {
                    quest.FactionRelationToPlayerChange(shrineFaction, FactionRelationKind.Hostile, true, null);
                }, null, text, null, null, QuestPart.SignalListenMode.OngoingOnly);
                quest.BuiltNearSettlement(shrineFaction, site, delegate
                {
                    quest.Message("WarningBuildingCausesHostility".Translate(shrineFaction.Named("FACTION")), MessageTypeDefOf.CautionInput, false, null, null, null);
                }, null, text2, null, null, QuestPart.SignalListenMode.OngoingOnly);
                quest.FactionRelationToPlayerChange(shrineFaction, FactionRelationKind.Hostile, true, text7);
            }
            slate.Set<Map>("map", map, false);
            slate.Set<int>("timer", num2, false);
            slate.Set<Faction>("shrineFaction", shrineFaction, false);
        }
        private bool TryFindSiteTile(out PlanetTile tile)
        {
            return TileFinder.TryFindNewSiteTile(out tile, 2, 20, false, null, 0.5f, true, TileFinderMode.Near, false, false, null, null);
        }
        protected override bool TestRunInt(Slate slate)
        {
            return this.TryFindSiteTile(out PlanetTile planetTile);
        }
        private static IntRange HackDefenceRange = new IntRange(10, 100);
        public float base_populationFactor;
        public float PB_populationFactor;
        [MustTranslate]
        public string PB_description;
        [MustTranslate]
        public string TTWSD_description;
        public float XP_populationFactor;
        [MustTranslate]
        public string XP_description;
        [MustTranslate]
        public string nonXP_description;
        [MustTranslate]
        public string nonviolent_description;
    }
    //makes the terminal.
    public class SitePartWorker_Shrine : SitePartWorker
    {
        public override void Init(Site site, SitePart sitePart)
        {
            base.Init(site, sitePart);
            sitePart.things = new ThingOwner<Thing>(sitePart);
            Thing thing = ThingMaker.MakeThing(ThingDefOf.AncientTerminal_Worshipful, null);
            sitePart.things.TryAdd(thing, true);
        }
        public override string GetArrivedLetterPart(Map map, out LetterDef preferredLetterDef, out LookTargets lookTargets)
        {
            return base.GetArrivedLetterPart(map, out preferredLetterDef, out lookTargets).Formatted(map.Parent.GetComponent<TimedMakeFactionHostile>().TicksLeft.Value.ToStringTicksToPeriod(true, false, true, true, false).Named("TIMER"));
        }
    }
    //patched into the worshipful terminal thingdef, if this is turned TTWSD_on (by the quest node up above), hacking it will cause thingToSpawn to be generated nearby. If it has CompExplosive, its wick is started
    public class CompProperties_TTWSD : CompProperties
    {
        public CompProperties_TTWSD()
        {
            this.compClass = typeof(CompTTWSD);
        }
        public ThingDef thingToSpawn;
    }
    public class CompTTWSD : ThingComp
    {
        public CompProperties_TTWSD Props
        {
            get
            {
                return (CompProperties_TTWSD)this.props;
            }
        }
        public override void Notify_Hacked(Pawn hacker = null)
        {
            base.Notify_Hacked(hacker);
            if (this.TTWSD_on)
            {
                Thing thing = ThingMaker.MakeThing(this.Props.thingToSpawn, null);
                GenPlace.TryPlaceThing(thing, this.parent.Position, this.parent.Map, ThingPlaceMode.Near, null, null, null, 1);
                CompExplosive ce = thing.TryGetComp<CompExplosive>();
                if (ce != null)
                {
                    ce.StartWick();
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.TTWSD_on, "TTWSD_on", false, false);
        }
        public bool TTWSD_on;
    }
}
