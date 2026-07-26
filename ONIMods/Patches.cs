using HarmonyLib;
using STRINGS;

namespace OxygenNotIncluded.Mods.ModTemplate
{
    public class Patches
    {
        [HarmonyPatch(typeof(Localization), "Initialize")]
        public static class Localization_Initialize_Patch
        {
            public static void Postfix()
            {
                Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CryoCondenserConfig.ID.ToUpper()}.NAME", UI.FormatAsLink("Cryo Condenser", CryoCondenserConfig.ID));
                Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CryoCondenserConfig.ID.ToUpper()}.DESC", "A high-powered condenser that cools gas into its liquid state and outputs the thermal heat into its surroundings.");
                Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CryoCondenserConfig.ID.ToUpper()}.EFFECT", $"Condenses incoming {UI.FormatAsLink("Gas", "ELEMENTS_GAS")} into {UI.FormatAsLink("Liquid", "ELEMENTS_LIQUID")} while outputting {UI.FormatAsLink("Heat", "HEAT")} in its immediate vicinity.");
            }
        }

        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
        {
            public static void Prefix()
            {
                ModUtil.AddBuildingToPlanScreen("Utilities", CryoCondenserConfig.ID);
            }
        }

        [HarmonyPatch(typeof(Database.Techs), "Init")]
        public static class Techs_Init_Patch
        {
            public static void Postfix(Database.Techs __instance)
            {
                // Dynamic Research Tier Selection
                string targetTechId = "LiquidTemperature"; // Default / Easy

                if (PlayerConfig.Instance?.Difficulty == TechDifficulty.Hard)
                {
                    targetTechId = "HydrogenEngine"; // Hard Mode
                }

                Tech tech = __instance.Get(targetTechId);
                tech?.unlockedItemIDs.Add(CryoCondenserConfig.ID);
            }
        }
    }
}