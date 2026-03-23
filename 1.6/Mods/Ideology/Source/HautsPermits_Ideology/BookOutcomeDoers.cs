using HautsF_Ideology;
using RimWorld;
using System;
using System.Text;
using Verse;

namespace HautsPermits_Ideology
{
    /*These are derivatives of PromoteIdeo, which is in the Framework's Ideology subdirectory
     * InspiringVerse
     * -inspireChancePerHour: with the book's quality as the x-value, evaluation produces the y-value. Every 250 ticks spent reading this book, a believer in its ideo has the [y-value/10]% chance to become inspired*/
    public class BookOutcomeProperties_InspiringVerse : BookOutcomeProperties_PromoteIdeo
    {
        public override Type DoerClass
        {
            get
            {
                return typeof(BookOutcomeDoerInspiringVerse);
            }
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
    }
    public class BookOutcomeDoerInspiringVerse : BookOutcomeDoerPromoteIdeo
    {
        public new BookOutcomeProperties_InspiringVerse Props
        {
            get
            {
                return (BookOutcomeProperties_InspiringVerse)this.props;
            }
        }
        public float ChanceToInspirePerHour
        {
            get
            {
                return this.Props.inspireChancePerHour.Evaluate((float)this.Quality);
            }
        }
        public override void ExtraReasurreEffect(Pawn reader, float oldCertainty)
        {
            if (reader.mindState.inspirationHandler != null && !reader.Inspired && Rand.Chance(this.ChanceToInspirePerHour / 10f))
            {
                InspirationDef inspiration = reader.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                if (inspiration != null)
                {
                    reader.mindState.inspirationHandler.TryStartInspiration(inspiration, "HVMP_InspiringVerseHitsRight".Translate(this.Book.Title, reader.Name.ToStringShort), true);
                }
            }
        }
        public override void ExtraEffectsStrings(ref StringBuilder stringBuilder)
        {
            if (this.ChanceToInspirePerHour > 0f)
            {
                string text3 = "HVMP_IdeoBookInspireChance".Translate((this.ChanceToInspirePerHour * 100f).ToStringDecimalIfSmall());
                stringBuilder.AppendLine(" - " + text3);
            }
        }
    }
    /*HediffGrantingIdeoBooks
     * -hediff: every 250 ticks spent reading this book, a believer in this ideo gains this hediff...
     * -durationPerHour: adding this amount of ticks to its HediffComp_Disappears duration, up to maxDuration...
     * -severityPerHour: ...and adding this much to its severity
     * hediffGainPerHour: the duration and severity gains are multiplied by the evaluation of the book's quality (e.g. 0 is awful, 6 is legendary)*/
    public class BookOutcomeProperties_HediffGrantingIdeoBook : BookOutcomeProperties_PromoteIdeo
    {
        public override Type DoerClass
        {
            get
            {
                return typeof(BookOutcomeDoerHediffGrantingForIdeo);
            }
        }
        public HediffDef hediff;
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
    }
    public class BookOutcomeDoerHediffGrantingForIdeo : BookOutcomeDoerPromoteIdeo
    {
        public new BookOutcomeProperties_HediffGrantingIdeoBook Props
        {
            get
            {
                return (BookOutcomeProperties_HediffGrantingIdeoBook)this.props;
            }
        }
        public float HediffGainPerHour
        {
            get
            {
                return this.Props.hediffGainPerHour.Evaluate((float)this.Quality);
            }
        }
        public float DurationPerHour
        {
            get
            {
                return this.Props.durationPerHour * HediffGainPerHour / 10;
            }
        }
        public float SeverityPerHour
        {
            get
            {
                return this.Props.severityPerHour * HediffGainPerHour / 10;
            }
        }
        public override void ExtraReasurreEffect(Pawn reader, float oldCertainty)
        {
            Hediff hediff = reader.health.hediffSet.GetFirstHediffOfDef(this.Props.hediff);
            if (hediff != null)
            {
                hediff.Severity += this.SeverityPerHour;
            } else {
                hediff = HediffMaker.MakeHediff(this.Props.hediff, reader, null);
                reader.health.AddHediff(hediff);
            }
            HediffComp_Disappears hcd = hediff.TryGetComp<HediffComp_Disappears>();
            if (hcd != null)
            {
                hcd.ticksToDisappear = Math.Min((int)this.DurationPerHour + hcd.ticksToDisappear, this.Props.maxDuration);
            }
        }
        public override void ExtraEffectsStrings(ref StringBuilder stringBuilder)
        {
            if (this.Props.hediff != null)
            {
                string text3 = "HVMP_IdeoBookHediffGain".Translate((this.SeverityPerHour * 10).ToStringByStyle(ToStringStyle.FloatTwo), this.Props.hediff.label, (this.DurationPerHour / 250).ToStringByStyle(ToStringStyle.FloatTwo));
                stringBuilder.AppendLine(" - " + text3);
            }
        }
    }
}
