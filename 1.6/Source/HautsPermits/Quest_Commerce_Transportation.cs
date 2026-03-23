using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace HautsPermits
{
    /*transport2: Neither Rain Nor Snow Nor (Heat Nor Gloom of Night Stays the Player From the Swift Completion of Their Assigned Quest)
     * -makes the appropriate distance range at which the site can spawn valueIfNRNSN
     * -stores NRNSN_timeValue in NRNSN_timeName, which is later deducted from the amount of time you have to complete the quest*/
    public class QuestNode_NRNSN : QuestNode
    {
        public bool NRNSN_Enabled()
        {
            return BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.transport2, HVMP_Mod.settings.transportX);
        }
        protected override bool TestRunInt(Slate slate)
        {
            bool goNRNSN = this.NRNSN_Enabled();
            this.SetVars(slate, goNRNSN);
            if (goNRNSN)
            {
                this.SetVars2(slate);
            } else {
                slate.Set<object>(this.NRNSN_timeName.GetValue(slate), 0, false);
            }
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            bool goNRNSN = this.NRNSN_Enabled();
            this.SetVars(slate, goNRNSN);
            if (goNRNSN)
            {
                this.SetVars2(slate);
            } else {
                slate.Set<object>(this.NRNSN_timeName.GetValue(slate), 0, false);
            }
        }
        private void SetVars(Slate slate, bool goNRNSN)
        {
            object obj = (goNRNSN ? valueIfNRNSN : value).GetValue(slate);
            if (this.convertTo.GetValue(slate) != null)
            {
                obj = ConvertHelper.Convert(obj, this.convertTo.GetValue(slate));
            }
            slate.Set<object>(this.name.GetValue(slate), obj, false);
        }
        private void SetVars2(Slate slate)
        {
            object obj = this.NRNSN_timeValue.GetValue(slate);
            if (this.convertTo.GetValue(slate) != null)
            {
                obj = ConvertHelper.Convert(obj, this.convertTo.GetValue(slate));
            }
            slate.Set<object>(this.NRNSN_timeName.GetValue(slate), obj, false);
        }
        [NoTranslate]
        public SlateRef<string> name;
        public SlateRef<object> value;
        public SlateRef<object> valueIfNRNSN;
        public SlateRef<Type> convertTo;
        [NoTranslate]
        public SlateRef<string> NRNSN_timeName;
        public SlateRef<object> NRNSN_timeValue;
    }
    /*This generates the world object, determines what the trade goods being requested are, and handles the other two mutators. huh, these mutators are out of alphabetical order. Well, whatever.
     * transport1: Pukis adds an ambush to the world site (by giving it a nonzero PUKIS_points, based on PUKIS_pointsFactor).
     * transport3: That Gucci That Louis sets requestedMinQuality to a potentially different value than Normal. Each QualityCategory key in TGTL_qualityChances has its corresponding value as its weighting*/
    public class QuestNode_PUKIS_TGTL : QuestNode
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
            if (worldObject is WorldObject_DeadDrop wodd)
            {
                bool mayhemMode = HVMP_Mod.settings.transportX;
                wodd.TryGetComponent<DeadDropComp>(out DeadDropComp ddc);
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.transport1, mayhemMode))
                {
                    Map map = QuestGen.slate.Get<Map>("map", null, false) ?? Find.AnyPlayerHomeMap;
                    wodd.PUKIS_points = (int)(this.PUKIS_pointsFactor * StorytellerUtility.DefaultThreatPointsNow(map));
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_PUKIS_info", this.PUKIS_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_PUKIS_info", " ") });
                }
                if (ddc != null)
                {
                    QuestPart_SetupDeadDrop questPart_InitiateTradeRequest = new QuestPart_SetupDeadDrop
                    {
                        settlement = wodd,
                        requestedThingDef = this.requestedThingDef.GetValue(slate),
                        requestedCount = this.requestedThingCount.GetValue(slate),
                        requestDuration = this.duration.GetValue(slate),
                        keepAfterQuestEnds = false,
                        inSignal = slate.Get<string>("inSignal", null, false)
                    };
                    QuestGen.quest.AddPart(questPart_InitiateTradeRequest);
                    if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.transport3, mayhemMode))
                    {
                        if (ddc != null)
                        {
                            ddc.requestedMinQuality = this.TGTL_qualityChances.RandomElementByWeight((KeyValuePair<QualityCategory, float> kvp) => kvp.Value).Key;
                            if (ddc.requestedMinQuality < QualityCategory.Normal)
                            {
                                ddc.requestedMinQuality = QualityCategory.Normal;
                            }
                        }
                    }
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("requestedQualityLabel", ddc.requestedMinQuality.GetLabel())
                    });
                }
            }
            if (this.storeAs.GetValue(slate) != null)
            {
                QuestGen.slate.Set<WorldObject>(this.storeAs.GetValue(slate), worldObject, false);
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return this.requestedThingCount.GetValue(slate) > 0 && this.requestedThingDef.GetValue(slate) != null;
        }
        public SlateRef<WorldObjectDef> def;
        public SlateRef<PlanetTile> tile;
        public SlateRef<Faction> faction;
        [NoTranslate]
        public SlateRef<string> storeAs;
        public SlateRef<ThingDef> requestedThingDef;
        public SlateRef<int> requestedThingCount;
        public SlateRef<int> duration;
        public float PUKIS_pointsFactor;
        [MustTranslate]
        public string PUKIS_description;
        public Dictionary<QualityCategory, float> TGTL_qualityChances;
    }
    //in addition to finalizing details of the world object's DeadDropComp, manages the quest's hyperlinks to the world site and the requested thing's def
    public class QuestPart_SetupDeadDrop : QuestPart
    {
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                if (this.settlement != null)
                {
                    yield return this.settlement;
                }
                yield break;
            }
        }
        public override IEnumerable<Dialog_InfoCard.Hyperlink> Hyperlinks
        {
            get
            {
                foreach (Dialog_InfoCard.Hyperlink hyperlink in base.Hyperlinks)
                {
                    yield return hyperlink;
                }
                yield return new Dialog_InfoCard.Hyperlink(this.requestedThingDef, -1);
                yield break;
            }
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                DeadDropComp component = this.settlement.GetComponent<DeadDropComp>();
                if (component != null)
                {
                    component.requestThingDef = this.requestedThingDef;
                    component.requestCount = this.requestedCount;
                    component.expiration = Find.TickManager.TicksGame + this.requestDuration;
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<WorldObject_DeadDrop>(ref this.settlement, "settlement", false);
            Scribe_Defs.Look<ThingDef>(ref this.requestedThingDef, "requestedThingDef");
            Scribe_Values.Look<int>(ref this.requestedCount, "requestedCount", 0, false);
            Scribe_Values.Look<int>(ref this.requestDuration, "requestDuration", 0, false);
            Scribe_Values.Look<bool>(ref this.keepAfterQuestEnds, "keepAfterQuestEnds", false, false);
        }
        public string inSignal;
        public WorldObject_DeadDrop settlement;
        public ThingDef requestedThingDef;
        public int requestedCount;
        public int requestDuration;
        public bool keepAfterQuestEnds;
    }
    //if PUKIS_points was set to a positive value, arriving at this causes an ambush; a caravan within 6 tiles of the site can randomly provoke the ambush early instead
    public class WorldObject_DeadDrop : WorldObject
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
        public void Notify_CaravanArrived(Caravan caravan)
        {
            if (this.PUKIS_points > 0)
            {
                this.Ambush(caravan);
            }
        }
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (this.PUKIS_points > 0 && this.IsHashIntervalTick(250, delta) && Rand.Chance(0.05f))
            {
                foreach (Caravan c in Find.WorldObjects.Caravans.InRandomOrder())
                {
                    if (c.Tile != null && this.Tile != null && Find.WorldGrid.TraversalDistanceBetween(c.Tile, this.Tile, true) <= 6f)
                    {
                        this.Ambush(c);
                    }
                }
            }
        }
        public void Ambush(Caravan target)
        {
			BranchQuestSetupUtility.DoAmbush(target, this.PUKIS_points);
            this.PUKIS_points = -1;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.PUKIS_points, "PUKIS_points", -1, false);
        }
        private Material cachedMat;
        public int PUKIS_points;
    }
    //handles the UI showing what you need to bring, and also handles the actual taking of those things which results in quest success
    public class WorldObjectCompProperties_DeadDrop : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_DeadDrop()
        {
            this.compClass = typeof(DeadDropComp);
        }
    }
    [StaticConstructorOnStartup]
    public class DeadDropComp : WorldObjectComp
    {
        public bool ActiveRequest
        {
            get
            {
                return this.expiration > Find.TickManager.TicksGame;
            }
        }
        public override string CompInspectStringExtra()
        {
            if (this.ActiveRequest)
            {
                return "CaravanRequestInfo".Translate(this.RequestedThingLabel(this.requestThingDef, this.requestCount).CapitalizeFirst(), (this.expiration - Find.TickManager.TicksGame).ToStringTicksToDays("F1"), (this.requestThingDef.GetStatValueAbstract(StatDefOf.MarketValue, null) * (float)this.requestCount).ToStringMoney(null));
            }
            return null;
        }
        public string RequestedThingLabel(ThingDef def, int count)
        {
            string text = GenLabel.ThingLabel(def, null, count);
            if (def.HasComp(typeof(CompQuality)))
            {
                text += " (" + "HVMP_QualOrBetter".Translate(this.requestedMinQuality.GetLabel().CapitalizeFirst()) + ")";
            }
            if (def.IsApparel)
            {
                text += " (" + "NotTainted".Translate() + ")";
            }
            return text;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            if (this.ActiveRequest && this.DDVisitedNow(caravan) == this.parent)
            {
                yield return this.FulfillRequestCommand(caravan);
            }
            yield break;
        }
        public WorldObject_DeadDrop DDVisitedNow(Caravan caravan)
        {
            if (!caravan.Spawned || caravan.pather.Moving)
            {
                return null;
            }
            List<WorldObject> sites = Find.WorldObjects.ObjectsAt(caravan.Tile).ToList();
            foreach (WorldObject wo in sites)
            {
                if (wo == this.parent && this.parent is WorldObject_DeadDrop wodd)
                {
                    return wodd;
                }
            }
            return null;
        }
        private Command FulfillRequestCommand(Caravan caravan)
        {
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "CommandFulfillTradeOffer".Translate();
            command_Action.defaultDesc = "CommandFulfillTradeOfferDesc".Translate();
            command_Action.icon = DeadDropComp.TradeCommandTex;
            command_Action.action = delegate
            {
                if (!this.ActiveRequest)
                {
                    Log.Error("Attempted to fulfill an unavailable request");
                    return;
                }
                if (!CaravanInventoryUtility.HasThings(caravan, this.requestThingDef, this.requestCount, new Func<Thing, bool>(this.PlayerCanGive)))
                {
                    Messages.Message("CommandFulfillTradeOfferFailInsufficient".Translate(this.RequestedThingLabel(this.requestThingDef, this.requestCount)), MessageTypeDefOf.RejectInput, false);
                    return;
                }
                WindowStack windowStack = Find.WindowStack;
                TaggedString taggedString = "CommandFulfillTradeOfferConfirm".Translate(GenLabel.ThingLabel(this.requestThingDef, null, this.requestCount));
                Action action = delegate
                {
                    this.Fulfill(caravan);
                };
                windowStack.Add(Dialog_MessageBox.CreateConfirmation(taggedString, action, false, null, WindowLayer.Dialog));
            };
            if (!CaravanInventoryUtility.HasThings(caravan, this.requestThingDef, this.requestCount, new Func<Thing, bool>(this.PlayerCanGive)))
            {
                command_Action.Disable("CommandFulfillTradeOfferFailInsufficient".Translate(this.RequestedThingLabel(this.requestThingDef, this.requestCount)));
            }
            return command_Action;
        }
        private void Fulfill(Caravan caravan)
        {
            int remaining = this.requestCount;
            List<Thing> list = CaravanInventoryUtility.TakeThings(caravan, delegate (Thing thing)
            {
                if (this.requestThingDef != thing.def)
                {
                    return 0;
                }
                if (!this.PlayerCanGive(thing))
                {
                    return 0;
                }
                int num = Mathf.Min(remaining, thing.stackCount);
                remaining -= num;
                return num;
            });
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Destroy(DestroyMode.Vanish);
            }
            QuestUtility.SendQuestTargetSignals(this.parent.questTags, "TradeRequestFulfilled", this.parent.Named("SUBJECT"), caravan.Named("CARAVAN"));
            if (this.parent is WorldObject_DeadDrop wodd)
            {
                wodd.Notify_CaravanArrived(caravan);
            }
            this.parent.Destroy();
        }
        private bool PlayerCanGive(Thing thing)
        {
            if (thing.GetRotStage() != RotStage.Fresh)
            {
                return false;
            }
            Apparel apparel = thing as Apparel;
            if (apparel != null && apparel.WornByCorpse)
            {
                return false;
            }
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            return compQuality == null || compQuality.Quality >= this.requestedMinQuality;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look<ThingDef>(ref this.requestThingDef, "requestThingDef");
            Scribe_Values.Look<int>(ref this.requestCount, "requestCount", 0, false);
            Scribe_Values.Look<QualityCategory>(ref this.requestedMinQuality, "requestedMinQuality", QualityCategory.Normal, false);
            Scribe_Values.Look<int>(ref this.expiration, "expiration", 0, false);
            BackCompatibility.PostExposeData(this);
        }
        public ThingDef requestThingDef;
        public QualityCategory requestedMinQuality = QualityCategory.Normal;
        public int requestCount;
        public int expiration = -1;
        public string outSignalFulfilled;
        private static readonly Texture2D TradeCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/FulfillTradeRequest", true);
    }
}
