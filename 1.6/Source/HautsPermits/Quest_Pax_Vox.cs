using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace HautsPermits
{
    /*make a world object, preferably of the PaxTalks class so that the mutators can function and the quest can work correctly. Sets up all three mutators; the PaxTalks world object does the heavy lifting.
     * vox1: Allies Attract Allies sets AAA_difficultyFactor for the PaxTalks
     * vox2: Silver Talks sets ST_difficultyFactor and ST_giftCount for the PaxTalks
     * vox3: The Time For Talk Is Over provides a TTFTIO_chance chance to set the PaxTalks' TTFTIO_points to [TTFTIO_pointsFactor * current storyteller threat points]*/
    public class QuestNode_GenerateVox : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            WorldObject worldObject = WorldObjectMaker.MakeWorldObject(this.def.GetValue(slate));
            worldObject.Tile = this.tile.GetValue(slate);
            if (this.faction.GetValue(slate) != null)
            {
                worldObject.SetFaction(this.faction.GetValue(slate));
            }
            if (worldObject is WorldObject_PaxTalks wopt)
            {
                bool mayhemMode = HVMP_Mod.settings.voxX;
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.vox1, mayhemMode))
                {
                    wopt.AAA_difficultyFactor = this.AAA_difficultyFactor;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_AAA_info", this.AAA_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_AAA_info", " ") });
                }
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.vox2, mayhemMode))
                {
                    wopt.ST_difficultyFactor = this.ST_difficultyFactor;
                    wopt.ST_giftCount = this.ST_giftCountRange.RandomInRange;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("moneyAmount", wopt.ST_giftCount.ToString())
                    });
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_ST_info", this.ST_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_ST_info", " ") });
                }
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.vox3, mayhemMode))
                {
                    if (Rand.Chance(this.TTFTIO_chance))
                    {
                        Map map = QuestGen.slate.Get<Map>("map", null, false) ?? Find.AnyPlayerHomeMap;
                        wopt.TTFTIO_points = (int)(this.TTFTIO_pointsFactor * StorytellerUtility.DefaultThreatPointsNow(map));
                    }
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_TTFTIO_info", this.TTFTIO_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TTFTIO_info", " ") });
                }
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
        public float AAA_difficultyFactor;
        [MustTranslate]
        public string AAA_description;
        public float ST_difficultyFactor;
        public IntRange ST_giftCountRange;
        [MustTranslate]
        public string ST_description;
        public float TTFTIO_chance;
        public float TTFTIO_pointsFactor;
        [MustTranslate]
        public string TTFTIO_description;
    }
    /*Very similar to a normal Peace Talks world object. However,
     * -negotiation ability can be subbed out for another stat as per the PaxTalksComp's negotiationStat field
     * -the outcome curve caps at a higher negotiator's stat value and a lower failure chance
     * -if the quest node gave it TTFTIO_points, then there's a half chance that the quest ends on reaching this world object (neither failure nor success) and an ambush occurs
     * -success chance is adversely impacted by AAA_difficultyFactor (meliorated down to a 1x multi as seen in AAA_Difficulty)
     *   and ST_difficultyFactor (meliorated down to a 1x multi if the caravan has silver >= ST_giftCount. That much silver is removed on visitation)*/
    public class WorldObject_PaxTalks : WorldObject
    {
        public override Material Material
        {
            get
            {
                if (this.cachedMat == null)
                {
                    Color color;
                    if (base.Faction != null)
                    {
                        color = base.Faction.Color;
                    } else {
                        color = Color.white;
                    }
                    this.cachedMat = MaterialPool.MatFrom(this.def.texture, ShaderDatabase.WorldOverlayTransparentLit, color, WorldMaterials.WorldObjectRenderQueue);
                }
                return this.cachedMat;
            }
        }
        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (this.TTFTIO_points > 0)
            {
                text += "HVMP_VoxSiteLabel_TTFTIO".Translate();
            }
            if (this.AAA_difficultyFactor > 1f)
            {
                text += "HVMP_VoxSiteLabel_AAA".Translate();
            }
            if (this.ST_giftCount > 0)
            {
                text += "HVMP_VoxSiteLabel_ST".Translate(this.ST_giftCount);
            }
            return text;
        }
        public void Notify_CaravanArrived(Caravan caravan)
        {
            if (this.TTFTIO_points <= 0f || Rand.Chance(0.5f))
            {
                Pawn pawn = BestCaravanPawnUtility.FindBestDiplomat(caravan);
                if (pawn == null)
                {
                    Messages.Message("MessagePeaceTalksNoDiplomat".Translate(), caravan, MessageTypeDefOf.NegativeEvent, false);
                    return;
                }
                float badOutcomeWeightFactor = this.GetBadOutcomeWeightFactor(pawn, caravan);
                if (this.ST_giftCount > 0)
                {
                    int silverCount = 0;
                    List<Thing> list = CaravanInventoryUtility.AllInventoryItems(caravan);
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing thing = list[i];
                        if (thing.def == ThingDefOf.Silver)
                        {
                            silverCount += thing.stackCount;
                        }
                    }
                    badOutcomeWeightFactor *= Math.Max(1f, ((this.ST_difficultyFactor - 1f) * (this.ST_giftCount - silverCount) / this.ST_giftCount) + 1f);
                    int silverToTake = this.ST_giftCount;
                    List<Thing> list2 = CaravanInventoryUtility.TakeThings(caravan, delegate (Thing thing)
                    {
                        if (ThingDefOf.Silver != thing.def)
                        {
                            return 0;
                        }
                        int numS = Mathf.Min(silverToTake, thing.stackCount);
                        silverToTake -= numS;
                        return numS;
                    });
                    for (int i = 0; i < list2.Count; i++)
                    {
                        list2[i].Destroy(DestroyMode.Vanish);
                    }
                }
                float num = 1f / badOutcomeWeightFactor;
                WorldObject_PaxTalks.tmpPossibleOutcomes.Clear();
                WorldObject_PaxTalks.tmpPossibleOutcomes.Add(new Pair<Action, float>(delegate
                {
                    this.Outcome_Backfire(caravan);
                }, 0.15f * badOutcomeWeightFactor));
                WorldObject_PaxTalks.tmpPossibleOutcomes.Add(new Pair<Action, float>(delegate
                {
                    this.Outcome_TalksFlounder(caravan);
                }, 0.15f));
                WorldObject_PaxTalks.tmpPossibleOutcomes.Add(new Pair<Action, float>(delegate
                {
                    this.Outcome_Success(caravan);
                }, 0.55f * num));
                WorldObject_PaxTalks.tmpPossibleOutcomes.RandomElementByWeight((Pair<Action, float> x) => x.Second).First();
                pawn.skills.Learn(SkillDefOf.Social, 6000f, true, false);
            } else {
                BranchQuestSetupUtility.DoAmbush(caravan, this.TTFTIO_points);
                QuestUtility.SendQuestTargetSignals(this.questTags, "TTFTIO_Ambush", this.Named("SUBJECT"));
            }
            this.Destroy();
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(caravan))
            {
                yield return floatMenuOption;
            }
            foreach (FloatMenuOption floatMenuOption2 in CaravanArrivalAction_VisitPaxTalks.GetFloatMenuOptions(caravan, this))
            {
                yield return floatMenuOption2;
            }
            yield break;
        }
        private void Outcome_Backfire(Caravan caravan)
        {
            Find.LetterStack.ReceiveLetter("LetterLabelPeaceTalks_Backfire".Translate(), this.GetLetterText("HVMP_PaxTalksBackfire".Translate(), caravan), LetterDefOf.NegativeEvent, caravan, base.Faction, null, null, null, 0, true);
            QuestUtility.SendQuestTargetSignals(this.questTags, "Failed", this.Named("SUBJECT"));
        }
        private void Outcome_TalksFlounder(Caravan caravan)
        {
            Find.LetterStack.ReceiveLetter("LetterLabelPeaceTalks_TalksFlounder".Translate(), this.GetLetterText("HVMP_PaxTalksFlounder".Translate(), caravan), LetterDefOf.NeutralEvent, caravan, base.Faction, null, null, null, 0, true);
            QuestUtility.SendQuestTargetSignals(this.questTags, "Failed", this.Named("SUBJECT"));
        }
        private void Outcome_Success(Caravan caravan)
        {
            Find.LetterStack.ReceiveLetter("LetterLabelPeaceTalks_Success".Translate(), this.GetLetterText("HVMP_PaxTalksSuccess".Translate(), caravan), LetterDefOf.PositiveEvent, caravan, base.Faction, null, null, null, 0, true);
            QuestUtility.SendQuestTargetSignals(this.questTags, "Resolved", this.Named("SUBJECT"));
        }
        private string GetLetterText(string baseText, Caravan caravan)
        {
            TaggedString taggedString = baseText;
            Pawn pawn = BestCaravanPawnUtility.FindBestDiplomat(caravan);
            if (pawn != null)
            {
                taggedString += "\n\n" + "PeaceTalksSocialXPGain".Translate(pawn.LabelShort, 6000f.ToString("F0"), pawn.Named("PAWN"));
            }
            return taggedString;
        }
        private float GetBadOutcomeWeightFactor(Pawn diplomat, Caravan caravan)
        {
            StatDef sd = StatDefOf.NegotiationAbility;
            PaxTalksComp ptc = this.GetComponent<PaxTalksComp>();
            if (ptc != null)
            {
                sd = ptc.negotiationStat;
            }
            float statValue = diplomat.GetStatValue(sd, true, -1);
            float num = 0f;
            if (ModsConfig.IdeologyActive)
            {
                bool flag = false;
                using (List<Pawn>.Enumerator enumerator = caravan.pawns.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current == caravan.Faction.leader)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                num = (flag ? (-0.05f) : 0.05f);
            }
            return WorldObject_PaxTalks.GetBadOutcomeWeightFactor(statValue) * (1f + num) * this.AAA_Difficulty;
        }
        private float AAA_Difficulty
        {
            get
            {
                float totalGoodwillFactions = 0f, totalAlliedFactions = 0f;
                Faction p = Faction.OfPlayer;
                if (p != null)
                {
                    foreach (Faction f in Find.FactionManager.AllFactionsVisible)
                    {
                        if (!f.def.HasModExtension<EBranchQuests>() && f != p && f.HasGoodwill && !f.def.PermanentlyHostileTo(p.def))
                        {
                            totalGoodwillFactions += 1f;
                            if (f.RelationKindWith(p) == FactionRelationKind.Ally)
                            {
                                totalAlliedFactions += 1f;
                            }
                        }
                    }
                }
                float result = this.AAA_difficultyFactor;
                if (totalGoodwillFactions <= 0f)
                {
                    return result;
                }
                return Math.Max(1f, ((result - 1f) * (totalGoodwillFactions - totalAlliedFactions) / totalGoodwillFactions) + 1f);
            }
        }
        private static float GetBadOutcomeWeightFactor(float negotationAbility)
        {
            return WorldObject_PaxTalks.BadOutcomeChanceFactorByNegotiationAbility.Evaluate(negotationAbility);
        }
        private Material cachedMat;
        private static readonly SimpleCurve BadOutcomeChanceFactorByNegotiationAbility = new SimpleCurve
        {
            {
                new CurvePoint(0f, 4f),
                true
            },
            {
                new CurvePoint(1f, 1f),
                true
            },
            {
                new CurvePoint(1.5f, 0.4f),
                true
            },
            {
                new CurvePoint(5f, 0.01f),
                true
            }
        };
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.AAA_difficultyFactor, "AAA_difficultyFactor", 1f, false);
            Scribe_Values.Look<int>(ref this.ST_giftCount, "ST_giftCount", -1, false);
            Scribe_Values.Look<float>(ref this.ST_difficultyFactor, "ST_difficultyFactor", 1f, false);
            Scribe_Values.Look<int>(ref this.TTFTIO_points, "TTFTIO_points", -1, false);
        }
        private static List<Pair<Action, float>> tmpPossibleOutcomes = new List<Pair<Action, float>>();
        public float AAA_difficultyFactor = 1f;
        public int ST_giftCount = -1;
        public float ST_difficultyFactor = 1f;
        public int TTFTIO_points;
    }
    public class WorldObjectCompProperties_PaxTalks : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_PaxTalks()
        {
            this.compClass = typeof(PaxTalksComp);
        }
        public StatDef negotiationStat;
    }
    //lets you sub out Negotiation Ability for a different stat (such as the Vanilla Skills Expanded stat) entirely in XML
    public class PaxTalksComp : WorldObjectComp
    {
        public override void Initialize(WorldObjectCompProperties props)
        {
            base.Initialize(props);
            if (this.props is WorldObjectCompProperties_PaxTalks woccpt)
            {
                this.negotiationStat = woccpt.negotiationStat;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look<StatDef>(ref this.negotiationStat, "negotiationStat");
        }
        public StatDef negotiationStat;
    }
    //the bespoke world object necessitates a bespoke caravan arrival action
    public class CaravanArrivalAction_VisitPaxTalks : CaravanArrivalAction
    {
        public override string Label
        {
            get
            {
                return "VisitPeaceTalks".Translate(this.paxTalks.Label);
            }
        }
        public override string ReportString
        {
            get
            {
                return "CaravanVisiting".Translate(this.paxTalks.Label);
            }
        }
        public CaravanArrivalAction_VisitPaxTalks()
        {
        }
        public CaravanArrivalAction_VisitPaxTalks(WorldObject_PaxTalks paxTalks)
        {
            this.paxTalks = paxTalks;
        }
        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
            if (!floatMenuAcceptanceReport)
            {
                return floatMenuAcceptanceReport;
            }
            if (this.paxTalks != null && this.paxTalks.Tile != destinationTile)
            {
                return false;
            }
            return CaravanArrivalAction_VisitPaxTalks.CanVisit(caravan, this.paxTalks);
        }
        public override void Arrived(Caravan caravan)
        {
            this.paxTalks.Notify_CaravanArrived(caravan);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<WorldObject_PaxTalks>(ref this.paxTalks, "paxTalks", false);
        }
        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, WorldObject_PaxTalks paxTalks)
        {
            return paxTalks != null && paxTalks.Spawned;
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, WorldObject_PaxTalks paxTalks)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_VisitPaxTalks>(() => CaravanArrivalAction_VisitPaxTalks.CanVisit(caravan, paxTalks), () => new CaravanArrivalAction_VisitPaxTalks(paxTalks), "VisitPeaceTalks".Translate(paxTalks.Label), caravan, paxTalks.Tile, paxTalks, null);
        }
        private WorldObject_PaxTalks paxTalks;
    }
}
