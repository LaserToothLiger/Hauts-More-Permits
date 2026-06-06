using HautsF_Ideology;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace HautsPermits_Ideology
{
    /*Derivative of PromoteIdeo, which is in the Framework's Ideology subdirectory
     * -benefitRoster: one of these is selected to be the book's "currentBuff" (although null is also a valid option). Every 250 ticks spent reading this book, a believer in this ideo gains currentBuff...
     * -durationPerHour: adding this amount of ticks to its HediffComp_Disappears duration, up to maxDuration...
     * -severityPerHour: ...and adding this much to its severity
     * -hediffGainPerHour: the duration and severity gains are multiplied by the evaluation of the book's quality (e.g. 0 is awful, 6 is legendary)
     * -inspireChancePerHour: only used if currentBuff is null. With the book's quality as the x-value, evaluation produces the y-value. Every 250 ticks spent reading this book, a believer in its ideo has the [y-value/10]% chance to become inspired*/
    public class CompProperties_ChooseScriptureBuff : CompProperties
    {
        public CompProperties_ChooseScriptureBuff()
        {
            this.compClass = typeof(CompChooseScriptureBuff);
        }
        public SimpleCurve inspireChancePerHour = new SimpleCurve
            {
                {new CurvePoint(0f, 0.0001f),true},
                {new CurvePoint(1f, 0.0001f),true},
                {new CurvePoint(2f, 0.0001f),true},
                {new CurvePoint(3f, 0.0001f),true},
                {new CurvePoint(4f, 0.0001f),true},
                {new CurvePoint(5f, 0.0001f),true},
                {new CurvePoint(6f, 0.0001f),true},
            };
        public int durationPerHour;
        public float severityPerHour;
        public int maxDuration = 60000;
        public SimpleCurve hediffGainPerHour = new SimpleCurve
            {
                {new CurvePoint(0f, 1f),true},
                {new CurvePoint(1f, 1f),true},
                {new CurvePoint(2f, 1f),true},
                {new CurvePoint(3f, 1f),true},
                {new CurvePoint(4f, 1f),true},
                {new CurvePoint(5f, 1f),true},
                {new CurvePoint(6f, 1f),true},
            };
        public List<HediffDef> benefitRoster = new List<HediffDef>();
    }
    public class CompChooseScriptureBuff : ThingComp
    {
        public CompProperties_ChooseScriptureBuff Props
        {
            get
            {
                return (CompProperties_ChooseScriptureBuff)this.props;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.currentBuff = Rand.Chance(1f/(1f+this.Props.benefitRoster.Count)) ? null : this.Props.benefitRoster.RandomElement();
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look<HediffDef>(ref this.currentBuff, "currentBuff");
        }
        public HediffDef currentBuff;
    }
    public class BookOutcomeProperties_ChooseScriptureBuff : BookOutcomeProperties_PromoteIdeo
    {
        public override Type DoerClass
        {
            get
            {
                return typeof(BookOutcomeDoerChooseScriptureBuff);
            }
        }
    }
    public class BookOutcomeDoerChooseScriptureBuff : BookOutcomeDoerPromoteIdeo
    {
        public new BookOutcomeProperties_ChooseScriptureBuff Props
        {
            get
            {
                return (BookOutcomeProperties_ChooseScriptureBuff)this.props;
            }
        }
        public CompChooseScriptureBuff BuffComp
        {
            get
            {
                if (this.Parent != null)
                {
                    CompChooseScriptureBuff ccsb = this.Parent.GetComp<CompChooseScriptureBuff>();
                    if (ccsb != null)
                    {
                        return ccsb;
                    }
                }
                return null;
            }
        }
        public HediffDef CurrentBuff
        {
            get
            {
                CompChooseScriptureBuff ccsb = this.BuffComp;
                if (ccsb != null)
                {
                    return ccsb.currentBuff;
                }
                return null;
            }
        }
        public float HediffGainPerHour
        {
            get
            {
                return this.BuffComp.Props.hediffGainPerHour.Evaluate((float)this.Quality);
            }
        }
        public float DurationPerHour
        {
            get
            {
                return this.BuffComp.Props.durationPerHour * HediffGainPerHour / 10;
            }
        }
        public float SeverityPerHour
        {
            get
            {
                return this.BuffComp.Props.severityPerHour * HediffGainPerHour / 10;
            }
        }
        public float ChanceToInspirePerHour
        {
            get
            {
                return this.BuffComp.Props.inspireChancePerHour.Evaluate((float)this.Quality);
            }
        }
        public override void ExtraReasurreEffect(Pawn reader, float oldCertainty)
        {
            CompChooseScriptureBuff ccsb = this.BuffComp;
            if (ccsb == null)
            {
                return;
            }
            //hediff
            HediffDef hd = ccsb.currentBuff;
            if (hd != null)
            {
                Hediff hediff = reader.health.hediffSet.GetFirstHediffOfDef(hd);
                if (hediff != null)
                {
                    hediff.Severity += this.SeverityPerHour;
                }else {
                    hediff = HediffMaker.MakeHediff(hd, reader, null);
                    reader.health.AddHediff(hediff);
                }
                HediffComp_Disappears hcd = hediff.TryGetComp<HediffComp_Disappears>();
                if (hcd != null)
                {
                    hcd.ticksToDisappear = Math.Min((int)this.DurationPerHour + hcd.ticksToDisappear, ccsb.Props.maxDuration);
                }
            } else {
                //inspire
                if (reader.mindState.inspirationHandler != null && !reader.Inspired && Rand.Chance(this.ChanceToInspirePerHour / 10f))
                {
                    InspirationDef inspiration = reader.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                    if (inspiration != null)
                    {
                        reader.mindState.inspirationHandler.TryStartInspiration(inspiration, "HVMP_InspiringVerseHitsRight".Translate(this.Book.Title, reader.Name.ToStringShort), true);
                    }
                }
            }
        }
        public override void ExtraEffectsStrings(ref StringBuilder stringBuilder)
        {
            if (this.BuffComp != null && this.BuffComp.currentBuff != null)
            {
                string text3 = "HVMP_IdeoBookHediffGain".Translate((this.SeverityPerHour * 10).ToStringByStyle(ToStringStyle.FloatTwo), this.BuffComp.currentBuff.label, (this.DurationPerHour / 250).ToStringByStyle(ToStringStyle.FloatTwo));
                stringBuilder.AppendLine(" - " + text3);
            } else if (this.ChanceToInspirePerHour > 0f) {
                string text3 = "HVMP_IdeoBookInspireChance".Translate((this.ChanceToInspirePerHour * 100f).ToStringDecimalIfSmall());
                stringBuilder.AppendLine(" - " + text3);
            }
        }
    }
}
