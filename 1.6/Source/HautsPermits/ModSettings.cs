using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HautsPermits
{
    /*|||||PERMIT SETTINGS|||||
     * permitsScaleBySeniority: makes certain permits (in the XML, most or all of these will have the HautsPermits.ScalingDisabledDescription DME) have improved effects for permit-users with higher titles
     * |||||QUEST SETTINGS|||||
     * maximumChaosMode: when an e-branch issues a quest, normally this causes the quest-issuing cooldowns of ALL branch factions to restart. This makes it only start the cooldown for the branch that actually issued the quest.
     * occultCanAlwaysBeEncountered: the Occult Branch (or really branch whose EBranchQuests DME has tiedToAnomalyThreatFraction = true) can only issue you their unique quests if the Anomaly system is active, OR if this is true
     * min|maxBranchQuestInterval: range (in days) that the cooldown between branch quest issuings can be done
     * questRewardFactor: multiplies the amount of goodwill or standing (favor) you get from completing a branch quest. See tooltip for some math, as it does not scale linearly in practice
     * bratBehaviorMinExpectationLvl, bratBehaviorMinSeniorityLvl: thresholds at which a quest will trigger the penalties set by the next two settings.
     *   Expectation level is that of your colony. If you have multiple, it uses the highest expectation level among them.
     *   Seniority level is the "seniority" of the highest title any of your pawns hold with the faction issuing the quest. E-branch titles have a seniority = 100x their apparent rank in the hierarchy, e.g. the highest title has 600.
     * goodwillQuestRefusal|FailureLoss: how much Goodwill, you Lose, for Refusing a quest (letting it expire without accepting it) or Losing one (ewisott). Not all quests have loss conditions.
     * |||||BRANCH PLATFORM SETTINGS|||||
     * makeNewBranchPlatformInterval: every so often, new branch platforms are created for every branch faction. This happens at the same time for all such factions. The interval between instances of this effect = this mod setting.
     * maxPlatformsPerBranch: a faction will not gain a new branch platform during the aforementioned process if it already has this many.
     * platformDefenderScale: any given Branch Platform has a base amount of defenders, but then they also gain an additional stack of defenders per point in this setting.
     * authorizerCooldownDays: using a permit via a permit authorizer puts it on cooldown for [the permit's standing cost * this many days].
     * authorizerStandingGain: injecting a permit authorizer into a pawn gives it this much standing with the authorizer's faction.
     * |||||QUEST MUTATORS|||||
     * any given quest has three "mutators", permutations on the quest's nature that make it more challenging. Thus, each quest has three mod settings (labeled __1, __2, and __3) as well as an __X setting.
     *   Turning on one of these settings will cause its corresponding mutator to be applied to any new instance of a quest of that type.
     *   1, 2, and 3 are in English alphabetical order. E.g. Commerce Fortification has fort1 (Props to the Makeup Department), fort2 (Riches to Ruins), and fort3 (What's Bugging You?).
     *   X refers to "mayhem mode". Toggling this on enables any quest mutators that have NOT been toggled on to have a 35% chance to be applied to any new instance of a quest of that type anyways.
     *   You can find where these take effect in the actual code of the quests' nodes.*/
    public class HVMP_Settings : ModSettings
    {
        public bool permitsScaleBySeniority = false;
        public bool maximumChaosMode = false;
        public bool occultCanAlwaysBeEncountered = false;
        public float minBranchQuestInterval = 12f;
        public float maxBranchQuestInterval = 18f;
        public float questRewardFactor = 1f;
        public int bratBehaviorMinExpectationLvl = 999;
        public int bratBehaviorMinSeniorityLvl = 99999;
        public int goodwillQuestRefusalLoss = 0;
        public int goodwillQuestFailureLoss = 4;
        public float makeNewBranchPlatformInterval = 15f;
        public float maxPlatformsPerBranch = 1f;
        public float platformDefenderScale = 3f;
        public float authorizerCooldownDays = 3f;
        public float authorizerStandingGain = 6f;
        public bool fort1, fort2, fort3, interv1, interv2, interv3, mm1, mm2, mm3, research1, research2, research3, transport1, transport2, transport3;
        public bool bellum1, bellum2, bellum3, caelum1, caelum2, caelum3, machina1, machina2, machina3, mundi1, mundi2, mundi3, vox1, vox2, vox3;
        public bool atlas1, atlas2, atlas3, icarus1, icarus2, icarus3, laelaps1, laelaps2, laelaps3, odyssey1, odyssey2, odyssey3, theseus1, theseus2, theseus3;
        public bool ethnog1, ethnog2, ethnog3, remnant1, remnant2, remnant3, replica1, replica2, replica3, sat1, sat2, sat3, shrine1, shrine2, shrine3;
        public bool cs1, cs2, cs3, ec1, ec2, ec3, fw1, fw2, fw3, hd1, hd2, hd3, ra1, ra2, ra3;
        public bool barker1, barker2, barker3, lc1, lc2, lc3, natali1, natali2, natali3, romero1, romero2, romero3, wells1, wells2, wells3;
        public bool fortX, intervX, mmX, researchX, transportX, bellumX, caelumX, machinaX, mundiX, voxX, atlasX, icarusX, laelapsX, odysseyX, theseusX, ethnogX, remnantX, replicaX, satX, shrineX, csX, ecX, fwX, hdX, raX, barkerX, lcX, nataliX, romeroX, wellsX;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref permitsScaleBySeniority, "permitsScaleBySeniority", false);
            Scribe_Values.Look(ref maximumChaosMode, "maximumChaosMode", false);
            Scribe_Values.Look(ref occultCanAlwaysBeEncountered, "occultCanAlwaysBeEncountered", false);
            Scribe_Values.Look(ref minBranchQuestInterval, "minBranchQuestInterval", 11f);
            if (maxBranchQuestInterval < minBranchQuestInterval)
            {
                maxBranchQuestInterval = minBranchQuestInterval;
            }
            Scribe_Values.Look(ref maxBranchQuestInterval, "maxBranchQuestInterval", 15f);
            Scribe_Values.Look(ref questRewardFactor, "questRewardFactor", 1f);
            Scribe_Values.Look(ref bratBehaviorMinExpectationLvl, "bratBehaviorMinExpectationLvl", 999);
            Scribe_Values.Look(ref bratBehaviorMinSeniorityLvl, "bratBehaviorMinSeniorityLvl", 99999);
            Scribe_Values.Look(ref goodwillQuestRefusalLoss, "goodwillQuestRefusalLoss", 0);
            Scribe_Values.Look(ref goodwillQuestFailureLoss, "goodwillQuestFailureLoss", 4);
            Scribe_Values.Look(ref makeNewBranchPlatformInterval, "makeNewBranchPlatformInterval", 15f);
            Scribe_Values.Look(ref maxPlatformsPerBranch, "maxPlatformsPerBranch", 1f);
            Scribe_Values.Look(ref platformDefenderScale, "platformDefenderScale", 3f);
            Scribe_Values.Look(ref authorizerCooldownDays, "authorizerCooldownDays", 3f);
            Scribe_Values.Look(ref authorizerStandingGain, "authorizerStandingGain", 6f);
            Scribe_Values.Look(ref fort1, "fort1", false);
            Scribe_Values.Look(ref fort2, "fort2", false);
            Scribe_Values.Look(ref fort3, "fort3", false);
            Scribe_Values.Look(ref fortX, "fortX", false);
            Scribe_Values.Look(ref interv1, "interv1", false);
            Scribe_Values.Look(ref interv2, "interv2", false);
            Scribe_Values.Look(ref interv3, "interv3", false);
            Scribe_Values.Look(ref intervX, "intervX", false);
            Scribe_Values.Look(ref mm1, "mm1", false);
            Scribe_Values.Look(ref mm2, "mm2", false);
            Scribe_Values.Look(ref mm3, "mm3", false);
            Scribe_Values.Look(ref mmX, "mmX", false);
            Scribe_Values.Look(ref research1, "research1", false);
            Scribe_Values.Look(ref research2, "research2", false);
            Scribe_Values.Look(ref research3, "research3", false);
            Scribe_Values.Look(ref researchX, "researchX", false);
            Scribe_Values.Look(ref transport1, "transport1", false);
            Scribe_Values.Look(ref transport2, "transport2", false);
            Scribe_Values.Look(ref transport3, "transport3", false);
            Scribe_Values.Look(ref transportX, "transportX", false);
            Scribe_Values.Look(ref bellum1, "bellum1", false);
            Scribe_Values.Look(ref bellum2, "bellum2", false);
            Scribe_Values.Look(ref bellum3, "bellum3", false);
            Scribe_Values.Look(ref bellumX, "bellumX", false);
            Scribe_Values.Look(ref caelum1, "caelum1", false);
            Scribe_Values.Look(ref caelum2, "caelum2", false);
            Scribe_Values.Look(ref caelum3, "caelum3", false);
            Scribe_Values.Look(ref caelumX, "caelumX", false);
            Scribe_Values.Look(ref machina1, "machina1", false);
            Scribe_Values.Look(ref machina2, "machina2", false);
            Scribe_Values.Look(ref machina3, "machina3", false);
            Scribe_Values.Look(ref machinaX, "machinaX", false);
            Scribe_Values.Look(ref mundi1, "mundi1", false);
            Scribe_Values.Look(ref mundi2, "mundi2", false);
            Scribe_Values.Look(ref mundi3, "mundi3", false);
            Scribe_Values.Look(ref mundiX, "mundiX", false);
            Scribe_Values.Look(ref vox1, "vox1", false);
            Scribe_Values.Look(ref vox2, "vox2", false);
            Scribe_Values.Look(ref vox3, "vox3", false);
            Scribe_Values.Look(ref voxX, "voxX", false);
            Scribe_Values.Look(ref atlas1, "atlas1", false);
            Scribe_Values.Look(ref atlas2, "atlas2", false);
            Scribe_Values.Look(ref atlas3, "atlas3", false);
            Scribe_Values.Look(ref atlasX, "atlasX", false);
            Scribe_Values.Look(ref icarus1, "icarus1", false);
            Scribe_Values.Look(ref icarus2, "icarus2", false);
            Scribe_Values.Look(ref icarus3, "icarus3", false);
            Scribe_Values.Look(ref icarusX, "icarusX", false);
            Scribe_Values.Look(ref laelaps1, "laelaps1", false);
            Scribe_Values.Look(ref laelaps2, "laelaps2", false);
            Scribe_Values.Look(ref laelaps3, "laelaps3", false);
            Scribe_Values.Look(ref laelapsX, "laelapsX", false);
            Scribe_Values.Look(ref odyssey1, "odyssey1", false);
            Scribe_Values.Look(ref odyssey2, "odyssey2", false);
            Scribe_Values.Look(ref odyssey3, "odyssey3", false);
            Scribe_Values.Look(ref odysseyX, "odysseyX", false);
            Scribe_Values.Look(ref theseus1, "theseus1", false);
            Scribe_Values.Look(ref theseus2, "theseus2", false);
            Scribe_Values.Look(ref theseus3, "theseus3", false);
            Scribe_Values.Look(ref theseusX, "theseusX", false);
            Scribe_Values.Look(ref ethnog1, "ethnog1", false);
            Scribe_Values.Look(ref ethnog2, "ethnog2", false);
            Scribe_Values.Look(ref ethnog3, "ethnog3", false);
            Scribe_Values.Look(ref ethnogX, "ethnogX", false);
            Scribe_Values.Look(ref remnant1, "remnant1", false);
            Scribe_Values.Look(ref remnant2, "remnant2", false);
            Scribe_Values.Look(ref remnant3, "remnant3", false);
            Scribe_Values.Look(ref remnantX, "remnantX", false);
            Scribe_Values.Look(ref replica1, "replica1", false);
            Scribe_Values.Look(ref replica2, "replica2", false);
            Scribe_Values.Look(ref replica3, "replica3", false);
            Scribe_Values.Look(ref replicaX, "replicaX", false);
            Scribe_Values.Look(ref sat1, "sat1", false);
            Scribe_Values.Look(ref sat2, "sat2", false);
            Scribe_Values.Look(ref sat3, "sat3", false);
            Scribe_Values.Look(ref satX, "satX", false);
            Scribe_Values.Look(ref shrine1, "shrine1", false);
            Scribe_Values.Look(ref shrine2, "shrine2", false);
            Scribe_Values.Look(ref shrine3, "shrine3", false);
            Scribe_Values.Look(ref shrineX, "shrineX", false);
            Scribe_Values.Look(ref cs1, "cs1", false);
            Scribe_Values.Look(ref cs2, "cs2", false);
            Scribe_Values.Look(ref cs3, "cs3", false);
            Scribe_Values.Look(ref csX, "csX", false);
            Scribe_Values.Look(ref ec1, "ec1", false);
            Scribe_Values.Look(ref ec2, "ec2", false);
            Scribe_Values.Look(ref ec3, "ec3", false);
            Scribe_Values.Look(ref ecX, "ecX", false);
            Scribe_Values.Look(ref fw1, "fw1", false);
            Scribe_Values.Look(ref fw2, "fw2", false);
            Scribe_Values.Look(ref fw3, "fw3", false);
            Scribe_Values.Look(ref fwX, "fwX", false);
            Scribe_Values.Look(ref hd1, "hd1", false);
            Scribe_Values.Look(ref hd2, "hd2", false);
            Scribe_Values.Look(ref hd3, "hd3", false);
            Scribe_Values.Look(ref hdX, "hdX", false);
            Scribe_Values.Look(ref ra1, "ra1", false);
            Scribe_Values.Look(ref ra2, "ra2", false);
            Scribe_Values.Look(ref ra3, "ra3", false);
            Scribe_Values.Look(ref raX, "raX", false);
            Scribe_Values.Look(ref barker1, "barker1", false);
            Scribe_Values.Look(ref barker2, "barker2", false);
            Scribe_Values.Look(ref barker3, "barker3", false);
            Scribe_Values.Look(ref barkerX, "barkerX", false);
            Scribe_Values.Look(ref lc1, "lc1", false);
            Scribe_Values.Look(ref lc2, "lc2", false);
            Scribe_Values.Look(ref lc3, "lc3", false);
            Scribe_Values.Look(ref lcX, "lcX", false);
            Scribe_Values.Look(ref natali1, "natali1", false);
            Scribe_Values.Look(ref natali2, "natali2", false);
            Scribe_Values.Look(ref natali3, "natali3", false);
            Scribe_Values.Look(ref nataliX, "nataliX", false);
            Scribe_Values.Look(ref romero1, "romero1", false);
            Scribe_Values.Look(ref romero2, "romero2", false);
            Scribe_Values.Look(ref romero3, "romero3", false);
            Scribe_Values.Look(ref romeroX, "romeroX", false);
            Scribe_Values.Look(ref wells1, "wells1", false);
            Scribe_Values.Look(ref wells2, "wells2", false);
            Scribe_Values.Look(ref wells3, "wells3", false);
            Scribe_Values.Look(ref wellsX, "wellsX", false);
            base.ExposeData();
        }
    }
    public class HVMP_Mod : Mod
    {
        public HVMP_Mod(ModContentPack content) : base(content)
        {
            HVMP_Mod.settings = GetSettings<HVMP_Settings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect rect = new Rect(inRect)
            {
                y = inRect.y + 40f
            };
            Rect rect2 = rect;
            rect = new Rect(inRect)
            {
                height = inRect.height - 40f,
                y = inRect.y + 40f
            };
            Rect rect3 = rect;
            Widgets.DrawMenuSection(rect3);
            List<TabRecord> list = new List<TabRecord>
            {
                //why force an extra translation key? lol
				new TabRecord("VEF_GeneralTitle".Translate(), delegate
                {
                    this.PageIndex = 0;
                    this.WriteSettings();
                }, this.PageIndex == 0),
                new TabRecord("HVMP_Label_MutatorSettings".Translate(), delegate
                {
                    this.PageIndex = 1;
                    this.WriteSettings();
                }, this.PageIndex == 1)
            };
            if (ModsConfig.OdysseyActive)
            {
                list.Add(
                new TabRecord("HVMP_Label_PlatformSettings".Translate(), delegate
                {
                    this.PageIndex = 2;
                    this.WriteSettings();
                }, this.PageIndex == 2));
            }
            TabDrawer.DrawTabs<TabRecord>(rect2, list, 200f);
            switch (this.PageIndex)
            {
                case 0:
                    this.MainSettings(rect3.ContractedBy(15f));
                    return;
                case 1:
                    this.MutatorSettings(rect3.ContractedBy(15f));
                    return;
                case 2:
                    if (ModsConfig.OdysseyActive)
                    {
                        this.BranchPlatformSettings(rect3.ContractedBy(15f));
                    }
                    else
                    {
                        this.MainSettings(rect3.ContractedBy(15f));
                    }
                    return;
                default:
                    base.DoSettingsWindowContents(inRect);
                    return;
            }
        }
        private void MainSettings(Rect inRect)
        {
            Rect rect = new Rect(inRect.xMin, inRect.yMin, inRect.width, inRect.height);
            Rect rect2 = new Rect(inRect.xMin, inRect.yMin, inRect.width * 0.96f, 4500f);
            //toggles chaos mode on or off: off by default, but if on every branch will offer quests on their own timer, leading to A LOT more quests
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(rect2);
            listingStandard.CheckboxLabeled("HVMP_SettingSeniorityScalingPermits".Translate(), ref settings.permitsScaleBySeniority, "HVMP_TooltipSeniorityScalingPermits".Translate());
            listingStandard.CheckboxLabeled("HVMP_SettingChaosMode".Translate(), ref settings.maximumChaosMode, "HVMP_TooltipChaosMode".Translate());
            if (ModsConfig.AnomalyActive)
            {
                listingStandard.CheckboxLabeled("HVMP_SettingAnomalyActivityLevel".Translate(), ref settings.occultCanAlwaysBeEncountered, "HVMP_TooltipAnomalyActivityLevel".Translate());
            }
            listingStandard.End();
            //time in between branch quests
            displayMin = ((int)settings.minBranchQuestInterval).ToString();
            displayMax = ((int)settings.maxBranchQuestInterval).ToString();
            float x = rect.xMin, y = rect.yMin + 90, halfWidth = rect2.width * 0.5f;
            float orig = settings.minBranchQuestInterval;
            Rect questDaysMinRect = new Rect(x + 10, y, halfWidth - 15, 32);
            settings.minBranchQuestInterval = Widgets.HorizontalSlider(questDaysMinRect, settings.minBranchQuestInterval, 1f, 60f, true, "HVMP_SettingMinDays".Translate(), "1", "60", 1f);
            TooltipHandler.TipRegion(questDaysMinRect.LeftPart(1f), "HVMP_TooltipMinDays".Translate());
            if (orig != settings.minBranchQuestInterval)
            {
                displayMin = ((int)settings.minBranchQuestInterval).ToString();
            }
            y += 32;
            string origString = displayMin;
            displayMin = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayMin);
            if (!displayMin.Equals(origString))
            {
                this.ParseInput(displayMin, settings.minBranchQuestInterval, out settings.minBranchQuestInterval, 1f, 60f);
            }
            if (settings.minBranchQuestInterval > settings.maxBranchQuestInterval)
            {
                settings.maxBranchQuestInterval = settings.minBranchQuestInterval;
                displayMax = ((int)settings.maxBranchQuestInterval).ToString();
            }
            y -= 32;
            orig = settings.maxBranchQuestInterval;
            Rect questDaysMaxRect = new Rect(x + 5 + halfWidth, y, halfWidth - 15, 32);
            settings.maxBranchQuestInterval = Widgets.HorizontalSlider(questDaysMaxRect, settings.maxBranchQuestInterval, 1f, 60f, true, "HVMP_SettingMaxDays".Translate(), "1", "60", 1f);
            TooltipHandler.TipRegion(questDaysMaxRect.LeftPart(1f), "HVMP_TooltipMaxDays".Translate());
            if (orig != settings.maxBranchQuestInterval)
            {
                displayMax = ((int)settings.maxBranchQuestInterval).ToString();
            }
            y += 32;
            origString = displayMax;
            displayMax = Widgets.TextField(new Rect(x + 5 + halfWidth, y, 50, 32), displayMax);
            if (!displayMax.Equals(origString))
            {
                this.ParseInput(displayMax, settings.maxBranchQuestInterval, out settings.maxBranchQuestInterval, 1f, 60f);
            }
            if (settings.maxBranchQuestInterval < settings.minBranchQuestInterval)
            {
                settings.minBranchQuestInterval = settings.maxBranchQuestInterval;
                displayMin = ((int)settings.minBranchQuestInterval).ToString();
            }
            //set how rewarding you want quests to be
            y += 50;
            displayQuestRewardFactor = (settings.questRewardFactor).ToStringByStyle(ToStringStyle.FloatOne);
            float origR = settings.questRewardFactor;
            Rect rewardFactorRect = new Rect(x + 10, y, halfWidth - 15, 32);
            settings.questRewardFactor = Widgets.HorizontalSlider(rewardFactorRect, settings.questRewardFactor, 0.2f, 2f, true, "HVMP_SettingRewardFactor".Translate(), "low", "high", 0.1f);
            TooltipHandler.TipRegion(rewardFactorRect.LeftPart(1f), "HVMP_TooltipRewardFactor".Translate());
            if (origR != settings.questRewardFactor)
            {
                displayQuestRewardFactor = ((int)settings.questRewardFactor).ToString() + "x";
            }
            y += 32;
            string origStringR = displayQuestRewardFactor;
            displayQuestRewardFactor = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayQuestRewardFactor);
            if (!displayQuestRewardFactor.Equals(origStringR))
            {
                this.ParseInput(displayQuestRewardFactor, settings.questRewardFactor, out settings.questRewardFactor, 0.2f, 2f);
            }
            //set whether you should be punished for failing or foregoing a quest, and if so, when that should start happening
            y += 50;
            Rect bratBehaviorRect = new Rect(halfWidth * 0.05f, y, halfWidth * 0.8f, 32);
            this.ExpectationLevelSelector(bratBehaviorRect);
            bb1 = this.ExpectationLabel;
            TooltipHandler.TipRegion(bratBehaviorRect.LeftPart(1f), "HVMP_TooltipExpectationLevel".Translate());
            Rect bratBehaviorRect2 = new Rect(halfWidth * 0.95f, y, halfWidth * 0.8f, 32);
            bb2 = this.SeniorityLabel;
            TooltipHandler.TipRegion(bratBehaviorRect2.LeftPart(1f), "HVMP_TooltipSeniorityLevel".Translate());
            this.SeniorityLevelSelector(bratBehaviorRect2);
            //set how punishing failing or foregoing a quest should be
            y += 50;
            if (settings.bratBehaviorMinSeniorityLvl < 999 || settings.bratBehaviorMinExpectationLvl < 999)
            {
                refusalLoss = ((int)settings.goodwillQuestRefusalLoss).ToString();
                failureLoss = ((int)settings.goodwillQuestFailureLoss).ToString();
                float orig2 = settings.goodwillQuestRefusalLoss;
                Rect refusalGwRect = new Rect(x + 10, y, halfWidth - 15, 32);
                settings.goodwillQuestRefusalLoss = (int)Widgets.HorizontalSlider(refusalGwRect, settings.goodwillQuestRefusalLoss, 0, 100, true, "HVMP_SettingGoodwillRefusal".Translate(), "0", "100", 1);
                TooltipHandler.TipRegion(refusalGwRect.LeftPart(1f), "HVMP_TooltipGoodwillRefusal".Translate());
                if (orig2 != settings.goodwillQuestRefusalLoss)
                {
                    refusalLoss = ((int)settings.goodwillQuestRefusalLoss).ToString();
                }
                y += 32;
                string origString2 = refusalLoss;
                refusalLoss = Widgets.TextField(new Rect(x + 10, y, 50, 32), refusalLoss);
                if (!refusalLoss.Equals(origString2))
                {
                    this.ParseInput(refusalLoss, settings.goodwillQuestRefusalLoss, out settings.goodwillQuestRefusalLoss, 0, 120);
                }
                y -= 32;
                orig = settings.goodwillQuestFailureLoss;
                Rect failureGwRect = new Rect(x + 5 + halfWidth, y, halfWidth - 15, 32);
                settings.goodwillQuestFailureLoss = (int)Widgets.HorizontalSlider(failureGwRect, settings.goodwillQuestFailureLoss, 0, 100, true, "HVMP_SettingGoodwillFailure".Translate(), "0", "100", 1);
                TooltipHandler.TipRegion(failureGwRect.LeftPart(1f), "HVMP_TooltipGoodwillFailure".Translate());
                if (orig != settings.goodwillQuestFailureLoss)
                {
                    failureLoss = ((int)settings.goodwillQuestFailureLoss).ToString();
                }
                y += 32;
                origString2 = failureLoss;
                failureLoss = Widgets.TextField(new Rect(x + 5 + halfWidth, y, 50, 32), failureLoss);
                if (!failureLoss.Equals(origString2))
                {
                    this.ParseInput(failureLoss, settings.goodwillQuestFailureLoss, out settings.goodwillQuestFailureLoss, 0, 120);
                }
            }
            base.DoSettingsWindowContents(inRect);
        }
        private void MutatorSettings(Rect inRect)
        {
            Rect rect = new Rect(inRect.xMin, inRect.yMin, inRect.width, inRect.height);
            Rect rect2 = new Rect(inRect.xMin, inRect.yMin, inRect.width * 0.96f, 4500f);
            Widgets.BeginScrollView(rect, ref this.scrollPosition, rect2, true);
            float x = rect.xMin, y = rect.yMin, halfWidth = rect2.width * 0.5f;
            Rect rect3 = new Rect(inRect.xMin, y, (inRect.width - 30f) * 0.8f, 4000f);
            Listing_Standard listing_Standard2 = new Listing_Standard();
            listing_Standard2.Begin(rect3);
            Text.Font = GameFont.Medium;
            listing_Standard2.Label("HVMP_Label_MutatorSettings".Translate(), 30f);
            //listing_Standard2.Gap(30f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Tooltip_QuestMutators".Translate());
            listing_Standard2.Gap(20f);
            listing_Standard2.Label("HVMP_Label_Commerce".Translate());
            listing_Standard2.Label("HVMP_Label_Fortification".Translate(), -1, "HVMP_Tooltip_Fortification".Translate());
            Text.Font = GameFont.Tiny;
            bool flagCA1 = settings.fort1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Fortification".Translate() + ": " + "HVMP_HardMode_Label_Fortification_1".Translate(), ref flagCA1, "HVMP_HardMode_Tooltip_Fortification_1".Translate());
            settings.fort1 = flagCA1;
            bool flagCA2 = settings.fort2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Fortification".Translate() + ": " + "HVMP_HardMode_Label_Fortification_2".Translate(), ref flagCA2, "HVMP_HardMode_Tooltip_Fortification_2".Translate());
            settings.fort2 = flagCA2;
            bool flagCA3 = settings.fort3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Fortification".Translate() + ": " + "HVMP_HardMode_Label_Fortification_3".Translate(), ref flagCA3, "HVMP_HardMode_Tooltip_Fortification_3".Translate());
            settings.fort3 = flagCA3;
            bool flagCAX = settings.fortX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagCAX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.fortX = flagCAX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Intervention".Translate(), -1, "HVMP_Tooltip_Intervention".Translate());
            Text.Font = GameFont.Tiny;
            bool flagCB1 = settings.interv1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Intervention".Translate() + ": " + "HVMP_HardMode_Label_Intervention_1".Translate(), ref flagCB1, "HVMP_HardMode_Tooltip_Intervention_1".Translate());
            settings.interv1 = flagCB1;
            bool flagCB2 = settings.interv2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Intervention".Translate() + ": " + "HVMP_HardMode_Label_Intervention_2".Translate(), ref flagCB2, "HVMP_HardMode_Tooltip_Intervention_2".Translate());
            settings.interv2 = flagCB2;
            bool flagCB3 = settings.interv3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Intervention".Translate() + ": " + "HVMP_HardMode_Label_Intervention_3".Translate(), ref flagCB3, "HVMP_HardMode_Tooltip_Intervention_3".Translate());
            settings.interv3 = flagCB3;
            bool flagCBX = settings.intervX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagCBX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.intervX = flagCBX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Mastermind".Translate(), -1, "HVMP_Tooltip_Mastermind".Translate());
            Text.Font = GameFont.Tiny;
            bool flagCC1 = settings.mm1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Mastermind".Translate() + ": " + "HVMP_HardMode_Label_Mastermind_1".Translate(), ref flagCC1, "HVMP_HardMode_Tooltip_Mastermind_1".Translate());
            settings.mm1 = flagCC1;
            bool flagCC2 = settings.mm2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Mastermind".Translate() + ": " + "HVMP_HardMode_Label_Mastermind_2".Translate(), ref flagCC2, "HVMP_HardMode_Tooltip_Mastermind_2".Translate());
            settings.mm2 = flagCC2;
            bool flagCC3 = settings.mm3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Mastermind".Translate() + ": " + "HVMP_HardMode_Label_Mastermind_3".Translate(), ref flagCC3, "HVMP_HardMode_Tooltip_Mastermind_3".Translate());
            settings.mm3 = flagCC3;
            bool flagCCX = settings.mmX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagCCX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.mmX = flagCCX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Research".Translate(), -1, "HVMP_Tooltip_Research".Translate());
            Text.Font = GameFont.Tiny;
            bool flagCD1 = settings.research1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Research".Translate() + ": " + "HVMP_HardMode_Label_Research_1".Translate(), ref flagCD1, "HVMP_HardMode_Tooltip_Research_1".Translate());
            settings.research1 = flagCD1;
            bool flagCD2 = settings.research2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Research".Translate() + ": " + "HVMP_HardMode_Label_Research_2".Translate(), ref flagCD2, "HVMP_HardMode_Tooltip_Research_2".Translate());
            settings.research2 = flagCD2;
            bool flagCD3 = settings.research3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Research".Translate() + ": " + "HVMP_HardMode_Label_Research_3".Translate(), ref flagCD3, "HVMP_HardMode_Tooltip_Research_3".Translate());
            settings.research3 = flagCD3;
            bool flagCDX = settings.researchX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagCDX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.researchX = flagCDX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Transportation".Translate(), -1, "HVMP_Tooltip_Transportation".Translate());
            Text.Font = GameFont.Tiny;
            bool flagCE1 = settings.transport1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Transportation".Translate() + ": " + "HVMP_HardMode_Label_Transportation_1".Translate(), ref flagCE1, "HVMP_HardMode_Tooltip_Transportation_1".Translate());
            settings.transport1 = flagCE1;
            bool flagCE2 = settings.transport2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Transportation".Translate() + ": " + "HVMP_HardMode_Label_Transportation_2".Translate(), ref flagCE2, "HVMP_HardMode_Tooltip_Transportation_2".Translate());
            settings.transport2 = flagCE2;
            bool flagCE3 = settings.transport3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Transportation".Translate() + ": " + "HVMP_HardMode_Label_Transportation_3".Translate(), ref flagCE3, "HVMP_HardMode_Tooltip_Transportation_3".Translate());
            settings.transport3 = flagCE3;
            bool flagCEX = settings.transportX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagCEX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.transportX = flagCEX;
            listing_Standard2.Gap(20f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Pax".Translate());
            listing_Standard2.Label("HVMP_Label_Bellum".Translate(), -1, "HVMP_Tooltip_Bellum".Translate());
            Text.Font = GameFont.Tiny;
            bool flagPA1 = settings.bellum1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Bellum".Translate() + ": " + "HVMP_HardMode_Label_Bellum_1".Translate(), ref flagPA1, "HVMP_HardMode_Tooltip_Bellum_1".Translate());
            settings.bellum1 = flagPA1;
            bool flagPA2 = settings.bellum2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Bellum".Translate() + ": " + "HVMP_HardMode_Label_Bellum_2".Translate(), ref flagPA2, "HVMP_HardMode_Tooltip_Bellum_2".Translate());
            settings.bellum2 = flagPA2;
            bool flagPA3 = settings.bellum3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Bellum".Translate() + ": " + "HVMP_HardMode_Label_Bellum_3".Translate(), ref flagPA3, "HVMP_HardMode_Tooltip_Bellum_3".Translate());
            settings.bellum3 = flagPA3;
            bool flagPAX = settings.bellumX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagPAX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.bellumX = flagPAX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Caelum".Translate(), -1, "HVMP_Tooltip_Caelum".Translate());
            Text.Font = GameFont.Tiny;
            bool flagPB1 = settings.caelum1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Caelum".Translate() + ": " + "HVMP_HardMode_Label_Caelum_1".Translate(), ref flagPB1, "HVMP_HardMode_Tooltip_Caelum_1".Translate());
            settings.caelum1 = flagPB1;
            bool flagPB2 = settings.caelum2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Caelum".Translate() + ": " + "HVMP_HardMode_Label_Caelum_2".Translate(), ref flagPB2, "HVMP_HardMode_Tooltip_Caelum_2".Translate());
            settings.caelum2 = flagPB2;
            bool flagPB3 = settings.caelum3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Caelum".Translate() + ": " + "HVMP_HardMode_Label_Caelum_3".Translate(), ref flagPB3, "HVMP_HardMode_Tooltip_Caelum_3".Translate());
            settings.caelum3 = flagPB3;
            bool flagPBX = settings.caelumX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagPBX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.caelumX = flagPBX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Machina".Translate(), -1, "HVMP_Tooltip_Machina".Translate());
            Text.Font = GameFont.Tiny;
            bool flagPC1 = settings.machina1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Machina".Translate() + ": " + "HVMP_HardMode_Label_Machina_1".Translate(), ref flagPC1, "HVMP_HardMode_Tooltip_Machina_1".Translate());
            settings.machina1 = flagPC1;
            bool flagPC2 = settings.machina2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Machina".Translate() + ": " + "HVMP_HardMode_Label_Machina_2".Translate(), ref flagPC2, "HVMP_HardMode_Tooltip_Machina_2".Translate());
            settings.machina2 = flagPC2;
            bool flagPC3 = settings.machina3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Machina".Translate() + ": " + "HVMP_HardMode_Label_Machina_3".Translate(), ref flagPC3, "HVMP_HardMode_Tooltip_Machina_3".Translate());
            settings.machina3 = flagPC3;
            bool flagPCX = settings.machinaX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagPCX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.machinaX = flagPCX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Mundi".Translate(), -1, "HVMP_Tooltip_Mundi".Translate());
            Text.Font = GameFont.Tiny;
            bool flagPD1 = settings.mundi1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Mundi".Translate() + ": " + "HVMP_HardMode_Label_Mundi_1".Translate(), ref flagPD1, "HVMP_HardMode_Tooltip_Mundi_1".Translate());
            settings.mundi1 = flagPD1;
            bool flagPD2 = settings.mundi2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Mundi".Translate() + ": " + "HVMP_HardMode_Label_Mundi_2".Translate(), ref flagPD2, "HVMP_HardMode_Tooltip_Mundi_2".Translate());
            settings.mundi2 = flagPD2;
            bool flagPD3 = settings.mundi3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Mundi".Translate() + ": " + "HVMP_HardMode_Label_Mundi_3".Translate(), ref flagPD3, "HVMP_HardMode_Tooltip_Mundi_3".Translate());
            settings.mundi3 = flagPD3;
            bool flagPDX = settings.mundiX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagPDX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.mundiX = flagPDX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Vox".Translate(), -1, "HVMP_Tooltip_Vox".Translate());
            Text.Font = GameFont.Tiny;
            bool flagPE1 = settings.vox1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Vox".Translate() + ": " + "HVMP_HardMode_Label_Vox_1".Translate(), ref flagPE1, "HVMP_HardMode_Tooltip_Vox_1".Translate());
            settings.vox1 = flagPE1;
            bool flagPE2 = settings.vox2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Vox".Translate() + ": " + "HVMP_HardMode_Label_Vox_2".Translate(), ref flagPE2, "HVMP_HardMode_Tooltip_Vox_2".Translate());
            settings.vox2 = flagPE2;
            bool flagPE3 = settings.vox3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Vox".Translate() + ": " + "HVMP_HardMode_Label_Vox_3".Translate(), ref flagPE3, "HVMP_HardMode_Tooltip_Vox_3".Translate());
            settings.vox3 = flagPE3;
            bool flagPEX = settings.voxX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagPEX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.voxX = flagPEX;
            listing_Standard2.Gap(20f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Rover".Translate());
            listing_Standard2.Label("HVMP_Label_Atlas".Translate(), -1, "HVMP_Tooltip_Atlas".Translate());
            Text.Font = GameFont.Tiny;
            bool flagRA1 = settings.atlas1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Atlas".Translate() + ": " + "HVMP_HardMode_Label_Atlas_1".Translate(), ref flagRA1, "HVMP_HardMode_Tooltip_Atlas_1".Translate());
            settings.atlas1 = flagRA1;
            bool flagRA2 = settings.atlas2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Atlas".Translate() + ": " + "HVMP_HardMode_Label_Atlas_2".Translate(), ref flagRA2, "HVMP_HardMode_Tooltip_Atlas_2".Translate());
            settings.atlas2 = flagRA2;
            bool flagRA3 = settings.atlas3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Atlas".Translate() + ": " + "HVMP_HardMode_Label_Atlas_3".Translate(), ref flagRA3, "HVMP_HardMode_Tooltip_Atlas_3".Translate());
            settings.atlas3 = flagRA3;
            bool flagRAX = settings.atlasX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagRAX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.atlasX = flagRAX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Icarus".Translate(), -1, "HVMP_Tooltip_Icarus".Translate());
            Text.Font = GameFont.Tiny;
            bool flagRB1 = settings.icarus1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Icarus".Translate() + ": " + "HVMP_HardMode_Label_Icarus_1".Translate(), ref flagRB1, "HVMP_HardMode_Tooltip_Icarus_1".Translate());
            settings.icarus1 = flagRB1;
            bool flagRB2 = settings.icarus2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Icarus".Translate() + ": " + "HVMP_HardMode_Label_Icarus_2".Translate(), ref flagRB2, "HVMP_HardMode_Tooltip_Icarus_2".Translate());
            settings.icarus2 = flagRB2;
            bool flagRB3 = settings.icarus3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Icarus".Translate() + ": " + "HVMP_HardMode_Label_Icarus_3".Translate(), ref flagRB3, "HVMP_HardMode_Tooltip_Icarus_3".Translate());
            settings.icarus3 = flagRB3;
            bool flagRBX = settings.icarusX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagRBX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.icarusX = flagRBX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Laelaps".Translate(), -1, "HVMP_Tooltip_Laelaps".Translate());
            Text.Font = GameFont.Tiny;
            bool flagRC1 = settings.laelaps1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Laelaps".Translate() + ": " + "HVMP_HardMode_Label_Laelaps_1".Translate(), ref flagRC1, "HVMP_HardMode_Tooltip_Laelaps_1".Translate());
            settings.laelaps1 = flagRC1;
            bool flagRC2 = settings.laelaps2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Laelaps".Translate() + ": " + "HVMP_HardMode_Label_Laelaps_2".Translate(), ref flagRC2, "HVMP_HardMode_Tooltip_Laelaps_2".Translate());
            settings.laelaps2 = flagRC2;
            bool flagRC3 = settings.laelaps3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Laelaps".Translate() + ": " + "HVMP_HardMode_Label_Laelaps_3".Translate(), ref flagRC3, "HVMP_HardMode_Tooltip_Laelaps_3".Translate());
            settings.laelaps3 = flagRC3;
            bool flagRCX = settings.laelapsX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagRCX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.laelapsX = flagRCX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Odyssey".Translate(), -1, "HVMP_Tooltip_Odyssey".Translate());
            Text.Font = GameFont.Tiny;
            bool flagRD1 = settings.odyssey1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Odyssey".Translate() + ": " + "HVMP_HardMode_Label_Odyssey_1".Translate(), ref flagRD1, "HVMP_HardMode_Tooltip_Odyssey_1".Translate());
            settings.odyssey1 = flagRD1;
            bool flagRD2 = settings.odyssey2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Odyssey".Translate() + ": " + "HVMP_HardMode_Label_Odyssey_2".Translate(), ref flagRD2, "HVMP_HardMode_Tooltip_Odyssey_2".Translate());
            settings.odyssey2 = flagRD2;
            bool flagRD3 = settings.odyssey3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Odyssey".Translate() + ": " + "HVMP_HardMode_Label_Odyssey_3".Translate(), ref flagRD3, "HVMP_HardMode_Tooltip_Odyssey_3".Translate());
            settings.odyssey3 = flagRD3;
            bool flagRDX = settings.odysseyX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagRDX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.odysseyX = flagRDX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Theseus".Translate(), -1, "HVMP_Tooltip_Theseus".Translate());
            Text.Font = GameFont.Tiny;
            bool flagRE1 = settings.theseus1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Theseus".Translate() + ": " + "HVMP_HardMode_Label_Theseus_1".Translate(), ref flagRE1, "HVMP_HardMode_Tooltip_Theseus_1".Translate());
            settings.theseus1 = flagRE1;
            bool flagRE2 = settings.theseus2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Theseus".Translate() + ": " + "HVMP_HardMode_Label_Theseus_2".Translate(), ref flagRE2, "HVMP_HardMode_Tooltip_Theseus_2".Translate());
            settings.theseus2 = flagRE2;
            bool flagRE3 = settings.theseus3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Theseus".Translate() + ": " + "HVMP_HardMode_Label_Theseus_3".Translate(), ref flagRE3, "HVMP_HardMode_Tooltip_Theseus_3".Translate());
            settings.theseus3 = flagRE3;
            bool flagREX = settings.theseusX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagREX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.theseusX = flagREX;
            if (ModsConfig.IdeologyActive)
            {
                listing_Standard2.Gap(20f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Archive".Translate());
                listing_Standard2.Label("HVMP_Label_Ethnography".Translate(), -1, "HVMP_Tooltip_Ethnography".Translate());
                Text.Font = GameFont.Tiny;
                bool flagAA1 = settings.ethnog1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Ethnography".Translate() + ": " + "HVMP_HardMode_Label_Ethnography_1".Translate(), ref flagAA1, "HVMP_HardMode_Tooltip_Ethnography_1".Translate());
                settings.ethnog1 = flagAA1;
                bool flagAA2 = settings.ethnog2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Ethnography".Translate() + ": " + "HVMP_HardMode_Label_Ethnography_2".Translate(), ref flagAA2, "HVMP_HardMode_Tooltip_Ethnography_2".Translate());
                settings.ethnog2 = flagAA2;
                bool flagAA3 = settings.ethnog3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Ethnography".Translate() + ": " + "HVMP_HardMode_Label_Ethnography_3".Translate(), ref flagAA3, "HVMP_HardMode_Tooltip_Ethnography_3".Translate());
                settings.ethnog3 = flagAA3;
                bool flagAAX = settings.ethnogX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagAAX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.ethnogX = flagAAX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Remnant".Translate(), -1, "HVMP_Tooltip_Remnant".Translate());
                Text.Font = GameFont.Tiny;
                bool flagAB1 = settings.remnant1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Remnant".Translate() + ": " + "HVMP_HardMode_Label_Remnant_1".Translate(), ref flagAB1, "HVMP_HardMode_Tooltip_Remnant_1".Translate());
                settings.remnant1 = flagAB1;
                bool flagAB2 = settings.remnant2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Remnant".Translate() + ": " + "HVMP_HardMode_Label_Remnant_2".Translate(), ref flagAB2, "HVMP_HardMode_Tooltip_Remnant_2".Translate());
                settings.remnant2 = flagAB2;
                bool flagAB3 = settings.remnant3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Remnant".Translate() + ": " + "HVMP_HardMode_Label_Remnant_3".Translate(), ref flagAB3, "HVMP_HardMode_Tooltip_Remnant_3".Translate());
                settings.remnant3 = flagAB3;
                bool flagABX = settings.remnantX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagABX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.remnantX = flagABX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Replica".Translate(), -1, "HVMP_Tooltip_Replica".Translate());
                Text.Font = GameFont.Tiny;
                bool flagAC1 = settings.replica1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Replica".Translate() + ": " + "HVMP_HardMode_Label_Replica_1".Translate(), ref flagAC1, "HVMP_HardMode_Tooltip_Replica_1".Translate());
                settings.replica1 = flagAC1;
                bool flagAC2 = settings.replica2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Replica".Translate() + ": " + "HVMP_HardMode_Label_Replica_2".Translate(), ref flagAC2, "HVMP_HardMode_Tooltip_Replica_2".Translate());
                settings.replica2 = flagAC2;
                bool flagAC3 = settings.replica3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Replica".Translate() + ": " + "HVMP_HardMode_Label_Replica_3".Translate(), ref flagAC3, "HVMP_HardMode_Tooltip_Replica_3".Translate());
                settings.replica3 = flagAC3;
                bool flagACX = settings.replicaX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagACX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.replicaX = flagACX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Satellite".Translate(), -1, "HVMP_Tooltip_Satellite".Translate());
                Text.Font = GameFont.Tiny;
                bool flagAD1 = settings.sat1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Satellite".Translate() + ": " + "HVMP_HardMode_Label_Satellite_1".Translate(), ref flagAD1, "HVMP_HardMode_Tooltip_Satellite_1".Translate());
                settings.sat1 = flagAD1;
                bool flagAD2 = settings.sat2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Satellite".Translate() + ": " + "HVMP_HardMode_Label_Satellite_2".Translate(), ref flagAD2, "HVMP_HardMode_Tooltip_Satellite_2".Translate());
                settings.sat2 = flagAD2;
                bool flagAD3 = settings.sat3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Satellite".Translate() + ": " + "HVMP_HardMode_Label_Satellite_3".Translate(), ref flagAD3, "HVMP_HardMode_Tooltip_Satellite_3".Translate());
                settings.sat3 = flagAD3;
                bool flagADX = settings.satX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagADX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.satX = flagADX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Shrine".Translate(), -1, "HVMP_Tooltip_Shrine".Translate());
                Text.Font = GameFont.Tiny;
                bool flagAE1 = settings.shrine1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Shrine".Translate() + ": " + "HVMP_HardMode_Label_Shrine_1".Translate(), ref flagAE1, "HVMP_HardMode_Tooltip_Shrine_1".Translate());
                settings.shrine1 = flagAE1;
                bool flagAE2 = settings.shrine2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Shrine".Translate() + ": " + "HVMP_HardMode_Label_Shrine_2".Translate(), ref flagAE2, "HVMP_HardMode_Tooltip_Shrine_2".Translate());
                settings.shrine2 = flagAE2;
                bool flagAE3 = settings.shrine3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Shrine".Translate() + ": " + "HVMP_HardMode_Label_Shrine_3".Translate(), ref flagAE3, "HVMP_HardMode_Tooltip_Shrine_3".Translate());
                settings.shrine3 = flagAE3;
                bool flagAEX = settings.shrineX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagAEX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.shrineX = flagAEX;
            }
            if (ModsConfig.BiotechActive)
            {
                listing_Standard2.Gap(20f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Ecosphere".Translate());
                listing_Standard2.Label("HVMP_Label_CaseStudy".Translate(), -1, "HVMP_Tooltip_CaseStudy".Translate());
                Text.Font = GameFont.Tiny;
                bool flagEA1 = settings.cs1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_CaseStudy".Translate() + ": " + "HVMP_HardMode_Label_CaseStudy_1".Translate(), ref flagEA1, "HVMP_HardMode_Tooltip_CaseStudy_1".Translate());
                settings.cs1 = flagEA1;
                bool flagEA2 = settings.cs2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_CaseStudy".Translate() + ": " + "HVMP_HardMode_Label_CaseStudy_2".Translate(), ref flagEA2, "HVMP_HardMode_Tooltip_CaseStudy_2".Translate());
                settings.cs2 = flagEA2;
                bool flagEA3 = settings.cs3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_CaseStudy".Translate() + ": " + "HVMP_HardMode_Label_CaseStudy_3".Translate(), ref flagEA3, "HVMP_HardMode_Tooltip_CaseStudy_3".Translate());
                settings.cs3 = flagEA3;
                bool flagEAX = settings.csX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagEAX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.csX = flagEAX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_ECquest".Translate(), -1, "HVMP_Tooltip_ECquest".Translate());
                Text.Font = GameFont.Tiny;
                bool flagEB1 = settings.ec1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_ECquest".Translate() + ": " + "HVMP_HardMode_Label_ECquest_1".Translate(), ref flagEB1, "HVMP_HardMode_Tooltip_ECquest_1".Translate());
                settings.ec1 = flagEB1;
                bool flagEB2 = settings.ec2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_ECquest".Translate() + ": " + "HVMP_HardMode_Label_ECquest_2".Translate(), ref flagEB2, "HVMP_HardMode_Tooltip_ECquest_2".Translate());
                settings.ec2 = flagEB2;
                bool flagEB3 = settings.ec3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_ECquest".Translate() + ": " + "HVMP_HardMode_Label_ECquest_3".Translate(), ref flagEB3, "HVMP_HardMode_Tooltip_ECquest_3".Translate());
                settings.ec3 = flagEB3;
                bool flagEBX = settings.ecX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagEBX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.ecX = flagEBX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_FieldWork".Translate(), -1, "HVMP_Tooltip_FieldWork".Translate());
                Text.Font = GameFont.Tiny;
                bool flagEC1 = settings.fw1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_FieldWork".Translate() + ": " + "HVMP_HardMode_Label_FieldWork_1".Translate(), ref flagEC1, "HVMP_HardMode_Tooltip_FieldWork_1".Translate());
                settings.fw1 = flagEC1;
                bool flagEC2 = settings.fw2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_FieldWork".Translate() + ": " + "HVMP_HardMode_Label_FieldWork_2".Translate(), ref flagEC2, "HVMP_HardMode_Tooltip_FieldWork_2".Translate());
                settings.fw2 = flagEC2;
                bool flagEC3 = settings.fw3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_FieldWork".Translate() + ": " + "HVMP_HardMode_Label_FieldWork_3".Translate(), ref flagEC3, "HVMP_HardMode_Tooltip_FieldWork_3".Translate());
                settings.fw3 = flagEC3;
                bool flagECX = settings.fwX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagECX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.fwX = flagECX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_HazardDisposal".Translate(), -1, "HVMP_Tooltip_HazardDisposal".Translate());
                Text.Font = GameFont.Tiny;
                bool flagED1 = settings.hd1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_HazardDisposal".Translate() + ": " + "HVMP_HardMode_Label_HazardDisposal_1".Translate(), ref flagED1, "HVMP_HardMode_Tooltip_HazardDisposal_1".Translate());
                settings.hd1 = flagED1;
                bool flagED2 = settings.hd2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_HazardDisposal".Translate() + ": " + "HVMP_HardMode_Label_HazardDisposal_2".Translate(), ref flagED2, "HVMP_HardMode_Tooltip_HazardDisposal_2".Translate());
                settings.hd2 = flagED2;
                bool flagED3 = settings.hd3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_HazardDisposal".Translate() + ": " + "HVMP_HardMode_Label_HazardDisposal_3".Translate(), ref flagED3, "HVMP_HardMode_Tooltip_HazardDisposal_3".Translate());
                settings.hd3 = flagED3;
                bool flagEDX = settings.hdX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagEDX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.hdX = flagEDX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_RAquest".Translate(), -1, "HVMP_Tooltip_RAquest".Translate());
                Text.Font = GameFont.Tiny;
                bool flagEE1 = settings.ra1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_RAquest".Translate() + ": " + "HVMP_HardMode_Label_RAquest_1".Translate(), ref flagEE1, "HVMP_HardMode_Tooltip_RAquest_1".Translate());
                settings.ra1 = flagEE1;
                bool flagEE2 = settings.ra2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_RAquest".Translate() + ": " + "HVMP_HardMode_Label_RAquest_2".Translate(), ref flagEE2, "HVMP_HardMode_Tooltip_RAquest_2".Translate());
                settings.ra2 = flagEE2;
                bool flagEE3 = settings.ra3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_RAquest".Translate() + ": " + "HVMP_HardMode_Label_RAquest_3".Translate(), ref flagEE3, "HVMP_HardMode_Tooltip_RAquest_3".Translate());
                settings.ra3 = flagEE3;
                bool flagEEX = settings.raX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagEEX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.raX = flagEEX;
            }
            if (ModsConfig.AnomalyActive)
            {
                listing_Standard2.Gap(20f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Occult".Translate());
                listing_Standard2.Label("HVMP_Label_Barker".Translate(), -1, "HVMP_Tooltip_Barker".Translate());
                Text.Font = GameFont.Tiny;
                bool flagOA1 = settings.barker1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Barker".Translate() + ": " + "HVMP_HardMode_Label_Barker_1".Translate(), ref flagOA1, "HVMP_HardMode_Tooltip_Barker_1".Translate());
                settings.barker1 = flagOA1;
                bool flagOA2 = settings.barker2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Barker".Translate() + ": " + "HVMP_HardMode_Label_Barker_2".Translate(), ref flagOA2, "HVMP_HardMode_Tooltip_Barker_2".Translate());
                settings.barker2 = flagOA2;
                bool flagOA3 = settings.barker3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Barker".Translate() + ": " + "HVMP_HardMode_Label_Barker_3".Translate(), ref flagOA3, "HVMP_HardMode_Tooltip_Barker_3".Translate());
                settings.barker3 = flagOA3;
                bool flagOAX = settings.barkerX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagOAX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.barkerX = flagOAX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Lovecraft".Translate(), -1, "HVMP_Tooltip_Lovecraft".Translate());
                Text.Font = GameFont.Tiny;
                bool flagOB1 = settings.lc1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Lovecraft".Translate() + ": " + "HVMP_HardMode_Label_Lovecraft_1".Translate(), ref flagOB1, "HVMP_HardMode_Tooltip_Lovecraft_1".Translate());
                settings.lc1 = flagOB1;
                bool flagOB2 = settings.lc2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Lovecraft".Translate() + ": " + "HVMP_HardMode_Label_Lovecraft_2".Translate(), ref flagOB2, "HVMP_HardMode_Tooltip_Lovecraft_2".Translate());
                settings.lc2 = flagOB2;
                bool flagOB3 = settings.lc3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Lovecraft".Translate() + ": " + "HVMP_HardMode_Label_Lovecraft_3".Translate(), ref flagOB3, "HVMP_HardMode_Tooltip_Lovecraft_3".Translate());
                settings.lc3 = flagOB3;
                bool flagOBX = settings.lcX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagOBX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.lcX = flagOBX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Natali".Translate(), -1, "HVMP_Tooltip_Natali".Translate());
                Text.Font = GameFont.Tiny;
                bool flagOC1 = settings.natali1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Natali".Translate() + ": " + "HVMP_HardMode_Label_Natali_1".Translate(), ref flagOC1, "HVMP_HardMode_Tooltip_Natali_1".Translate());
                settings.natali1 = flagOC1;
                bool flagOC2 = settings.natali2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Natali".Translate() + ": " + "HVMP_HardMode_Label_Natali_2".Translate(), ref flagOC2, "HVMP_HardMode_Tooltip_Natali_2".Translate());
                settings.natali2 = flagOC2;
                bool flagOC3 = settings.natali3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Natali".Translate() + ": " + "HVMP_HardMode_Label_Natali_3".Translate(), ref flagOC3, "HVMP_HardMode_Tooltip_Natali_3".Translate());
                settings.natali3 = flagOC3;
                bool flagOCX = settings.nataliX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagOCX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.nataliX = flagOCX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Romero".Translate(), -1, "HVMP_Tooltip_Romero".Translate());
                Text.Font = GameFont.Tiny;
                bool flagOD1 = settings.romero1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Romero".Translate() + ": " + "HVMP_HardMode_Label_Romero_1".Translate(), ref flagOD1, "HVMP_HardMode_Tooltip_Romero_1".Translate());
                settings.romero1 = flagOD1;
                bool flagOD2 = settings.romero2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Romero".Translate() + ": " + "HVMP_HardMode_Label_Romero_2".Translate(), ref flagOD2, "HVMP_HardMode_Tooltip_Romero_2".Translate());
                settings.romero2 = flagOD2;
                bool flagOD3 = settings.romero3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Romero".Translate() + ": " + "HVMP_HardMode_Label_Romero_3".Translate(), ref flagOD3, "HVMP_HardMode_Tooltip_Romero_3".Translate());
                settings.romero3 = flagOD3;
                bool flagODX = settings.romeroX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagODX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.romeroX = flagODX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Wells".Translate(), -1, "HVMP_Tooltip_Wells".Translate());
                Text.Font = GameFont.Tiny;
                bool flagOE1 = settings.wells1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Wells".Translate() + ": " + "HVMP_HardMode_Label_Wells_1".Translate(), ref flagOE1, "HVMP_HardMode_Tooltip_Wells_1".Translate());
                settings.wells1 = flagOE1;
                bool flagOE2 = settings.wells2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Wells".Translate() + ": " + "HVMP_HardMode_Label_Wells_2".Translate(), ref flagOE2, "HVMP_HardMode_Tooltip_Wells_2".Translate());
                settings.wells2 = flagOE2;
                bool flagOE3 = settings.wells3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Wells".Translate() + ": " + "HVMP_HardMode_Label_Wells_3".Translate(), ref flagOE3, "HVMP_HardMode_Tooltip_Wells_3".Translate());
                settings.wells3 = flagOE3;
                bool flagOEX = settings.wellsX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagOEX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.wellsX = flagOEX;
            }
            listing_Standard2.End();
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }
        private void BranchPlatformSettings(Rect inRect)
        {
            Rect rect = new Rect(inRect.xMin, inRect.yMin, inRect.width, inRect.height);
            Rect rect2 = new Rect(inRect.xMin, inRect.yMin, inRect.width * 0.96f, 4500f);
            float x = rect.xMin, y = rect.yMin, halfWidth = rect2.width * 0.5f;
            //branch platform spawn rate
            displayBPI = ((int)settings.makeNewBranchPlatformInterval).ToString();
            displayAuthCD = ((int)settings.authorizerCooldownDays).ToString();
            displayBPLimit = ((int)settings.maxPlatformsPerBranch).ToString();
            displayPASG = ((int)settings.authorizerStandingGain).ToString();
            displayBPD = ((int)settings.platformDefenderScale).ToString();
            Rect rect3 = new Rect(inRect.xMin, y, (inRect.width - 30f) * 0.8f, 4000f);
            Listing_Standard listing_Standard2 = new Listing_Standard();
            listing_Standard2.Begin(rect3);
            Text.Font = GameFont.Medium;
            listing_Standard2.Label("HVMP_Label_PlatformSettings".Translate(), 30f);
            //listing_Standard2.Gap(30f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Tooltip_PlatformSettings".Translate());
            listing_Standard2.End();
            y += 100;
            float origBPI = settings.makeNewBranchPlatformInterval;
            Rect bpiRect = new Rect(x + 10, y, halfWidth - 15, 32);
            settings.makeNewBranchPlatformInterval = Widgets.HorizontalSlider(bpiRect, settings.makeNewBranchPlatformInterval, 5f, 60f, true, "HVMP_SettingBranchPlatformInterval".Translate(), "5 days", "60 days", 1f);
            TooltipHandler.TipRegion(bpiRect.LeftPart(1f), "HVMP_TooltipBranchPlatformInterval".Translate());
            if (origBPI != settings.makeNewBranchPlatformInterval)
            {
                displayBPI = ((int)settings.makeNewBranchPlatformInterval).ToString();
            }
            y += 32;
            string origStringBPI = displayBPI;
            displayBPI = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayBPI);
            if (!displayBPI.Equals(origStringBPI))
            {
                this.ParseInput(displayBPI, settings.makeNewBranchPlatformInterval, out settings.makeNewBranchPlatformInterval, 5f, 60f);
            }
            y -= 32;
            //authorizer cooldown per standing cost
            float origPACooldown = settings.authorizerCooldownDays;
            Rect paCooldownRect = new Rect(x + 5 + halfWidth, y, halfWidth - 15, 32);
            settings.authorizerCooldownDays = Widgets.HorizontalSlider(paCooldownRect, settings.authorizerCooldownDays, 1f, 6f, true, "HVMP_SettingAuthorizerCooldown".Translate(), "1d", "6d", 0.1f);
            TooltipHandler.TipRegion(paCooldownRect.LeftPart(1f), "HVMP_TooltipAuthorizerCooldown".Translate());
            if (origPACooldown != settings.authorizerCooldownDays)
            {
                displayAuthCD = ((int)settings.authorizerCooldownDays).ToString();
            }
            y += 32;
            string origStringPACooldown = displayAuthCD;
            displayAuthCD = Widgets.TextField(new Rect(x + 5 + halfWidth, y, 50, 32), displayAuthCD);
            if (!displayAuthCD.Equals(origStringPACooldown))
            {
                this.ParseInput(displayAuthCD, settings.authorizerCooldownDays, out settings.authorizerCooldownDays, 1f, 6f);
            }
            y += 32;
            //max platforms per branch
            float origBPLimit = settings.maxPlatformsPerBranch;
            Rect bplimitRect = new Rect(x + 10, y, halfWidth - 15, 32);
            settings.maxPlatformsPerBranch = Widgets.HorizontalSlider(bplimitRect, settings.maxPlatformsPerBranch, 1f, 4f, true, "HVMP_SettingBranchPlatformLimit".Translate(), "1", "4", 1f);
            TooltipHandler.TipRegion(bplimitRect.LeftPart(1f), "HVMP_TooltipBranchPlatformLimit".Translate());
            if (origBPLimit != settings.maxPlatformsPerBranch)
            {
                displayBPLimit = ((int)settings.maxPlatformsPerBranch).ToString();
            }
            y += 32;
            string origStringBPLimit = displayBPLimit;
            displayBPLimit = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayBPLimit);
            if (!displayBPLimit.Equals(origStringBPLimit))
            {
                this.ParseInput(displayBPLimit, settings.maxPlatformsPerBranch, out settings.maxPlatformsPerBranch, 1f, 4f);
            }
            y -= 32;
            //authorizer standing gain
            float origPASG = settings.authorizerStandingGain;
            Rect pasgRect = new Rect(x + 5 + halfWidth, y, halfWidth - 15, 32);
            settings.authorizerStandingGain = Widgets.HorizontalSlider(pasgRect, settings.authorizerStandingGain, 1f, 10f, true, "HVMP_SettingAuthorizerStandingGain".Translate(), "1", "10", 1f);
            TooltipHandler.TipRegion(pasgRect.LeftPart(1f), "HVMP_TooltipAuthorizerStandingGain".Translate());
            if (origPASG != settings.authorizerStandingGain)
            {
                displayPASG = ((int)settings.authorizerStandingGain).ToString();
            }
            y += 32;
            string origStringPASG = displayPASG;
            displayPASG = Widgets.TextField(new Rect(x + 5 + halfWidth, y, 50, 32), displayPASG);
            if (!displayPASG.Equals(origStringPASG))
            {
                this.ParseInput(displayPASG, settings.authorizerStandingGain, out settings.authorizerStandingGain, 1f, 10f);
            }
            y += 32;
            //branch platform defender scale
            float origBPD = settings.platformDefenderScale;
            Rect bpDefenderRect = new Rect(x + 10, y, halfWidth - 15, 32);
            settings.platformDefenderScale = Widgets.HorizontalSlider(bpDefenderRect, settings.platformDefenderScale, 1f, 7f, true, "HVMP_SettingBranchPlatformDefenders".Translate(), "1x", "7x", 1f);
            TooltipHandler.TipRegion(bpDefenderRect.LeftPart(1f), "HVMP_TooltipBranchPlatformDefenders".Translate());
            if (origBPD != settings.platformDefenderScale)
            {
                displayBPD = ((int)settings.platformDefenderScale).ToString();
            }
            y += 32;
            string origStringBPD = displayBPD;
            displayBPD = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayBPD);
            if (!displayBPD.Equals(origStringBPD))
            {
                this.ParseInput(displayBPD, settings.platformDefenderScale, out settings.platformDefenderScale, 1f, 7f);
            }
        }
        private void ExpectationLevelSelector(Rect rect)
        {
            if (Widgets.ButtonText(rect, "HVMP_TooltipMinExpectationLevel".Translate(bb1), true, true, true, null))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>
                {
                    new FloatMenuOption("HVMP_Inactive".Translate(), delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 999;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption(ExpectationsUtility.ExpectationForOrder(0).LabelCap, delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 0;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption(ExpectationsUtility.ExpectationForOrder(1).LabelCap, delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 1;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption(ExpectationsUtility.ExpectationForOrder(2).LabelCap, delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 2;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption(ExpectationsUtility.ExpectationForOrder(3).LabelCap, delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 3;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption(ExpectationsUtility.ExpectationForOrder(4).LabelCap, delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 4;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption(ExpectationsUtility.ExpectationForOrder(5).LabelCap, delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 5;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0)
                };
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }
        private string ExpectationLabel
        {
            get
            {
                return settings.bratBehaviorMinExpectationLvl > 5 ? "HVMP_Inactive".Translate() : ExpectationsUtility.ExpectationForOrder(settings.bratBehaviorMinExpectationLvl).LabelCap;
            }
        }
        private void SeniorityLevelSelector(Rect rect)
        {
            if (Widgets.ButtonText(rect, "HVMP_TooltipMinSeniorityLevel".Translate(bb2), true, true, true, null))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>
                {
                    new FloatMenuOption("HVMP_Inactive".Translate(), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 99999;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_LevelFriend".Translate(), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 0;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_Level".Translate(1), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 100;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_Level".Translate(2), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 200;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_Level".Translate(3), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 300;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_Level".Translate(4), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 400;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_Level".Translate(5), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 500;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_Level".Translate(6), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 600;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0)
                };
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }
        private string SeniorityLabel
        {
            get
            {
                return settings.bratBehaviorMinSeniorityLvl > 800 ? "HVMP_Inactive".Translate() : "HVMP_Level".Translate(settings.bratBehaviorMinSeniorityLvl / 100);
            }
        }
        private void ParseInput(string buffer, float origValue, out float newValue, float min, float max)
        {
            if (!float.TryParse(buffer, out newValue))
                newValue = origValue;
            if (newValue < min)
                newValue = min;
            if (newValue > max)
                newValue = max;
        }
        private void ParseInput(string buffer, int origValue, out int newValue, int min, int max)
        {
            if (!int.TryParse(buffer, out newValue))
                newValue = origValue;
            if (newValue < min)
                newValue = min;
            if (newValue > max)
                newValue = max;
        }
        public override string SettingsCategory()
        {
            return "Hauts' Enterprise: More Permits";
        }
        public static HVMP_Settings settings;
        public string displayMin, displayMax, displayQuestRewardFactor, refusalLoss, failureLoss, bb1, bb2, displayBPI, displayBPLimit, displayBPD, displayAuthCD, displayPASG;
        public Vector2 scrollPosition = Vector2.zero;
        private int PageIndex;
    }
}
