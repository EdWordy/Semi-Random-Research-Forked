using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace CM_Semi_Random_Research
{
    [StaticConstructorOnStartup]
    public static class MainTabWindow_Research_Patches
    {
        private static readonly Texture2D NextResearchButtonIcon = ContentFinder<Texture2D>.Get("UI/Buttons/MainButtons/CM_Semi_Random_Research_Random");

        [HarmonyPatch(typeof(MainTabWindow_Research))]
        [HarmonyPatch("DrawLeftRect", MethodType.Normal)]
        public static class MainTabWindow_Research_DrawLeftRect
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                //// if (selectedProject.CanStartNow && selectedProject != Find.ResearchManager.currentProj)
                // IL_030f: ldarg.0
                // IL_0310: ldfld class Verse.ResearchProjectDef RimWorld.MainTabWindow_Research::selectedProject
                // IL_0315: callvirt instance bool Verse.ResearchProjectDef::get_CanStartNow() // <--- what we are searching for
                // IL_031a: brfalse IL_03a2

                FieldInfo selectedProjectFieldInfo = typeof(RimWorld.MainTabWindow_Research).GetField("selectedProject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                MethodInfo canStartNowMethodInfo = AccessTools.Method(typeof(Verse.ResearchProjectDef), "get_CanStartNow");

                MethodInfo replacementCanStartCheck = AccessTools.Method(typeof(SemiRandomResearchUtility), nameof(SemiRandomResearchUtility.CanSelectNormalResearchNow));

                List<CodeInstruction> instructionList = instructions.ToList();

                for (int i = 0; i < instructionList.Count; ++i)
                {
                    if (i >= 2 && i < instructionList.Count - 1)
                    {
                        // Verify everything we are replacing to make sure this hasn't already been tampered with
                        if (instructionList[i - 2].IsLdarg() &&
                            instructionList[i - 1].LoadsField(selectedProjectFieldInfo) &&
                            instructionList[i - 0].Calls(canStartNowMethodInfo))
                        {
                            Log.Message("[CM_Semi_Random_Research] - patching to conditionally hide normal start research button.");

                            instructionList[i - 2] = new CodeInstruction(OpCodes.Nop);
                            instructionList[i - 1] = new CodeInstruction(OpCodes.Nop);
                            instructionList[i - 0] = new CodeInstruction(OpCodes.Call, replacementCanStartCheck);
                        }
                    }
                }

                foreach (CodeInstruction instruction in instructionList)
                {
                    yield return instruction;
                }
            }

            [HarmonyPostfix]
            public static void Postfix(ResearchProjectDef __instance, Rect leftOutRect)
            {
                float buttonSize = 32.0f;
                Rect buttonRect = new Rect(leftOutRect.xMax - buttonSize, leftOutRect.yMin, buttonSize, buttonSize);

                // I'm just going to check both buttons in case either snatches up the event
                bool pressedButton1 = Widgets.ButtonTextSubtle(buttonRect, "");
                bool pressedButton2 = Widgets.ButtonImage(buttonRect, NextResearchButtonIcon);

                if (pressedButton1 || pressedButton2)
                {
                    SoundDefOf.ResearchStart.PlayOneShotOnCamera();

                    MainTabWindow currentWindow = Find.WindowStack.WindowOfType<MainTabWindow>();
                    MainTabWindow newWindow = SemiRandomResearchDefOf.CM_Semi_Random_Research_MainButton_Next_Research.TabWindow;

                    //Log.Message(string.Format("Has currentWindow {0}, has newWindow {1}", (currentWindow != null).ToString(), (newWindow != null).ToString()));
                    
                    if (currentWindow != null && newWindow != null)
                    {
                        Find.WindowStack.TryRemove(currentWindow, false);
                        Find.WindowStack.Add(newWindow);
                        SoundDefOf.TabOpen.PlayOneShotOnCamera();
                    }
                }
            }
        }
    }
}
