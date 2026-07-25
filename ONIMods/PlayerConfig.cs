using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace OxygenNotIncluded.Mods.ModTemplate
{
    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    public class PlayerConfig : SingletonOptions<PlayerConfig>
    {
        [JsonProperty]
        [Option("Power Consumption (Watts)", "Set the power usage for the Cryo Condenser.")]
        [Limit(1200f, 10000f)]
        public float PowerConsumption { get; set; }

        public PlayerConfig()
        {
            PowerConsumption = 2400f;
        }
    }
}