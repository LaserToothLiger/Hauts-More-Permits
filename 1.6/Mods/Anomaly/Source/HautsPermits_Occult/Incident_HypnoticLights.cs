using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsPermits_Occult
{
    //make the condition, play the sound. You can't create a hypnolight within a hypnolight, that would tear open a rift to the Astral Plane
    public class IncidentWorker_Hypnolight : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return !map.gameConditionManager.ConditionIsActive(HVMPDefOf_A.HVMP_LovecraftHypnoLight);
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            this.DoConditionAndLetter(parms, map, Mathf.RoundToInt(this.def.durationDays.RandomInRange * 60000f), parms.points);
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera((Map)parms.target);
            return true;
        }
        protected void DoConditionAndLetter(IncidentParms parms, Map map, int duration, float points)
        {
            if (points < 0f)
            {
                points = StorytellerUtility.DefaultThreatPointsNow(map);
            }
            GameCondition gameCondition = GameConditionMaker.MakeCondition(HVMPDefOf_A.HVMP_LovecraftHypnoLight, duration);
            map.gameConditionManager.RegisterCondition(gameCondition);
            base.SendStandardLetter(gameCondition.LabelCap, gameCondition.LetterText, gameCondition.def.letterDef, parms, LookTargets.Invalid, Array.Empty<NamedArgument>());
        }
    }
    /*hypnotic lights is a lot like an aurora: bright, throws colors around, has a (ridiculously strong) influence on skygazing recreation
     * it also periodically decreases pawns' outdoors needs and adds the hypnotization hediff to non-blind, non-anomalous pawns who can see the sky (in unroofed cell and it's lit at all)*/
    public class GameCondition_HypnoLight : GameCondition
    {
        public Color CurrentColor
        {
            get
            {
                return Color.Lerp(GameCondition_HypnoLight.Colors[this.prevColorIndex], GameCondition_HypnoLight.Colors[this.curColorIndex], this.curColorTransition);
            }
        }
        private int GetNewColorIndex()
        {
            return (from x in Enumerable.Range(0, GameCondition_HypnoLight.Colors.Length)
                    where x != this.curColorIndex
                    select x).RandomElement<int>();
        }
        private int TransitionDurationTicks
        {
            get
            {
                if (!base.Permanent)
                {
                    return 280;
                }
                return 3750;
            }
        }
        public override float SkyGazeChanceFactor(Map map)
        {
            return 10f;
        }
        public override float SkyGazeJoyGainFactor(Map map)
        {
            return 5f;
        }
        public override float SkyTargetLerpFactor(Map map)
        {
            return GameConditionUtility.LerpInOutValue(this, (float)this.TransitionTicks, 1f);
        }
        public override SkyTarget? SkyTarget(Map map)
        {
            Color currentColor = this.CurrentColor;
            SkyColorSet skyColorSet = new SkyColorSet(Color.Lerp(Color.white, currentColor, 0.075f) * this.Brightness(map), new Color(0.92f, 0.92f, 0.92f), Color.Lerp(Color.white, currentColor, 0.025f) * this.Brightness(map), 1f);
            return new SkyTarget?(new SkyTarget(Mathf.Max(GenCelestial.CurCelestialSunGlow(map), 0.25f), skyColorSet, 1f, 1f));
        }
        private float Brightness(Map map)
        {
            return Mathf.Max(0.73f, GenCelestial.CurCelestialSunGlow(map));
        }
        public override void Init()
        {
            base.Init();
            this.curColorIndex = Rand.Range(0, GameCondition_HypnoLight.Colors.Length);
            this.prevColorIndex = this.curColorIndex;
            this.curColorTransition = 1f;
        }
        public override void GameConditionTick()
        {
            this.curColorTransition += 1f / (float)this.TransitionDurationTicks;
            if (this.curColorTransition >= 1f)
            {
                this.prevColorIndex = this.curColorIndex;
                this.curColorIndex = this.GetNewColorIndex();
                this.curColorTransition = 0f;
            }
            if (Find.TickManager.TicksGame % 100 == 0)
            {
                foreach (Map m in base.AffectedMaps)
                {
                    foreach (Pawn p in m.mapPawns.AllPawnsSpawned)
                    {
                        if (p.needs.outdoors != null)
                        {
                            p.needs.outdoors.CurLevel -= 0.01f;
                        }
                        if (!p.PositionHeld.Roofed(p.MapHeld) && m.glowGrid.GroundGlowAt(p.PositionHeld) > float.Epsilon && !PawnUtility.IsBiologicallyOrArtificiallyBlind(p) && !p.IsMutant && !p.IsEntity)
                        {
                            p.health.hediffSet.TryGetHediff(HVMPDefOf_A.HVMP_LightHypnotized, out Hediff lh);
                            if (lh == null)
                            {
                                Hediff hediff = HediffMaker.MakeHediff(HVMPDefOf_A.HVMP_LightHypnotized, p);
                                p.health.AddHediff(hediff);
                            }
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.curColorIndex, "curColorIndex", 0, false);
            Scribe_Values.Look<int>(ref this.prevColorIndex, "prevColorIndex", 0, false);
            Scribe_Values.Look<float>(ref this.curColorTransition, "curColorTransition", 0f, false);
        }
        private float curColorTransition;
        private int curColorIndex = -1;
        private int prevColorIndex = -1;
        private static readonly Color[] Colors = new Color[]
        {
            new Color(0f, 1f, 0f),
            new Color(0.3f, 1f, 0f),
            new Color(0f, 1f, 0.7f),
            new Color(0.3f, 1f, 0.7f),
            new Color(0f, 0.5f, 1f),
            new Color(0f, 0f, 1f),
            new Color(0.87f, 0f, 1f),
            new Color(0.75f, 0f, 1f)
        };
    }
    //instantly clean up if there is no hypnotic light condition on the map. Otherwise, gains severity while susceptible to hypnotic light as per the game condition's definition
    public class Hediff_HypnoLight : Hediff
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.Spawned)
            {
                GameCondition_HypnoLight activeCondition = pawn.MapHeld.gameConditionManager.GetActiveCondition<GameCondition_HypnoLight>();
                if (activeCondition != null)
                {
                    if (!this.pawn.PositionHeld.Roofed(this.pawn.MapHeld) && this.pawn.Map.glowGrid.GroundGlowAt(this.pawn.PositionHeld) > float.Epsilon && !PawnUtility.IsBiologicallyOrArtificiallyBlind(this.pawn) && !this.pawn.IsMutant && !this.pawn.IsEntity)
                    {
                        this.Severity += 0.0001f;
                        return;
                    }
                }
            }
            this.Severity -= 0.001f;
        }
    }
}
