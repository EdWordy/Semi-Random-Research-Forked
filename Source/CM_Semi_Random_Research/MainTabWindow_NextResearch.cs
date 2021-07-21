using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace CM_Semi_Random_Research
{
    [StaticConstructorOnStartup]
    public class MainTabWindow_NextResearch : MainTabWindow
    {
        protected ResearchProjectDef selectedProject;

        protected override float Margin => 6f;

        private float betweenColumnSpace => 24f;

        private Vector2 leftScrollPosition = Vector2.zero;

        private float leftScrollViewHeight;

        private Vector2 rightScrollPosition = Vector2.zero;

        private float rightScrollViewHeight;

        private static readonly Color FulfilledPrerequisiteColor = Color.green;

        private static readonly Texture2D ResearchBarFillTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.8f, 0.85f));

        private static readonly Texture2D ResearchBarBGTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f));

        private static readonly Texture2D ResearchButtonIcon = ContentFinder<Texture2D>.Get("UI/Buttons/MainButtons/CM_Semi_Random_Research_ResearchTree");

        private Dictionary<ResearchProjectDef, List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>>> cachedUnlockedDefsGroupedByPrerequisites;

        private static List<Building> tmpAllBuildings = new List<Building>();

        private int currentRandomSeed = 0;

        private bool ColonistsHaveResearchBench
        {
            get
            {
                bool result = false;
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    if (maps[i].listerBuildings.ColonistsHaveResearchBench())
                    {
                        result = true;
                        break;
                    }
                }
                return result;
            }
        }

        public override Vector2 InitialSize => new Vector2(900f, 700f);

        public List<ResearchProjectDef> currentAvailableProjects = new List<ResearchProjectDef>();

        public MainTabWindow_NextResearch()
        {

        }

        public override void PreOpen()
        {
            base.PreOpen();

            currentRandomSeed = Rand.Int;

            ResearchTracker researchTracker = Current.Game.World.GetComponent<ResearchTracker>();

            if (researchTracker != null)
            {
                currentAvailableProjects = researchTracker.GetCurrentlyAvailableProjects();
                selectedProject = researchTracker.CurrentProject;
            }

            cachedUnlockedDefsGroupedByPrerequisites = null;
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();

            ResearchTracker researchTracker = Current.Game.World.GetComponent<ResearchTracker>();

            if (researchTracker != null)
            {
                currentAvailableProjects = researchTracker.GetCurrentlyAvailableProjects();
                if (!currentAvailableProjects.Contains(selectedProject))
                    selectedProject = researchTracker.CurrentProject;
            }
        }

        public override void DoWindowContents(Rect rect)
        {

            SetInitialSizeAndPosition();

            float columnWidth = ((rect.width - ((Margin * 2) + betweenColumnSpace)) * 0.5f);
            float columnHeight = rect.height - (Margin * 2);

            Rect leftOutRect = new Rect(Margin,
                                        Margin,
                                        columnWidth,
                                        columnHeight);

            Rect rightOutRect = new Rect(leftOutRect.xMax + betweenColumnSpace,
                                         Margin,
                                         columnWidth,
                                         columnHeight);

            DrawLeftColumn(leftOutRect);
            DrawRightColumn(rightOutRect);
        }

        private void DrawLeftColumn(Rect leftRect)
        {
            ResearchTracker researchTracker = Current.Game.World.GetComponent<ResearchTracker>();

            Rect position = leftRect;
            GUI.BeginGroup(position);

            float currentY = 0f;
            float mainLabelHeight = 50.0f;
            float gapHeight = 10.0f;
            float researchProjectGapHeight = 32.0f;
            float buttonHeight = 94.0f;

            float rerollButtonHeight = 40.0f;

            float footerHeight = (gapHeight + rerollButtonHeight);

            if (researchTracker != null && researchTracker.CanReroll)
                footerHeight += (gapHeight + rerollButtonHeight);

            // Selected project name
            Text.Font = GameFont.Medium;
            GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
            Rect mainLabelRect = new Rect(0f, currentY, position.width, mainLabelHeight);
            Widgets.LabelCacheHeight(ref mainLabelRect, "CM_Semi_Random_Research_Available_Projects".Translate());
            GenUI.ResetLabelAlign();
            currentY += mainLabelHeight;

            Rect scrollOutRect = new Rect(0f, currentY, position.width, position.height - (footerHeight + currentY));
            Rect scrollViewRect = new Rect(0f, currentY, scrollOutRect.width - 16f, leftScrollViewHeight);

            Widgets.BeginScrollView(scrollOutRect, ref leftScrollPosition, scrollViewRect);

            foreach (ResearchProjectDef projectDef in currentAvailableProjects)
            {
                Rect buttonRect = new Rect(0f, currentY, scrollViewRect.width, buttonHeight);
                DrawResearchButton(ref buttonRect, projectDef);
                currentY += buttonHeight + researchProjectGapHeight;
            }

            currentY = (leftScrollViewHeight = currentY + 3f);

            Widgets.EndScrollView();

            if (researchTracker != null)
            {
                Widgets.DrawLineHorizontal(leftRect.xMin, scrollOutRect.yMax + gapHeight, position.width);
                Rect autoResearchCheckRect = new Rect(0f, scrollOutRect.yMax + gapHeight + gapHeight, position.width, 0f);
                TaggedString translatedAutoResearchString = "CM_Semi_Random_Research_Auto_Research_Label".Translate();
                Widgets.LabelCacheHeight(ref autoResearchCheckRect, translatedAutoResearchString);
                Widgets.CheckboxLabeled(autoResearchCheckRect, translatedAutoResearchString, ref researchTracker.autoResearch);

                if (researchTracker.CanReroll)
                {
                    Rect rerollButtonRect = new Rect(0f, autoResearchCheckRect.yMax + gapHeight, position.width, rerollButtonHeight);
                    if (Widgets.ButtonText(rerollButtonRect, "CM_Semi_Random_Research_Reroll_Label".Translate()))
                    {
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        researchTracker.Reroll();
                    }
                }
            }

            DrawGoToTechTreeButton(position);

            GUI.EndGroup();
        }

        private void DrawResearchButton(ref Rect drawRect, ResearchProjectDef projectDef)
        {
            float iconSize = 64.0f;
            float innerMargin = Margin;

            // Remember starting text settings
            TextAnchor startingTextAnchor = Text.Anchor;
            Color startingGuiColor = GUI.color;
            Text.Font = GameFont.Small;


            // Measure everything
            Rect textRect = drawRect;
            TaggedString projectLabel = projectDef.LabelCap;
            Widgets.LabelCacheHeight(ref textRect, projectLabel, false);
            textRect.height = textRect.height + innerMargin + innerMargin;

            Rect iconRect = new Rect(drawRect.x, drawRect.y + textRect.height - 1, iconSize + innerMargin, iconSize + innerMargin);
            drawRect.height = textRect.height + iconRect.height - 1;

            Rect detailsRect = new Rect(drawRect.x + iconRect.width + innerMargin,
                                        drawRect.y + textRect.height + innerMargin,
                                        drawRect.width - (iconRect.width + innerMargin + innerMargin),
                                        drawRect.height - (textRect.height + innerMargin + innerMargin));

            // Set colors
            Color backgroundColor = default(Color);
            Color textColor = Widgets.NormalOptionColor;
            Color borderColor = default(Color);

            if (projectDef == Find.ResearchManager.currentProj)
            {
                backgroundColor = TexUI.ActiveResearchColor;
            }
            else
            {
                backgroundColor = TexUI.AvailResearchColor;
            }

            if (selectedProject == projectDef)
            {
                backgroundColor += TexUI.HighlightBgResearchColor;
                borderColor = TexUI.HighlightBorderResearchColor;
            }
            else
            {
                borderColor = TexUI.DefaultBorderResearchColor;
            }


            // Main button background and border
            Rect buttonRect = drawRect;
            if (Widgets.CustomButtonText(ref buttonRect, "", backgroundColor, textColor, borderColor))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                selectedProject = projectDef;
            }

            DrawBorderedBox(textRect, backgroundColor, borderColor);
            DrawBorderedBox(iconRect, backgroundColor, borderColor);

            // Text
            //   Shrink text box to allow for margin
            textRect.width = textRect.width - (innerMargin * 2);
            textRect.center = buttonRect.center;
            textRect.y = buttonRect.y;

            //   Draw project name
            GUI.color = textColor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(textRect, projectLabel);
            //   Draw project cost
            GUI.color = textColor;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(textRect, projectDef.CostApparent.ToString());

            
            // Icon
            //   Shrink icon box to allow for margin
            Vector2 originalIconCenter = iconRect.center;
            iconRect.width = iconSize;
            iconRect.height = iconSize;
            iconRect.center = originalIconCenter;

            //   Draw Icon
            Def firstUnlockable = GetFirstUnlockable(projectDef);
            if (firstUnlockable != null)
                Widgets.DefIcon(iconRect, firstUnlockable);


            List<string> unlockedProjectLabels = DefDatabase<ResearchProjectDef>.AllDefsListForReading
                                                                                .Where(def => !def.IsFinished &&
                                                                                              (def.prerequisites       != null && def.prerequisites.Contains(projectDef)) ||
                                                                                              (def.hiddenPrerequisites != null && def.hiddenPrerequisites.Contains(projectDef)))
                                                                                .Select(def => def.label)
                                                                                .ToList();

            if (unlockedProjectLabels.Count > 0)
            {
                Text.Anchor = TextAnchor.UpperLeft;
                string unlocksResearchString = "CM_Semi_Random_Unlocks_Research".Translate();
                string detailsLabel = "CM_Semi_Random_Unlocks_Research".Translate();

                for (int i = 0; i < unlockedProjectLabels.Count; ++i)
                {
                    detailsLabel = detailsLabel + unlockedProjectLabels[i];
                    if (i < unlockedProjectLabels.Count - 1)
                        detailsLabel = detailsLabel + ", ";
                }

                GUI.color = textColor;
                Widgets.Label(detailsRect, detailsLabel);

                GUI.color = Color.white;
                Widgets.Label(detailsRect, unlocksResearchString);
            }


            GUI.color = startingGuiColor;
            Text.Anchor = startingTextAnchor;
        }

        private void DrawBorderedBox(Rect rect, Color backgroundColor, Color borderColor, float borderThickness = 1f)
        {
            Color saveColor = GUI.color;

            Rect innerRect = new Rect(rect);
            innerRect.x += borderThickness;
            innerRect.y += borderThickness;
            innerRect.width -= borderThickness * 2;
            innerRect.height -= borderThickness * 2;

            Widgets.DrawRectFast(rect, borderColor);
            Widgets.DrawRectFast(innerRect, backgroundColor);

            GUI.color = saveColor;
        }

        private void DrawGoToTechTreeButton(Rect mainRect)
        {
            float buttonSize = 32.0f;
            Rect buttonRect = new Rect(mainRect.xMax - buttonSize - Margin, mainRect.yMin, buttonSize, buttonSize);

            // I'm just going to check both buttons in case either snatches up the event
            bool pressedButton1 = Widgets.ButtonTextSubtle(buttonRect, "");
            bool pressedButton2 = Widgets.ButtonImage(buttonRect, ResearchButtonIcon);

            if (pressedButton1 || pressedButton2)
            {
                SoundDefOf.ResearchStart.PlayOneShotOnCamera();

                MainTabWindow currentWindow = Find.WindowStack.WindowOfType<MainTabWindow>();
                MainTabWindow newWindow = MainButtonDefOf.Research.TabWindow;

                //Log.Message(string.Format("Has currentWindow {0}, has newWindow {1}", (currentWindow != null).ToString(), (newWindow != null).ToString()));

                if (currentWindow != null && newWindow != null)
                {
                    Find.WindowStack.TryRemove(currentWindow, false);
                    Find.WindowStack.Add(newWindow);
                    SoundDefOf.TabOpen.PlayOneShotOnCamera();
                }
            }
        }

        private void DrawRightColumn(Rect rightRect)
        {
            Rect position = rightRect;
            GUI.BeginGroup(position);
            if (selectedProject != null)
            {
                float projectNameHeight = 50.0f;
                float gapHeight = 10.0f;
                float startResearchButtonHeight = 68.0f;
                float progressBarHeight = 35.0f;
                float footerHeight = (gapHeight + startResearchButtonHeight + gapHeight + progressBarHeight);

                float debugFinishResearchNowButtonHeight = 30.0f;

                float currentY = 0f;

                Rect outRect = new Rect(0f, 0f, position.width, position.height - footerHeight);
                Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, rightScrollViewHeight);

                Widgets.BeginScrollView(outRect, ref rightScrollPosition, viewRect);

                // Selected project name
                Text.Font = GameFont.Medium;
                GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
                Rect projectNameRect = new Rect(0f, currentY, viewRect.width, projectNameHeight);
                Widgets.LabelCacheHeight(ref projectNameRect, selectedProject.LabelCap);
                GenUI.ResetLabelAlign();
                currentY += projectNameRect.height;

                // Selected project description
                Text.Font = GameFont.Small;
                Rect projectDescriptionRect = new Rect(0f, currentY, viewRect.width, 0f);
                Widgets.LabelCacheHeight(ref projectDescriptionRect, selectedProject.description);
                currentY += projectDescriptionRect.height;

                // Tech level research cost multiplier description
                if ((int)selectedProject.techLevel > (int)Faction.OfPlayer.def.techLevel)
                {
                    float costMultiplier = selectedProject.CostFactor(Faction.OfPlayer.def.techLevel);
                    Rect techLevelMultilplierDescriptionRect = new Rect(0f, currentY, viewRect.width, 0f);
                    string text = "TechLevelTooLow".Translate(Faction.OfPlayer.def.techLevel.ToStringHuman(), selectedProject.techLevel.ToStringHuman(), (1f / costMultiplier).ToStringPercent());
                    if (costMultiplier != 1f)
                    {
                        text += " " + "ResearchCostComparison".Translate(selectedProject.baseCost.ToString("F0"), selectedProject.CostApparent.ToString("F0"));
                    }
                    Widgets.LabelCacheHeight(ref techLevelMultilplierDescriptionRect, text);
                    currentY += techLevelMultilplierDescriptionRect.height;
                }

                // Prerequisites
                currentY += DrawResearchPrereqs(rect: new Rect(0f, currentY, viewRect.width, outRect.height), project: selectedProject);
                currentY += DrawResearchBenchRequirements(rect: new Rect(0f, currentY, viewRect.width, outRect.height), project: selectedProject);

                // Unlockables
                Rect projectUnlockablesRect = new Rect(0f, currentY, viewRect.width, footerHeight);
                currentY += DrawUnlockableHyperlinks(projectUnlockablesRect, selectedProject);
                currentY = (rightScrollViewHeight = currentY + 3f);

                Widgets.EndScrollView();

                // Start research button
                
                Rect startResearchButtonRect = new Rect(0f, outRect.height + gapHeight, position.width, startResearchButtonHeight);
                if (selectedProject.CanStartProject() && selectedProject != Find.ResearchManager.currentProj)
                {
                    if (Widgets.ButtonText(startResearchButtonRect, "Research".Translate()))
                    {
                        SoundDefOf.ResearchStart.PlayOneShotOnCamera();
                        Find.ResearchManager.currentProj = selectedProject;
                        Current.Game.World.GetComponent<ResearchTracker>()?.SetCurrentProject(selectedProject);
                        TutorSystem.Notify_Event("StartResearchProject");
                        if (!ColonistsHaveResearchBench)
                        {
                            Messages.Message("MessageResearchMenuWithoutBench".Translate(), MessageTypeDefOf.CautionInput);
                        }
                    }
                }
                else
                {
                    string projectStatus = "";
                    if (selectedProject.IsFinished)
                    {
                        projectStatus = "Finished".Translate();
                        Text.Anchor = TextAnchor.MiddleCenter;
                    }
                    else if (selectedProject == Find.ResearchManager.currentProj)
                    {
                        projectStatus = "InProgress".Translate();
                        Text.Anchor = TextAnchor.MiddleCenter;
                    }
                    else
                    {
                        projectStatus = "Locked".Translate() + ":";
                        if (!selectedProject.PrerequisitesCompleted)
                        {
                            projectStatus += "\n  " + "PrerequisitesNotCompleted".Translate();
                        }
                        if (!selectedProject.TechprintRequirementMet)
                        {
                            projectStatus += "\n  " + "InsufficientTechprintsApplied".Translate(selectedProject.TechprintsApplied, selectedProject.TechprintCount);
                        }
                    }
                    Widgets.DrawHighlight(startResearchButtonRect);
                    Widgets.Label(startResearchButtonRect.ContractedBy(5f), projectStatus);
                    Text.Anchor = TextAnchor.UpperLeft;
                }

                // Progress bar
                Rect progressBarRect = new Rect(0f, startResearchButtonRect.yMax + gapHeight, position.width, progressBarHeight);
                Widgets.FillableBar(progressBarRect, selectedProject.ProgressPercent, ResearchBarFillTex, ResearchBarBGTex, doBorder: true);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(progressBarRect, selectedProject.ProgressApparent.ToString("F0") + " / " + selectedProject.CostApparent.ToString("F0"));
                Text.Anchor = TextAnchor.UpperLeft;
                if (Prefs.DevMode && !selectedProject.IsFinished && Widgets.ButtonText(new Rect(startResearchButtonRect.x, startResearchButtonRect.y - debugFinishResearchNowButtonHeight, 120f, debugFinishResearchNowButtonHeight), "Debug: Finish now"))
                {
                    Find.ResearchManager.currentProj = selectedProject;
                    Find.ResearchManager.FinishProject(selectedProject);
                }
            }

            GUI.EndGroup();
        }

        private float DrawResearchPrereqs(ResearchProjectDef project, Rect rect)
        {
            if (project.prerequisites.NullOrEmpty())
            {
                return 0f;
            }
            float xMin = rect.xMin;
            float yMin = rect.yMin;
            Widgets.LabelCacheHeight(ref rect, "ResearchPrerequisites".Translate() + ":");
            rect.yMin += rect.height;
            rect.xMin += 6f;
            for (int i = 0; i < project.prerequisites.Count; i++)
            {
                GUI.color = FulfilledPrerequisiteColor;
                Widgets.LabelCacheHeight(ref rect, project.prerequisites[i].LabelCap);
                rect.yMin += rect.height;
            }
            if (project.hiddenPrerequisites != null)
            {
                for (int j = 0; j < project.hiddenPrerequisites.Count; j++)
                {
                    GUI.color = FulfilledPrerequisiteColor;
                    Widgets.LabelCacheHeight(ref rect, project.hiddenPrerequisites[j].LabelCap);
                    rect.yMin += rect.height;
                }
            }
            GUI.color = Color.white;
            rect.xMin = xMin;
            return rect.yMin - yMin;
        }

        private float DrawResearchBenchRequirements(ResearchProjectDef project, Rect rect)
        {
            float xMin = rect.xMin;
            float yMin = rect.yMin;
            if (project.requiredResearchBuilding != null)
            {
                List<Map> maps = Find.Maps;
                Widgets.LabelCacheHeight(ref rect, "RequiredResearchBench".Translate() + ":");
                rect.xMin += 6f;
                rect.yMin += rect.height;
                GUI.color = FulfilledPrerequisiteColor;
                rect.height = Text.CalcHeight(project.requiredResearchBuilding.LabelCap, rect.width - 24f - 6f);
                Widgets.HyperlinkWithIcon(rect, new Dialog_InfoCard.Hyperlink(project.requiredResearchBuilding));
                rect.yMin += rect.height + 4f;
                GUI.color = Color.white;
                rect.xMin = xMin;
            }
            if (!project.requiredResearchFacilities.NullOrEmpty())
            {
                Widgets.LabelCacheHeight(ref rect, "RequiredResearchBenchFacilities".Translate() + ":");
                rect.yMin += rect.height;
                Building_ResearchBench building_ResearchBench = FindBenchFulfillingMostRequirements(project.requiredResearchBuilding, project.requiredResearchFacilities);
                CompAffectedByFacilities bestMatchingBench = null;
                if (building_ResearchBench != null)
                {
                    bestMatchingBench = building_ResearchBench.TryGetComp<CompAffectedByFacilities>();
                }
                rect.xMin += 6f;
                for (int j = 0; j < project.requiredResearchFacilities.Count; j++)
                {
                    DrawResearchBenchFacilityRequirement(project.requiredResearchFacilities[j], bestMatchingBench, project, ref rect);
                    rect.yMin += rect.height;
                }
                rect.yMin += 4f;
            }
            GUI.color = Color.white;
            rect.xMin = xMin;
            return rect.yMin - yMin;
        }

        private Building_ResearchBench FindBenchFulfillingMostRequirements(ThingDef requiredResearchBench, List<ThingDef> requiredFacilities)
        {
            tmpAllBuildings.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                tmpAllBuildings.AddRange(maps[i].listerBuildings.allBuildingsColonist);
            }
            float num = 0f;
            Building_ResearchBench building_ResearchBench = null;
            for (int j = 0; j < tmpAllBuildings.Count; j++)
            {
                Building_ResearchBench building_ResearchBench2 = tmpAllBuildings[j] as Building_ResearchBench;
                if (building_ResearchBench2 != null && (requiredResearchBench == null || building_ResearchBench2.def == requiredResearchBench))
                {
                    float researchBenchRequirementsScore = GetResearchBenchRequirementsScore(building_ResearchBench2, requiredFacilities);
                    if (building_ResearchBench == null || researchBenchRequirementsScore > num)
                    {
                        num = researchBenchRequirementsScore;
                        building_ResearchBench = building_ResearchBench2;
                    }
                }
            }
            tmpAllBuildings.Clear();
            return building_ResearchBench;
        }

        private void DrawResearchBenchFacilityRequirement(ThingDef requiredFacility, CompAffectedByFacilities bestMatchingBench, ResearchProjectDef project, ref Rect rect)
        {
            Thing thing = null;
            Thing thing2 = null;
            if (bestMatchingBench != null)
            {
                thing = bestMatchingBench.LinkedFacilitiesListForReading.Find((Thing x) => x.def == requiredFacility);
                thing2 = bestMatchingBench.LinkedFacilitiesListForReading.Find((Thing x) => x.def == requiredFacility && bestMatchingBench.IsFacilityActive(x));
            }
            GUI.color = FulfilledPrerequisiteColor;
            string text = requiredFacility.LabelCap;
            if (thing != null && thing2 == null)
            {
                text += " (" + "InactiveFacility".Translate() + ")";
            }
            rect.height = Text.CalcHeight(text, rect.width - 24f - 6f);
            Widgets.HyperlinkWithIcon(rect, new Dialog_InfoCard.Hyperlink(requiredFacility), text);
        }

        private float GetResearchBenchRequirementsScore(Building_ResearchBench bench, List<ThingDef> requiredFacilities)
        {
            float num = 0f;
            for (int i = 0; i < requiredFacilities.Count; i++)
            {
                CompAffectedByFacilities benchComp = bench.GetComp<CompAffectedByFacilities>();
                if (benchComp != null)
                {
                    List<Thing> linkedFacilitiesListForReading = benchComp.LinkedFacilitiesListForReading;
                    if (linkedFacilitiesListForReading.Find((Thing x) => x.def == requiredFacilities[i] && benchComp.IsFacilityActive(x)) != null)
                    {
                        num += 1f;
                    }
                    else if (linkedFacilitiesListForReading.Find((Thing x) => x.def == requiredFacilities[i]) != null)
                    {
                        num += 0.6f;
                    }
                }
            }
            return num;
        }

        private Def GetFirstUnlockable(ResearchProjectDef project)
        {
            List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>> list = UnlockedDefsGroupedByPrerequisites(project);

            if (list.NullOrEmpty())
                return null;

            List<Def> defList = list.First().Second;
            if (defList.NullOrEmpty())
                return null;

            int randomIndex = Rand.RangeInclusiveSeeded(0, defList.Count - 1, currentRandomSeed);

            return defList[randomIndex];
        }

        private float DrawUnlockableHyperlinks(Rect rect, ResearchProjectDef project)
        {
            List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>> list = UnlockedDefsGroupedByPrerequisites(project);
            if (list.NullOrEmpty())
            {
                return 0f;
            }
            float yMin = rect.yMin;
            float x = rect.x;
            foreach (Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>> item in list)
            {
                ResearchPrerequisitesUtility.UnlockedHeader first = item.First;
                rect.x = x;
                if (!first.unlockedBy.Any())
                {
                    Widgets.LabelCacheHeight(ref rect, "Unlocks".Translate() + ":");
                }
                else
                {
                    Widgets.LabelCacheHeight(ref rect, string.Concat("UnlockedWith".Translate(), " ", HeaderLabel(first), ":"));
                }
                rect.x += 6f;
                rect.yMin += rect.height;
                foreach (Def item2 in item.Second)
                {
                    Widgets.HyperlinkWithIcon(hyperlink: new Dialog_InfoCard.Hyperlink(item2), rect: new Rect(rect.x, rect.yMin, rect.width, 24f));
                    rect.yMin += 24f;
                }
            }
            return rect.yMin - yMin;
        }


        private string HeaderLabel(ResearchPrerequisitesUtility.UnlockedHeader headerProject)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string value = "";
            for (int i = 0; i < headerProject.unlockedBy.Count; i++)
            {
                ResearchProjectDef researchProjectDef = headerProject.unlockedBy[i];
                string text = researchProjectDef.LabelCap;
                stringBuilder.Append(text).Append(value);
                value = ", ";
            }
            return stringBuilder.ToString();
        }

        private List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>> UnlockedDefsGroupedByPrerequisites(ResearchProjectDef project)
        {
            if (cachedUnlockedDefsGroupedByPrerequisites == null)
            {
                cachedUnlockedDefsGroupedByPrerequisites = new Dictionary<ResearchProjectDef, List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>>>();
            }
            if (!cachedUnlockedDefsGroupedByPrerequisites.TryGetValue(project, out var value))
            {
                value = ResearchPrerequisitesUtility.UnlockedDefsGroupedByPrerequisites(project);
                cachedUnlockedDefsGroupedByPrerequisites.Add(project, value);
            }
            return value;
        }
    }
}