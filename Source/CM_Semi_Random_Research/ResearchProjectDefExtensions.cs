using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Grammar;

namespace CM_Semi_Random_Research
{
    public static class ResearchProjectDefExtensions
    {
        public static bool CanStartProject(this ResearchProjectDef researchProject)
        {
            if (Find.ResearchManager.currentProj == null && !researchProject.IsFinished && researchProject.PrerequisitesCompleted && researchProject.TechprintRequirementMet)
            {
                if (researchProject.requiredResearchBuilding != null)
                {
                    List<Map> maps = Find.Maps;
                    for (int i = 0; i < maps.Count; i++)
                    {
                        List<Building> allBuildingsColonist = maps[i].listerBuildings.allBuildingsColonist;
                        for (int j = 0; j < allBuildingsColonist.Count; j++)
                        {
                            Building_ResearchBench building_ResearchBench = allBuildingsColonist[j] as Building_ResearchBench;
                            if (building_ResearchBench != null && researchProject.CanBeResearchedAt(building_ResearchBench, ignoreResearchBenchPowerStatus: true))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
