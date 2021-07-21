using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CM_Semi_Random_Research
{
    [StaticConstructorOnStartup]
    public static class MainButtonsRoot_Patches
    {
        [HarmonyPatch(typeof(MainButtonsRoot))]
        [HarmonyPatch(MethodType.Constructor)]
        public static class MainButtonsRoot_Constructor
        {
            [HarmonyPostfix]
            public static void Postfix(ref List<MainButtonDef> ___allButtonsInOrder)
            {
                if (___allButtonsInOrder != null)
                {
                    if (SemiRandomResearchMod.settings.featureEnabled)
                        ___allButtonsInOrder = ___allButtonsInOrder.Where(button => button != MainButtonDefOf.Research).ToList();
                    else
                        ___allButtonsInOrder = ___allButtonsInOrder.Where(button => button != SemiRandomResearchDefOf.CM_Semi_Random_Research_MainButton_Next_Research).ToList();
                }
            }
        }
    }
}
