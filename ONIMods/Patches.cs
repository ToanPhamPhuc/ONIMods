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
                Tech tech = null;

                if (PlayerConfig.Instance?.Difficulty == TechDifficulty.Hard)
                {
                    // DLC: CryoFuelPropulsion | Base Game: HydrogenEngine
                    string[] hardTechCandidates = new string[] 
                    { 
                        "CryoFuelPropulsion", 
                        "HydrogenEngine" 
                    };

                    foreach (string techId in hardTechCandidates)
                    {
                        tech = __instance.TryGet(techId);
                        if (tech != null) break;
                    }
                }

                // Fallback to Easy Mode (LiquidTemperature / Aquatuner) if not set or found
                if (tech == null)
                {
                    tech = __instance.TryGet("LiquidTemperature");
                }

                // Add building to the resolved tech node
                tech?.unlockedItemIDs.Add(CryoCondenserConfig.ID);
            }
        }
    }
}