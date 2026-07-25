using HarmonyLib;
using STRINGS;

namespace OxygenNotIncluded.Mods.ModTemplate
{
    public class Patches
    {
        // Add UI Strings & Localization
        [HarmonyPatch(typeof(Localization), "Initialize")]
        public static class Localization_Initialize_Patch
        {
            public static void Postfix()
            {
                Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CryoCondenserConfig.ID.ToUpper()}.NAME", UI.FormatAsLink("Cryo Condenser", CryoCondenserConfig.ID));
                Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CryoCondenserConfig.ID.ToUpper()}.DESC", "A high-powered condenser that cools gas into its liquid state and outputs the thermal heat into its surroundings.");
                Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CryoCondenserConfig.ID.ToUpper()}.EFFECT", $"Condenses incoming {UI.FormatAsLink("Gas", "ELEMENTS_GAS")} into {UI.FormatAsLink("Liquid", "ELEMENTS_LIQUID")} cooled 14°C below its boiling point while outputting {UI.FormatAsLink("Heat", "HEAT")} in its immediate vicinity.");
            }
        }

        // Add to Build Menu (Plumbing Category)
        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
        {
            public static void Prefix()
            {
                ModUtil.AddBuildingToPlanScreen("Utilities", CryoCondenserConfig.ID);
            }
        }

        // Add to Research Tree
        [HarmonyPatch(typeof(Database.Techs), "Init")]
        public static class Techs_Init_Patch
        {
            public static void Postfix(Database.Techs __instance)
            {
                Tech tech = __instance.Get("LiquidTemperature");
                tech?.unlockedItemIDs.Add(CryoCondenserConfig.ID);
            }
        }
    }
}