using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HautsPermits
{
    /*when you open a pallet, you can retroactively choose what was inside the box all along.
     * This occurs in a unique window which displays a finite list of choices.
     * The configuration of this list is determined by these fields in the CompProperties.
     * maxNumOptions: the list can only have this many elements. Options are randomly picked from all possible Things meeting ValidateOption() and the various other fields in this CompProperties
     *   Books are specifically never eligible because their title and description generation is a fuck
     * ---thingCategoryWhitelist: if specified, only Things that have this category can show up as options
     * ---allowedTradeTag: if specified, only Things that have this string as a tradeTag can show up as options
     * ---unitMarketValue|MassOutputCap: if positive, only Things whose per-unit market value|mass do not exceed this value can show up as options
     * ---requiredTradeability: if All, any Thing that is Buyable, Sellable, or All can be an option. Otherwise, only Things that match the specified Tradeability can be options
     * Generated options can be stacks (in fact, onlyHasStackableItems PREVENTS Things that don't stack from being options). Stack size is constrained by the following fields
     * ---netMarketValue|MassOutputCap: if positive, the total market value|mass of the stack can't exceed this value
     * ---countCantExceedStackSize: the stack size cannot exceed its def's stackLimit
     * ---netCountOutputCap: the stack size cannot exceed this value
     * qualityGenerator: applied to the option, if possible*/
    public class CompProperties_UseEffect_MultipleChoicePallet : CompProperties_UseEffect
    {
        public CompProperties_UseEffect_MultipleChoicePallet()
        {
            this.compClass = typeof(CompUseEffect_MultipleChoicePallet);
        }
        public ThingCategoryDef thingCategoryWhitelist;
        public string allowedTradeTag;
        public int maxNumOptions = 10;
        public float unitMarketValueOutputCap = -1f;
        public float unitMassOutputCap = -1f;
        public float netMarketValueOutputCap = -1f;
        public float netMassOutputCap = -1f;
        public bool onlyHasStackableItems = true;
        public bool countCantExceedStackSize = true;
        public int netCountOutputCap = 100000;
        public Tradeability requiredTradeability = Tradeability.All;
        public QualityGenerator qualityGenerator = QualityGenerator.BaseGen;
    }
    public class CompUseEffect_MultipleChoicePallet : CompUseEffect
    {
        public CompProperties_UseEffect_MultipleChoicePallet Props
        {
            get
            {
                return (CompProperties_UseEffect_MultipleChoicePallet)this.props;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            if (this.options.NullOrEmpty())
            {
                List<ThingDefCountClass> tdccs = new List<ThingDefCountClass>();
                int mno = this.Props.maxNumOptions;
                ThingCategoryDef category = this.Props.thingCategoryWhitelist;
                if (category != null)
                {
                    foreach (ThingDef td in DefDatabase<ThingDef>.AllDefsListForReading.InRandomOrder())
                    {
                        if (td.thingCategories != null && td.thingCategories.Contains(category) && this.ValidateOption(td))
                        {
                            tdccs.Add(this.CreateTDCC(td));
                            if (mno > 0)
                            {
                                mno--;
                                if (mno <= 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                } else if (this.Props.allowedTradeTag != null) {
                    string tts = this.Props.allowedTradeTag;
                    foreach (ThingDef td in DefDatabase<ThingDef>.AllDefsListForReading.InRandomOrder())
                    {
                        if (td.tradeTags != null && td.tradeTags.Contains(tts) && this.ValidateOption(td))
                        {
                            tdccs.Add(this.CreateTDCC(td));
                            if (mno > 0)
                            {
                                mno--;
                                if (mno <= 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                } else if (mno > 0) {
                    List<ThingDef> usedTds = new List<ThingDef>();
                    while (mno > 0)
                    {
                        ThingDef td = DefDatabase<ThingDef>.AllDefsListForReading.Where((ThingDef thing) => !usedTds.Contains(thing) && this.ValidateOption(thing)).RandomElement();
                        usedTds.Add(td);
                        tdccs.Add(this.CreateTDCC(td));
                        mno--;
                    }
                } else {
                    this.parent.Destroy();
                }
                this.options = tdccs;
            }
        }
        public bool ValidateOption(ThingDef thing)
        {
            return thing.category == ThingCategory.Item && thing.BaseMarketValue > 0f && !thing.HasComp<CompBook>() && (this.Props.unitMarketValueOutputCap <= 0f || thing.BaseMarketValue <= this.Props.unitMarketValueOutputCap) && (this.Props.unitMassOutputCap <= 0f || thing.BaseMass <= this.Props.unitMassOutputCap) && ((this.Props.requiredTradeability == Tradeability.All && thing.tradeability != Tradeability.None) || thing.tradeability == this.Props.requiredTradeability) && (!this.Props.onlyHasStackableItems || thing.stackLimit > 1) && !thing.destroyOnDrop;
        }
        public ThingDefCountClass CreateTDCC(ThingDef td)
        {
            ThingDefCountClass tdcc = new ThingDefCountClass();
            tdcc.thingDef = td;
            float mass = td.BaseMass;
            float mv = td.BaseMarketValue;
            tdcc.count = Math.Max(1, this.Props.netCountOutputCap);
            if (this.Props.netMarketValueOutputCap > 0f)
            {
                int limit = Mathf.FloorToInt(this.Props.netMarketValueOutputCap / mv);
                if (limit < tdcc.count)
                {
                    tdcc.count = limit;
                }
            }
            if (this.Props.netMassOutputCap > 0f)
            {
                int limit = Mathf.FloorToInt(this.Props.netMassOutputCap / mass);
                if (limit < tdcc.count)
                {
                    tdcc.count = limit;
                }
            }
            if (!this.Props.countCantExceedStackSize && tdcc.count > td.stackLimit)
            {
                tdcc.count = td.stackLimit;
            }
            tdcc.count = Math.Max(1, tdcc.count);
            tdcc.quality = QualityUtility.GenerateQuality(this.Props.qualityGenerator);
            if (td.MadeFromStuff)
            {
                tdcc.stuff = GenStuff.RandomStuffByCommonalityFor(td, TechLevel.Undefined);
            }
            return tdcc;
        }
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            if (usedBy.Spawned)
            {
                if (this.options.NullOrEmpty())
                {
                    Log.Error("there were no valid items, somehow");
                    this.parent.Destroy();
                    return;
                }
                if (!usedBy.IsPlayerControlled)
                {
                    CompUseEffect_MultipleChoicePallet.OpenPallet(usedBy, this.parent, this.options.RandomElement());
                }
                MultipleChoicePalletWindow window = new MultipleChoicePalletWindow(usedBy, this);
                Find.WindowStack.Add(window);
            }
        }
        public static void OpenPallet(Pawn pawn, Thing pallet, ThingDefCountClass chosenTDCC)
        {
            Thing thing = ThingMaker.MakeThing(chosenTDCC.thingDef, chosenTDCC.stuff);
            thing.stackCount = chosenTDCC.count;
            if (thing.TryGetComp(out CompQuality compQuality))
            {
                compQuality.SetQuality(chosenTDCC.quality, new ArtGenerationContext?(ArtGenerationContext.Outsider));
            }
            if (chosenTDCC.color != null && thing.TryGetComp(out CompColorable compColorable))
            {
                compColorable.SetColor(chosenTDCC.color.Value);
            }
            if (thing is Building b)
            {
                b.MakeMinified();
            }
            GenDrop.TryDropSpawn(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, out Thing theThing, null, null, true);
            thing.Notify_DebugSpawned();
            pallet.SplitOff(1).Destroy();
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look<ThingDefCountClass>(ref this.options, "options", LookMode.Deep, Array.Empty<object>());
        }
        public List<ThingDefCountClass> options;
    }
    public class MultipleChoicePalletWindow : Window
    {
        public MultipleChoicePalletWindow(Pawn pawn, CompUseEffect_MultipleChoicePallet palletComp)
        {
            this.pawn = pawn;
            this.options.Clear();
            this.pallet = palletComp.parent;
            this.options = palletComp.options;
        }
        public override void PreOpen()
        {
            base.PreOpen();
            this.forcePause = true;
        }
        private float Height
        {
            get
            {
                return CharacterCardUtility.PawnCardSize(this.pawn).y + Window.CloseButSize.y + 4f + this.Margin * 2f;
            }
        }
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(500f, this.Height);
            }
        }
        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMax -= 4f + Window.CloseButSize.y;
            Text.Font = GameFont.Small;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width * 0.7f, this.scrollHeight);
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect, true);
            float num = 0f;
            Widgets.Label(0f, ref num, viewRect.width, "HVMP_PalletOpeningLabel".Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), default(TipSignal));
            num += 14f;
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect rect = new Rect(0f, num, inRect.width - 30f, 99999f);
            listing_Standard.Begin(rect);
            foreach (ThingDefCountClass tdcc in this.options)
            {
                bool flag = this.chosenTDCC == tdcc;
                bool flag2 = flag;
                string descString = tdcc.count + "x ";
                if (tdcc.thingDef.MadeFromStuff)
                {
                    descString += tdcc.stuff.label + " ";
                }
                descString += tdcc.thingDef.label + " ";
                if (tdcc.thingDef.HasComp<CompQuality>())
                {
                    descString += "(" + tdcc.quality + ")";
                }
                listing_Standard.CheckboxLabeled(descString, ref flag, null);
                if (flag != flag2)
                {
                    if (flag)
                    {
                        this.chosenTDCC = tdcc;
                    }
                }
            }
            listing_Standard.End();
            num += listing_Standard.CurHeight + 10f + 4f;
            if (Event.current.type == EventType.Layout)
            {
                this.scrollHeight = Mathf.Max(num, inRect.height);
            }
            Widgets.EndScrollView();
            Rect rect2 = new Rect(0f, inRect.yMax + 4f, inRect.width, Window.CloseButSize.y);
            AcceptanceReport acceptanceReport = this.CanClose();
            if (!acceptanceReport.Accepted)
            {
                TextAnchor anchor = Text.Anchor;
                GameFont font = Text.Font;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleRight;
                Rect rect3 = rect;
                rect3.xMax = rect2.xMin - 4f;
                Widgets.Label(rect3, acceptanceReport.Reason.Colorize(ColoredText.WarningColor));
                Text.Font = font;
                Text.Anchor = anchor;
            }
            if (Widgets.ButtonText(rect2, "OK".Translate(), true, true, true, null))
            {
                if (acceptanceReport.Accepted)
                {
                    CompUseEffect_MultipleChoicePallet.OpenPallet(this.pawn, this.pallet, chosenTDCC);
                    this.Close(true);
                }
                else
                {
                    Messages.Message(acceptanceReport.Reason, null, MessageTypeDefOf.RejectInput, false);
                }
            }
        }
        private AcceptanceReport CanClose()
        {
            if (this.chosenTDCC == null)
            {
                return "HVMP_Choose".Translate();
            }
            return AcceptanceReport.WasAccepted;
        }
        private Pawn pawn;
        private Thing pallet;
        private ThingDefCountClass chosenTDCC = null;
        private float scrollHeight;
        private Vector2 scrollPosition;
        private List<ThingDefCountClass> options = new List<ThingDefCountClass>();
    }
}
