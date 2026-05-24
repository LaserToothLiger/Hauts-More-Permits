using HautsFramework;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;

namespace HautsPermits
{
    //hospitality, for pawns of a random kind from pawnKinds. (Currently, this is only set to Town_Trader, but you could sub it out for whatever. Guard 9 Stellarchs for me, please! - the Commerce Branch)
    public class QuestNode_CommerceFortification : QuestNode_CommerceIntermediary
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            PawnKindDef pawnKindDef;
            if (pawnKinds.TryRandomElement(out pawnKindDef))
            {
                slate.Set<PawnKindDef>(this.storePawnKindAs.GetValue(slate), pawnKindDef, false);
            }
            base.RunInt();
        }
        [NoTranslate]
        public SlateRef<string> storePawnKindAs;
        public List<PawnKindDef> pawnKinds;
    }
    /*handles instantiation of all three mutators' effects
     * fort1: Greed is Good - creates the GIG_condition and puts it in the world game condition manager. Lasts for GIG_duration
     * fort2: Riches to Ruins - gives the R2R_hediff to all such pawns
     * fort3: What's Bugging You? - gives the WBY_hediff to all such pawns. Wow, these are pretty simple in comparison to some of the other mutators*/
    public class QuestNode_Fort_AllThreeMutators : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (this.pawns.GetValue(slate) == null)
            {
                return;
            }
            bool mayhemMode = HVMP_Mod.settings.fortX;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.fort1, mayhemMode) && !this.IDHTGU_nodes.NullOrEmpty())
            {
                for (int i = 0; i < this.IDHTGU_nodes.Count; i++)
                {
                    this.IDHTGU_nodes[i].Run();
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_IDHTGU_info_singular", this.IDHTGU_description_singular.Formatted()),
                    new Rule_String("mutator_IDHTGU_info_plural", this.IDHTGU_description_plural.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_IDHTGU_info_singular", " "), new Rule_String("mutator_IDHTGU_info_plural", " ") });
            }
            if (this.R2R_hediff != null && BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.fort2, mayhemMode))
            {
                List<Pawn> pawns = this.pawns.GetValue(slate).ToList();
                for (int i = 0; i < pawns.Count; i++)
                {
                    Hediff h = HediffMaker.MakeHediff(this.R2R_hediff, pawns[i]);
                    pawns[i].health.AddHediff(h);
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_R2R_info_singular", this.R2R_description_singular.Formatted()),
                    new Rule_String("mutator_R2R_info_plural", this.R2R_description_plural.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_R2R_info_singular", " "), new Rule_String("mutator_R2R_info_plural", " ") });
            }
            if (this.WBY_hediff != null && BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.fort3, mayhemMode))
            {
                List<Pawn> pawns = this.pawns.GetValue(slate).ToList();
                for (int i = 0; i < pawns.Count; i++)
                {
                    Hediff h = HediffMaker.MakeHediff(this.WBY_hediff, pawns[i]);
                    pawns[i].health.AddHediff(h);
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_WBY_info_singular", this.WBY_description_singular.Formatted()),
                    new Rule_String("mutator_WBY_info_plural", this.WBY_description_plural.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_WBY_info_singular", " "), new Rule_String("mutator_WBY_info_plural", " ") });
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<IEnumerable<Pawn>> pawns;
        public List<QuestNode> IDHTGU_nodes = new List<QuestNode>();
        [MustTranslate]
        public string IDHTGU_description_singular;
        [MustTranslate]
        public string IDHTGU_description_plural;
        public HediffDef R2R_hediff;
        [MustTranslate]
        public string R2R_description_singular;
        [MustTranslate]
        public string R2R_description_plural;
        public HediffDef WBY_hediff;
        [MustTranslate]
        public string WBY_description_singular;
        [MustTranslate]
        public string WBY_description_plural;
    }
    //GIG game condition adds spy points over time to hostile pawns
    public class GameCondition_GreedIsGood : GameCondition
    {
        public override void GameConditionTick()
        {
            base.GameConditionTick();
            if (Find.TickManager.TicksGame % 60000 == 0)
            {
                WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                if (WCFC != null)
                {
                    Faction p = Faction.OfPlayerSilentFail;
                    if (p != null)
                    {
                        foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
                        {
                            if (f != p && f.RelationKindWith(p) == FactionRelationKind.Hostile)
                            {
                                Hauts_FactionCompHolder fch = WCFC.FindCompsFor(f);
                                if (fch != null)
                                {
                                    HautsFactionComp_SpyPoints spc = fch.TryGetComp<HautsFactionComp_SpyPoints>();
                                    if (spc != null)
                                    {
                                        spc.spyPoints += 75;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    /*derivative of SeverityPerDay, which also intermittently causes infestations on the pawn's location.
     * Obviously, does not work if the pawn is despawned and thus has no location, but we don't need to care much about that since the game won't LET you despawn quest lodgers in many of the most common ways*/
    public class HediffCompProperties_SeverityPerDay_WBY : HediffCompProperties_SeverityPerDay
    {
        public HediffCompProperties_SeverityPerDay_WBY()
        {
            this.compClass = typeof(HediffComp_SeverityPerDay_WBY);
        }
        public float infestationMTBhours;
    }
    public class HediffComp_SeverityPerDay_WBY : HediffComp_SeverityPerDay
    {
        private HediffCompProperties_SeverityPerDay_WBY Props
        {
            get
            {
                return (HediffCompProperties_SeverityPerDay_WBY)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.SpawnedOrAnyParentSpawned && this.Pawn.IsHashIntervalTick(500, delta) && this.parent.Severity == this.parent.def.maxSeverity)
            {
                if (this.Props.infestationMTBhours > 0f && Rand.MTBEventOccurs(this.Props.infestationMTBhours, 2500f, 500))
                {
                    IncidentParms incidentParms = new IncidentParms();
                    incidentParms.target = this.Pawn.MapHeld;
                    incidentParms.points = StorytellerUtility.DefaultThreatPointsNow(this.Pawn.MapHeld);
                    incidentParms.infestationLocOverride = new IntVec3?(this.Pawn.PositionHeld);
                    incidentParms.forced = true;
                    IncidentDefOf.Infestation.Worker.TryExecute(incidentParms);
                }
            }
        }
    }
}
