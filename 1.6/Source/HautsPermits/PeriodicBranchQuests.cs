using HautsFramework;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HautsPermits
{
    /*A faction is only a branch faction if its def has this DME.
     * Its presence makes the faction's comms console options those of e-branches, prevents them from ever being considered destroyed, and [Odyssey] allows for the creation of branch platforms.
     *   (and possibly other properties I've forgor, but you get the gist. Go read the user manual to see what's so unique about branch factions)
     * quests: when the BranchStuff world component issues a quest for this branch faction (other than Making Ties), it pulls a random one from this list
     * donationTraderKind: when you ring them up on the comms console, this is the trader that is keyed to the donations option. See TraderKinds_Branches.xml
     * donationString: the aforementioned donation option has a label that says "Donate __ (grants standing)". This fills that blank
     * tiedToAnomalyThreatFraction: this faction's Making Ties quest and periodic quests can only be offered if the monolith is active or if a chance check against the current anomaly threat fraction passes.*/
    public class EBranchQuests : DefModExtension
    {
        public EBranchQuests() { }
        public List<QuestScriptDef> quests;
        public TraderKindDef donationTraderKind;
        public string donationString;
        public bool tiedToAnomalyThreatFraction;
    }
    /*initDelay: when this comp is instantiated, if the faction is a branch faction (remember, that means if it has the EBranchQuests DME), its first quest will be issued after a random value within this many ticks.
     * This comp handles:
     * -branch faction leaders should have the highest title possible, purely for display purposes when necessary
     * -keep track of the cooldowns for random periodic quests, as well as the universal cooldown for soliciting a quest thru the comms console
     * -inflict the Refusal goodwill loss if a periodic quest could not be issued SPECIFICALLY because you disabled goodwill or standing rewards for this faction.
     * "interfactionAidTick" specifically is used to do Cosmopolitan's cooldown-reducing effect on requesting military aid without resorting to wacky reflection tricks*/
    public class HautsFactionCompProperties_PeriodicBranchQuests : HautsFactionCompProperties
    {
        public HautsFactionCompProperties_PeriodicBranchQuests()
        {
            this.compClass = typeof(HautsFactionComp_PeriodicBranchQuests);
        }
        public IntRange initDelay;
    }
    public class HautsFactionComp_PeriodicBranchQuests : HautsFactionComp
    {
        public HautsFactionCompProperties_PeriodicBranchQuests Props
        {
            get
            {
                return (HautsFactionCompProperties_PeriodicBranchQuests)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            EBranchQuests gq = this.ThisFaction.def.GetModExtension<EBranchQuests>();
            if (gq != null)
            {
                this.cooldownTicks = this.Props.initDelay.RandomInRange - Find.TickManager.TicksGame;
                this.commsQuestCooldownTicks = 0;
                this.isBranch = true;
                Pawn leader = this.ThisFaction.leader;
                if (leader != null)
                {
                    if (leader.royalty != null)
                    {
                        leader.royalty = new Pawn_RoyaltyTracker(leader);
                    }
                    if (leader.Faction.def.HasRoyalTitles)
                    {
                        leader.royalty.SetTitle(leader.Faction, leader.Faction.def.RoyalTitlesAllInSeniorityOrderForReading.Last(), false, false, false);
                    }
                }
            } else {
                this.cooldownTicks = -1;
                this.commsQuestCooldownTicks = -1;
            }
        }
        public override void CompPostTick()
        {
            base.CompPostTick();
            if (this.isBranch)
            {
                if (this.commsQuestCooldownTicks > 0)
                {
                    this.commsQuestCooldownTicks--;
                }
                if (this.cooldownTicks > 0)
                {
                    this.cooldownTicks--;
                } else {
                    this.DoCooldowns();
                    if (this.tiesEstablished)
                    {
                        if (this.ThisFaction.allowGoodwillRewards && this.ThisFaction.allowRoyalFavorRewards)
                        {
                            this.IssueQuest();
                        } else {
                            int ebgl = HVMP_Utility.ExpectationBasedGoodwillLoss(null, true, true, this.ThisFaction);
                            if (ebgl != 0)
                            {
                                Faction.OfPlayer.TryAffectGoodwillWith(this.ThisFaction, ebgl, true, true, HVMPDefOf.HVMP_IgnoredQuest, null);
                            }
                        }
                    } else if (!this.tieQuestOffered && this.AnomalyRequirementsMet()) {
                        this.IssueQuest(true);
                    }
                }
            }
        }
        public bool AnomalyRequirementsMet(bool onlyIfNotDisabled = false)
        {
            if (HVMP_Mod.settings.occultCanAlwaysBeEncountered)
            {
                return true;
            }
            if (ModsConfig.AnomalyActive)
            {
                AnomalyPlaystyleDef apsd = Find.Storyteller.difficulty.AnomalyPlaystyleDef;
                if (apsd != null && apsd.enableAnomalyContent)
                {
                    if (Find.Anomaly.HighestLevelReached > 0 || (onlyIfNotDisabled && !Find.Anomaly.GenerateMonolith))
                    {
                        return true;
                    }
                    EBranchQuests gq = this.ThisFaction.def.GetModExtension<EBranchQuests>();
                    if (gq != null && (!gq.tiedToAnomalyThreatFraction || Rand.Value <= Find.Anomaly.AnomalyThreatFractionNow))
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }
        public void DoCooldowns()
        {
            if (ModsConfig.OdysseyActive)
            {
                Faction tg = Find.FactionManager.OfTradersGuild;
                if (tg != null)
                {
                    this.ThisFaction.SetRelation(new FactionRelation
                    {
                        other = tg,
                        kind = FactionRelationKind.Ally
                    });
                }
            }
            Faction mech = Find.FactionManager.OfMechanoids;
            if (mech != null)
            {
                this.ThisFaction.SetRelation(new FactionRelation
                {
                    other = mech,
                    kind = FactionRelationKind.Hostile
                });
            }
            if (HVMP_Mod.settings.maximumChaosMode || !this.AnomalyRequirementsMet())
            {
                //this.cooldownTicks = 600;
                this.cooldownTicks = Rand.RangeInclusive(60000 * (int)HVMP_Mod.settings.minBranchQuestInterval, 60000 * (int)HVMP_Mod.settings.maxBranchQuestInterval);
            } else {
                WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
                {
                    Hauts_FactionCompHolder fch = WCFC.FindCompsFor(faction);
                    if (fch != null)
                    {
                        HautsFactionComp_PeriodicBranchQuests pgq = fch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                        if (pgq != null)
                        {
                            //pgq.cooldownTicks = 600;
                            pgq.cooldownTicks = Rand.RangeInclusive(60000 * (int)HVMP_Mod.settings.minBranchQuestInterval, 60000 * (int)HVMP_Mod.settings.maxBranchQuestInterval);
                        }
                    }
                }
            }
        }
        public void IssueQuest(bool intro = false)
        {
            if (Find.AnyPlayerHomeMap == null)
            {
                return;
            }
            EBranchQuests gq = this.ThisFaction.def.GetModExtension<EBranchQuests>();
            if (gq != null && this.AnomalyRequirementsMet())
            {
                if (intro)
                {
                    this.tieQuestOffered = true;
                    Slate slate = new Slate();
                    slate.Set<Faction>("faction", this.ThisFaction, false);
                    slate.Set<Thing>("asker", this.ThisFaction.leader, false);
                    Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(HVMPDefOf.HVMP_BranchIntro, slate);
                    if (!quest.hidden && quest.root.sendAvailableLetter)
                    {
                        QuestUtility.SendLetterQuestAvailable(quest);
                    }
                } else if (gq.quests != null) {
                    List<QuestScriptDef> qsds = new List<QuestScriptDef>();
                    Slate slate = new Slate();
                    slate.Set<Faction>("branchFaction", this.ThisFaction, false);
                    slate.Set<float>("points", HVMP_Utility.TryGetPoints(null), false);
                    slate.Set<Map>("map", null, false);
                    IIncidentTarget randomPlayerHomeMap = Find.RandomSurfacePlayerHomeMap;
                    if (randomPlayerHomeMap == null)
                    {
                        randomPlayerHomeMap = Find.RandomPlayerHomeMap;
                    }
                    foreach (QuestScriptDef qsd in gq.quests)
                    {
                        if (qsd.CanRun(slate, randomPlayerHomeMap ?? Find.World))
                        {

                            qsds.Add(qsd);
                        }
                    }
                    if (qsds.Count == 0)
                    {
                        Log.Error("HVMP_ErrorNoUsableBranchQuests".Translate(this.ThisFaction.NameColored));
                        return;
                    }
                    QuestScriptDef questDef = qsds.RandomElement();
                    Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
                    if (!quest.hidden && quest.root.sendAvailableLetter)
                    {
                        QuestUtility.SendLetterQuestAvailable(quest);
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.tieQuestOffered, "tieQuestOffered", false, false);
            Scribe_Values.Look<bool>(ref this.tiesEstablished, "tiesEstablished", false, false);
            Scribe_Values.Look<int>(ref this.cooldownTicks, "cooldownTicks", 0, false);
            Scribe_Values.Look<int>(ref this.commsQuestCooldownTicks, "commsQuestCooldownTicks", 0, false);
            Scribe_Values.Look<bool>(ref this.isBranch, "isBranch", false, false);
            Scribe_Values.Look<int>(ref this.interfactionAidTick, "interfactionAidTick", 0, false);
        }
        public bool tieQuestOffered;
        public bool tiesEstablished;
        public int cooldownTicks;
        public int commsQuestCooldownTicks;
        public bool isBranch;
        public Pawn tmpNegotiatorForInterfactionAid;
        public int interfactionAidTick;
    }
}
