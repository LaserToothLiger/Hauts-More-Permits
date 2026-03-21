using RimWorld;
using Verse;

namespace HautsPermits
{
    [DefOf]
    public static class HVMPDefOf
    {
        static HVMPDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HVMPDefOf));
        }
        public static FactionDef HVMP_CommerceBranch;
        public static FactionDef HVMP_PaxBranch;
        public static FactionDef HVMP_RoverBranch;
        [MayRequireIdeology]
        public static FactionDef HVMP_ArchiveBranch;
        [MayRequireBiotech]
        public static FactionDef HVMP_EcosphereBranch;
        [MayRequireAnomaly]
        public static FactionDef HVMP_OccultBranch;

        public static GameConditionDef HVMP_CommsBlackout;
        [MayRequireIdeology]
        public static GameConditionDef HVMP_RevealingScanEffect;

        public static HediffDef HVMP_HostileEnvironmentFilm;
        public static HediffDef HVMP_TargetedPsychicSuppression;

        public static HistoryEventDef HVMP_CutTiesWithBranch;
        public static HistoryEventDef HVMP_SolicitedQuest;
        public static HistoryEventDef HVMP_RefreshedPermitCDs;
        public static HistoryEventDef HVMP_IgnoredQuest;
        public static HistoryEventDef HVMP_IngratiationAccepted;

        public static IncidentDef HVMP_RaidBellum;
        [MayRequireBiotech]
        public static IncidentDef HVMP_MutantManhunterPack;
        [MayRequireAnomaly]
        public static IncidentDef HVMP_ShamblerAssault;
        [MayRequireAnomaly]
        public static IncidentDef HVMP_UsherStrike;

        public static JobDef HVMP_StudyQuestItem;
        [MayRequireBiotech]
        public static JobDef HVMP_InjectChargecellBattery;
        [MayRequireBiotech]
        public static JobDef HVMP_InjectChargecellMech;
        [MayRequireOdyssey]
        public static JobDef HVMP_InstallPTargeter;
        [MayRequireOdyssey]
        public static JobDef HVMP_InstallBSU;
        [MayRequireOdyssey]
        public static JobDef HVMP_AttachIngressor;

        [MayRequireIdeology]
        public static PawnKindDef HVMP_Anthropologist;

        [MayRequireIdeology]
        public static PreceptDef HVMP_InterfactionAidImproved;

        public static QuestScriptDef HVMP_BranchIntro;
        public static QuestScriptDef HVMP_BranchOutro;

        [MayRequireIdeology]
        public static SitePartDef HVMP_Shrine;

        public static ThingDef HVMP_DropPodOfFaction;
        public static ThingDef HVMP_DelayedPowerBeam;
        public static ThingDef HVMP_DatedAtlas;
        public static ThingDef HVMP_TunnelHiveSpawner;
        [MayRequireOdyssey]
        public static ThingDef HVMP_EnterpriseSecurityCrate;
        [MayRequireOdyssey]
        public static ThingDef HVMP_BiofilmMedicine;
        public static FleckDef HVMP_BribeGlow;
        public static FleckDef HVMP_ScalpelBLAST;
        public static FleckDef HVMP_RepairGlow;
        public static FleckDef HVMP_DecryptGlow;
        public static FleckDef HVMP_QualityGlow;
        public static FleckDef HVMP_VaalOrNoBaals;

        [MayRequireIdeology]
        public static ThoughtDef HVMP_AnthroAnnoyance;

        public static WorldObjectDef HVMP_AtlasPoint;
        public static WorldObjectDef HVMP_OdysseyPoint;
        [MayRequireOdyssey]
        public static WorldObjectDef HVMP_BranchPlatform;
    }
}
