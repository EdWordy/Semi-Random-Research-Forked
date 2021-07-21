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
    public static class DiaOption_Patches
    {
        [HarmonyPatch(typeof(DiaOption))]
        [HarmonyPatch("Activate", MethodType.Normal)]
        public static class DiaOption_FinishProject
        {
            public static bool finishingProject = false;

            [HarmonyPrefix]
            public static void Prefix(string ___text)
            {
                if (___text == "ResearchScreen".Translate())
                    finishingProject = true;
            }

            [HarmonyPostfix]
            public static void Postfix()
            {
                finishingProject = false;
            }
        }
    }
}
