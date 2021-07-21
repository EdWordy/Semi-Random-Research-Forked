using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CM_Semi_Random_Research
{
    public enum ManualReroll
    {
        None,
        Once,
        Always
    }

    public class SemiRandomResearchModSettings : ModSettings
    {
        public bool featureEnabled = true;
        public bool rerollAllEveryTime = true;

        public bool forceLowestTechLevel = false;
        public bool restrictToFactionTechLevel = false;

        //public bool showResearchButton = true;

        public ManualReroll allowManualReroll = ManualReroll.None;

        public int availableProjectCount = 3;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref featureEnabled, "featureEnabled", true);
            Scribe_Values.Look(ref rerollAllEveryTime, "rerollAllEveryTime", true);

            //Scribe_Values.Look(ref showResearchButton, "showResearchButton", true);

            Scribe_Values.Look(ref allowManualReroll, "allowManualReroll", ManualReroll.None);

            Scribe_Values.Look(ref availableProjectCount, "availableProjectCount", 3);

            Scribe_Values.Look(ref forceLowestTechLevel, "forceLowestTechLevel", false);
            Scribe_Values.Look(ref restrictToFactionTechLevel, "restrictToFactionTechLevel", false);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            bool showResearchButtonWas = featureEnabled;

            string intEditBuffer = availableProjectCount.ToString();
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.ColumnWidth = (inRect.width - 34f) / 2f;

            listing_Standard.Begin(inRect);

            listing_Standard.CheckboxLabeled("CM_Semi_Random_Research_Setting_Feature_Enabled_Label".Translate(), ref featureEnabled, "CM_Semi_Random_Research_Setting_Feature_Enabled_Description".Translate());
            listing_Standard.CheckboxLabeled("CM_Semi_Random_Research_Setting_Reroll_All_Every_Time_Label".Translate(), ref rerollAllEveryTime, "CM_Semi_Random_Research_Setting_Reroll_All_Every_Time_Description".Translate());
            //listing_Standard.CheckboxLabeled("CM_Semi_Random_Research_Setting_Show_Research_Button_Label".Translate(), ref showResearchButton, "CM_Semi_Random_Research_Setting_Show_Research_Button_Description".Translate());


            listing_Standard.GapLine();

            listing_Standard.Label("CM_Semi_Random_Research_Setting_Manual_Reroll_Label".Translate());
            if (listing_Standard.RadioButton("CM_Semi_Random_Research_Setting_No_Manual_Reroll_Label".Translate(), allowManualReroll == ManualReroll.None, 8f, "CM_Semi_Random_Research_Setting_No_Manual_Reroll_Description".Translate()))
                allowManualReroll = ManualReroll.None;
            if (listing_Standard.RadioButton("CM_Semi_Random_Research_Setting_Reroll_One_Time_Label".Translate(), allowManualReroll == ManualReroll.Once, 8f, "CM_Semi_Random_Research_Setting_Reroll_One_Time_Description".Translate()))
                allowManualReroll = ManualReroll.Once;
            if (listing_Standard.RadioButton("CM_Semi_Random_Research_Setting_Reroll_Any_Time_Label".Translate(), allowManualReroll == ManualReroll.Always, 8f, "CM_Semi_Random_Research_Setting_Reroll_Any_Time_Description".Translate()))
                allowManualReroll = ManualReroll.Always;

            listing_Standard.GapLine();

            listing_Standard.Label("CM_Semi_Random_Research_Setting_Available_Projects_Count_Label".Translate(), -1, "CM_Semi_Random_Research_Setting_Available_Projects_Count_Description".Translate());
            listing_Standard.Label(availableProjectCount.ToString());
            listing_Standard.IntAdjuster(ref availableProjectCount, 1, 1);

            listing_Standard.GapLine();
            listing_Standard.CheckboxLabeled("CM_Semi_Random_Research_Setting_Force_Lowest_Tech_Level_Label".Translate(), ref forceLowestTechLevel, "CM_Semi_Random_Research_Setting_Force_Lowest_Tech_Level_Description".Translate());
            listing_Standard.CheckboxLabeled("CM_Semi_Random_Research_Setting_Restrict_To_Faction_Tech_Level_Label".Translate(), ref restrictToFactionTechLevel, "CM_Semi_Random_Research_Setting_Restrict_To_Faction_Tech_Level_Description".Translate());

            listing_Standard.End();

            if (featureEnabled != showResearchButtonWas)
                UpdateShowResearchButton();
        }

        public void UpdateSettings()
        {
        }

        public void UpdateShowResearchButton()
        {
            UIRoot_Play uiRootPlay = Find.UIRoot as UIRoot_Play;

            if (uiRootPlay != null)
            {
                MainButtonsRoot mainButtonsRoot = uiRootPlay.mainButtonsRoot;

                if (mainButtonsRoot != null)
                {
                    FieldInfo allButtonsInOrderField = mainButtonsRoot.GetType().GetField("allButtonsInOrder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    List<MainButtonDef> mainButtons = allButtonsInOrderField.GetValue(mainButtonsRoot) as List<MainButtonDef>;

                    MainButtonDef buttonToUse = SemiRandomResearchDefOf.CM_Semi_Random_Research_MainButton_Next_Research;
                    if (!featureEnabled)
                        buttonToUse = MainButtonDefOf.Research;

                    // Pull both of the buttons out to be sure, then put the correct one back in
                    mainButtons = mainButtons.Where(button => button != MainButtonDefOf.Research && button != SemiRandomResearchDefOf.CM_Semi_Random_Research_MainButton_Next_Research).ToList();
                    mainButtons.Add(buttonToUse);
                    mainButtons.Sort((a, b) => a.order - b.order);

                    allButtonsInOrderField.SetValue(mainButtonsRoot, mainButtons);
                }
            }
        }
    }
}
