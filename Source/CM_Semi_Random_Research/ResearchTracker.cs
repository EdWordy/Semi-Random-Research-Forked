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
    public class ResearchTracker : WorldComponent
    {
        private List<ResearchProjectDef> currentAvailableProjects = new List<ResearchProjectDef>();
        private ResearchProjectDef currentProject = null;

        public ResearchProjectDef CurrentProject => currentProject;

        public bool autoResearch = false;

        private bool rerolled = false;

        public bool CanReroll => (SemiRandomResearchMod.settings.allowManualReroll == ManualReroll.Always || (SemiRandomResearchMod.settings.allowManualReroll == ManualReroll.Once && !rerolled));

        public ResearchTracker(World world) : base(world)
        {

        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref currentAvailableProjects, "currentAvailableProjects", LookMode.Def);
            Scribe_Defs.Look(ref currentProject, "currentProject");
            Scribe_Values.Look(ref autoResearch, "autoResearch", false);
            Scribe_Values.Look(ref rerolled, "rerolled", false);
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            if (currentProject != null && currentProject.IsFinished)
                rerolled = false;

            if (currentProject == null || currentProject.IsFinished)
            {
                if (autoResearch)
                    SetCurrentProject(GetCurrentlyAvailableProjects().FirstOrDefault());
                else if (currentProject != null && currentProject.IsFinished)
                    SetCurrentProject(null);
            }

            ResearchProjectDef activeProject = Find.ResearchManager.currentProj;

            if (activeProject != currentProject)
            {
                if (!SemiRandomResearchMod.settings.featureEnabled)
                {
                    SetCurrentProject(activeProject);
                }
                else if (currentProject == null && currentAvailableProjects.Contains(activeProject))
                {
                    SetCurrentProject(activeProject);
                }
                else
                {
                    SetCurrentProject(currentProject);
                }
            }
        }

        public List<ResearchProjectDef> GetCurrentlyAvailableProjects()
        {
            currentAvailableProjects = currentAvailableProjects.Where(projectDef => !projectDef.IsFinished).ToList();

            if (!SemiRandomResearchMod.settings.rerollAllEveryTime || currentProject == null || currentProject.IsFinished)
            {
                //Log.Message("Replacing missing projects. reroll: " + SemiRandomResearchMod.settings.rerollAllEveryTime.ToString() + " - currentProject null: " + (currentProject == null).ToString());
                int numberOfMissingProjects = SemiRandomResearchMod.settings.availableProjectCount - currentAvailableProjects.Count;
                if (numberOfMissingProjects > 0)
                {
                    TechLevel maxTechLevel = TechLevel.Archotech;
                    // If setting is enabled, block techs beyond player faction's tech level
                    if (SemiRandomResearchMod.settings.restrictToFactionTechLevel)
                        maxTechLevel = Faction.OfPlayer.def.techLevel;

                    List<ResearchProjectDef> allAvailableProjects = DefDatabase<ResearchProjectDef>.AllDefsListForReading
                        .Where((ResearchProjectDef projectDef) => !currentAvailableProjects.Contains(projectDef) && projectDef.techLevel <= maxTechLevel && projectDef.CanStartProject()).ToList();

                    // Force completing lowest level if setting is enabled
                    if (SemiRandomResearchMod.settings.forceLowestTechLevel && allAvailableProjects.Count > 0)
                    {
                        // Need to include the tech level of projects already in the offered list
                        if (currentAvailableProjects.Count > 0)
                            maxTechLevel = currentAvailableProjects.Select(projectDef => projectDef.techLevel).Max();

                        allAvailableProjects = allAvailableProjects.Where(projectDef => projectDef.techLevel <= maxTechLevel).ToList();

                        // Go through each tech level and select from lowest available
                        for (TechLevel techLevel = TechLevel.Animal; techLevel <= maxTechLevel; ++techLevel)
                        {
                            List<ResearchProjectDef> projectsAtTechLevel = allAvailableProjects.Where(projectDef => projectDef.techLevel <= techLevel).ToList();
                            if (projectsAtTechLevel.Count > 0)
                            {
                                allAvailableProjects = projectsAtTechLevel;
                                break;
                            }
                        }
                    }

                    if (allAvailableProjects.Count > 0)
                    {
                        allAvailableProjects.Shuffle();
                        currentAvailableProjects.AddRange(allAvailableProjects.Take(Math.Min(numberOfMissingProjects, allAvailableProjects.Count)).ToList());
                    }
                }
            }

            return new List<ResearchProjectDef> (currentAvailableProjects);
        }

        public void SetCurrentProject(ResearchProjectDef newCurrentProject)
        {
            //Log.Message("SetCurrentProject: " + ((newCurrentProject == null) ? "null" : newCurrentProject.ToString()));

            currentProject = newCurrentProject;
            Find.ResearchManager.currentProj = currentProject;

            if (currentProject != null && !SemiRandomResearchMod.settings.featureEnabled && !currentAvailableProjects.Contains(currentProject))
                currentAvailableProjects.Add(currentProject);

            if (currentProject != null && SemiRandomResearchMod.settings.rerollAllEveryTime)
                currentAvailableProjects = currentAvailableProjects.Where(projectDef => projectDef == currentProject).ToList();
        }

        public void Reroll()
        {
            rerolled = true;

            currentProject = null;
            Find.ResearchManager.currentProj = null;

            currentAvailableProjects.Clear();
        }
    }
}
