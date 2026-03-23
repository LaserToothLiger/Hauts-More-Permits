using HarmonyLib;
using HautsPermits;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace HautsPermits_Ideology
{

    [StaticConstructorOnStartup]
    public class HautsPermits_Ideology
    {
        private static readonly Type patchType = typeof(HautsPermits_Ideology);
        static HautsPermits_Ideology()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautspermits.ideology");
            //handles the mood-debuffing and Ethnography quest-fulfilling effects of social interactions initiated by Anthropologists
            harmony.Patch(AccessTools.Method(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith)),
                          postfix: new HarmonyMethod(patchType, nameof(HVMP_I_TryInteractWithPostfix)));
        }
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        public static void HVMP_I_TryInteractWithPostfix(Pawn_InteractionsTracker __instance, Pawn recipient, InteractionDef intDef)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_InteractionsTracker), __instance, "pawn") as Pawn;
            if (pawn.kindDef == HVMPDefOf.HVMP_Anthropologist && recipient.needs.mood != null && recipient.IsColonist && !recipient.IsQuestLodger() && recipient.kindDef != HVMPDefOf.HVMP_Anthropologist)
            {
                float val = Rand.Value;
                if (val <= 0.05f)
                {
                    recipient.needs.mood.thoughts.memories.TryGainMemory(HVMPDefOf.HVMP_AnthroAnnoyance, pawn);
                    recipient.needs.mood.thoughts.memories.TryGainMemory(HVMPDefOf.HVMP_AnthroAnnoyance, pawn);
                } else if (val <= 0.3f) {
                    recipient.needs.mood.thoughts.memories.TryGainMemory(HVMPDefOf.HVMP_AnthroAnnoyance, pawn);
                }
                List<Quest> questsListForReading = Find.QuestManager.QuestsListForReading;
                for (int i = 0; i < questsListForReading.Count; i++)
                {
                    if (questsListForReading[i].State == QuestState.Ongoing)
                    {
                        List<QuestPart> partsListForReading = questsListForReading[i].PartsListForReading;
                        for (int j = 0; j < partsListForReading.Count; j++)
                        {
                            if (partsListForReading[j] is QuestPart_ShuttleAnthro qpsa && qpsa.lodgers.Contains(pawn))
                            {
                                qpsa.questionsLeft--;
                                if (qpsa.questionsLeft <= 0 && qpsa.State == QuestPartState.Enabled)
                                {
                                    qpsa.CompleteOnLastQuestion();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
