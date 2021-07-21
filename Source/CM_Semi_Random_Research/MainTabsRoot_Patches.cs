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
    public static class MainTabsRoot_Patches
    {
        [HarmonyPatch(typeof(MainTabsRoot))]
        [HarmonyPatch("SetCurrentTab", MethodType.Normal)]
        public static class MainTabsRoot_SetCurrentTab
        {
            [HarmonyPrefix]
            public static void Prefix(ref MainButtonDef tab)
            {
                if (tab == null)
                    return;

                if (tab == MainButtonDefOf.Research && 
                   (SemiRandomResearchMod.settings.featureEnabled && DiaOption_Patches.DiaOption_FinishProject.finishingProject))
                    tab = SemiRandomResearchDefOf.CM_Semi_Random_Research_MainButton_Next_Research;
            }
        }
    }
}
