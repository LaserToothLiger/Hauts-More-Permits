using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsPermits
{
    //using this item creates a menu from which you can choose to apply one game condition
    public class CompProperties_UseEffect_RegionalDisruptorMenu : CompProperties_UseEffect
    {
        public CompProperties_UseEffect_RegionalDisruptorMenu()
        {
            this.compClass = typeof(CompUseEffect_RegionalDisruptorMenu);
        }
        public string openingLabel = "HVMP_DisruptorFiringLabel";
        public int duration;
        public List<GameConditionDef> gameConditions;
        public SoundDef sound;
    }
    public class CompUseEffect_RegionalDisruptorMenu : CompUseEffect
    {
        public CompProperties_UseEffect_RegionalDisruptorMenu Props
        {
            get
            {
                return (CompProperties_UseEffect_RegionalDisruptorMenu)this.props;
            }
        }
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            if (usedBy.Spawned)
            {
                if (!usedBy.IsPlayerControlled)
                {
                    CompUseEffect_RegionalDisruptorMenu.GenerateCondition(usedBy, this.parent, this.Props.duration, this.Props.gameConditions.RandomElement(), this.Props.sound);
                    return;
                }
                RegionalDisruptorWindow window = new RegionalDisruptorWindow(usedBy, this);
                Find.WindowStack.Add(window);
            }
        }
        public static void GenerateCondition(Pawn pawn, Thing disruptor, int duration, GameConditionDef gcd, SoundDef sound)
        {
            if (disruptor.Map != null)
            {
                GameCondition gameCondition = GameConditionMaker.MakeCondition(gcd, -1);
                gameCondition.Duration = duration;
                disruptor.Map.GameConditionManager.RegisterCondition(gameCondition);
                if (sound != null)
                {
                    sound.PlayOneShot(new TargetInfo(disruptor.Position, disruptor.Map, false));
                }
            }
            disruptor.SplitOff(1).Destroy();
        }
    }
    public class RegionalDisruptorWindow : Window
    {
        public RegionalDisruptorWindow(Pawn pawn, CompUseEffect_RegionalDisruptorMenu disruptorComp)
        {
            this.pawn = pawn;
            this.options.Clear();
            this.duration = disruptorComp.Props.duration;
            this.disruptor = disruptorComp.parent;
            this.openingLabel = disruptorComp.Props.openingLabel;
            this.options = disruptorComp.Props.gameConditions;
            this.sound = disruptorComp.Props.sound;
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
            Widgets.Label(0f, ref num, viewRect.width, this.openingLabel.Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), default(TipSignal));
            num += 14f;
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect rect = new Rect(0f, num, inRect.width - 30f, 99999f);
            listing_Standard.Begin(rect);
            foreach (GameConditionDef gcd in this.options)
            {
                bool flag = this.chosenGCD == gcd;
                bool flag2 = flag;
                string descString = gcd.LabelCap;
                listing_Standard.CheckboxLabeled(descString, ref flag, gcd.description);
                if (flag != flag2)
                {
                    if (flag)
                    {
                        this.chosenGCD = gcd;
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
                    CompUseEffect_RegionalDisruptorMenu.GenerateCondition(this.pawn, this.disruptor, this.duration, this.chosenGCD, this.sound);
                    this.Close(true);
                } else {
                    Messages.Message(acceptanceReport.Reason, null, MessageTypeDefOf.RejectInput, false);
                }
            }
        }
        private AcceptanceReport CanClose()
        {
            if (this.chosenGCD == null)
            {
                return "HVMP_Choose".Translate();
            }
            return AcceptanceReport.WasAccepted;
        }
        private Pawn pawn;
        private string openingLabel;
        private Thing disruptor;
        private GameConditionDef chosenGCD = null;
        private float scrollHeight;
        private Vector2 scrollPosition;
        private int duration;
        private SoundDef sound;
        private List<GameConditionDef> options = new List<GameConditionDef>();
    }

    //it's a solar flare/emi dynamo, but it also stuns buildings for an hour when it starts out
    public class GameCondition_DisableElectricityPlusBuildingStun : GameCondition_DisableElectricity
    {
        public override void GameConditionTick()
        {
            base.GameConditionTick();
            if (!this.doneStunYet)
            {
                foreach (Map m in this.AffectedMaps)
                {
                    foreach (Building b in m.listerBuildings.allBuildingsNonColonist)
                    {
                        if (b.Faction == null || b.Faction.RelationKindWith(Faction.OfPlayerSilentFail) == FactionRelationKind.Hostile)
                        {
                            CompStunnable stunComp = b.GetComp<CompStunnable>();
                            if (stunComp != null)
                            {
                                stunComp.StunHandler.StunFor(2500, null, false);
                            }
                        }
                    }
                }
                this.doneStunYet = true;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.doneStunYet, "doneStunYet", false, false);
        }
        public bool doneStunYet;
    }
}
