using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace OxygenNotIncluded.Mods.ModTemplate
{
    public enum TechDifficulty
    {
        [Option("Easy (Liquid Temperature)", "Unlocked alongside Thermo Aquatuner / Liquid Temperature tech.")]
        Easy,
        [Option("Hard (Hydrogen Engines / High-Tier)", "Unlocked at late-game Hydrogen Engine research.")]
        Hard
    }

    public enum CoolingMode
    {
        [Option("Easy (Safe Mode)", "Cools liquid to 4°C above its freezing point to prevent pipe bursts.")]
        Safe,
        [Option("Hard (Legacy Mode)", "Cools liquid 14°C below its boiling point (can freeze/burst pipes for narrow-range gases like Hydrogen).")]
        Legacy
    }

    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    public class PlayerConfig : SingletonOptions<PlayerConfig>
    {
        [JsonProperty]
        [Option("Power Consumption (Watts)", "Set the power usage for the Cryo Condenser.")]
        [Limit(1200f, 10000f)]
        public float PowerConsumption { get; set; } = 2400f;

        [JsonProperty]
        [Option("Tech Tree Difficulty", "Controls where the Cryo Condenser appears in the research tree.")]
        public TechDifficulty Difficulty { get; set; } = TechDifficulty.Easy;

        [JsonProperty]
        [Option("Cooling Mode", "Controls output temperature logic.")]
        public CoolingMode OutputCoolingMode { get; set; } = CoolingMode.Safe;

        // NEW: Experimental / Alpha toggle for Turbo Mode
        [JsonProperty]
        [Option("Enable Turbo Mode (Alpha)", "Enable the sidescreen button to toggle 4x speed and power mode.")]
        public bool EnableTurboMode { get; set; } = false;
    }
}