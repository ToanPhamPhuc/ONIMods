using KSerialization;
using UnityEngine;

namespace OxygenNotIncluded.Mods.ModTemplate
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class CryoCondenser : KMonoBehaviour, ISim200ms
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Storage storage;
        [MyCmpReq]
        private Operational operational;
        [MyCmpReq]
        private PrimaryElement primaryElement;
        [MyCmpReq]
        private EnergyConsumer energyConsumer;
#pragma warning restore CS0649

        private const float BATCH_MASS_KG = 10f;
        private const float BASE_IDLE_WATTAGE = 0f;

        // Ratio based on Aquatuner baseline (1200W / 585,060 DTU/s = ~0.002051 W/DTU)
        private const float WATTS_PER_DTU_PER_SEC = 1200f / 585060f;

        public void Sim200ms(float dt)
        {
            if (!operational.IsOperational)
            {
                operational.SetActive(false);
                energyConsumer.BaseWattageRating = BASE_IDLE_WATTAGE;
                return;
            }

            // Calculate total matching gas mass in storage
            float storedGasMass = 0f;
            GameObject targetGasItem = null;

            for (int i = storage.items.Count - 1; i >= 0; i--)
            {
                GameObject item = storage.items[i];
                if (item == null) continue;

                PrimaryElement pe = item.GetComponent<PrimaryElement>();
                if (pe != null && pe.Element.IsGas && pe.Element.lowTempTransition != null)
                {
                    storedGasMass += pe.Mass;
                    if (targetGasItem == null) targetGasItem = item;
                }
            }

            // Only run when we have accumulated a full 10 kg batch
            if (storedGasMass >= BATCH_MASS_KG && targetGasItem != null)
            {
                PrimaryElement pe = targetGasItem.GetComponent<PrimaryElement>();
                Element element = pe.Element;
                Element liquidElement = element.lowTempTransition;

                float massToConvert = Mathf.Min(pe.Mass, BATCH_MASS_KG);

                // Target temp: 14 K below condensation point
                float targetTempKelvin = Mathf.Max(element.lowTemp - 14f, 1f);

                float tempDiff = pe.Temperature - targetTempKelvin;
                float heatExtractedDTU = 0f;

                if (tempDiff > 0f)
                {
                    // Total heat extracted (DTU) for this batch
                    heatExtractedDTU = massToConvert * element.specificHeatCapacity * tempDiff;

                    // Heat rate per second (since Sim200ms runs 5 times per second, batch heat per sec)
                    float heatRateDTUperSec = heatExtractedDTU;

                    // Dynamic Power Calculation based on extracted heat
                    float dynamicWattage = heatRateDTUperSec * WATTS_PER_DTU_PER_SEC;

                    // Set building power draw dynamically
                    energyConsumer.BaseWattageRating = dynamicWattage;

                    // Dump extracted heat directly into building body
                    float buildingMass = primaryElement.Mass;
                    float buildingSHC = primaryElement.Element.specificHeatCapacity;

                    if (buildingMass > 0f && buildingSHC > 0f)
                    {
                        primaryElement.Temperature += heatExtractedDTU / (buildingMass * buildingSHC);
                    }
                }

                // Activate building visual/audio states
                operational.SetActive(true);

                // Perform Element Transition
                SimHashes liquidHash = liquidElement.id;
                byte diseaseIdx = pe.DiseaseIdx;
                int diseaseCount = pe.DiseaseCount;

                storage.ConsumeIgnoringDisease(targetGasItem);

                storage.AddLiquid(
                    liquidHash,
                    massToConvert,
                    targetTempKelvin,
                    diseaseIdx,
                    diseaseCount,
                    false,
                    true
                );
            }
            else
            {
                // Reset power draw & state when idle/buffering gas
                energyConsumer.BaseWattageRating = BASE_IDLE_WATTAGE;
                operational.SetActive(false);
            }
        }
    }
}