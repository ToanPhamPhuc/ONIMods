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

        private const float BATCH_MASS_KG = 10f; // Requires 10 kg batch
        private const float WATTS_PER_DTU_PER_SEC = 1200f / 585060f; // ~0.002051 W per DTU/s

        public void Sim200ms(float dt)
        {
            if (!operational.IsOperational)
            {
                operational.SetActive(false);
                energyConsumer.BaseWattageRating = 0f;
                return;
            }

            // 1. Group total stored gas mass by Element type
            System.Collections.Generic.Dictionary<Element, float> elementMassMap = new System.Collections.Generic.Dictionary<Element, float>();

            for (int i = 0; i < storage.items.Count; i++)
            {
                GameObject item = storage.items[i];
                if (item == null) continue;

                PrimaryElement pe = item.GetComponent<PrimaryElement>();
                if (pe != null && pe.Element.IsGas && pe.Element.lowTempTransition != null && pe.Mass > 0f)
                {
                    if (!elementMassMap.ContainsKey(pe.Element))
                    {
                        elementMassMap[pe.Element] = 0f;
                    }
                    elementMassMap[pe.Element] += pe.Mass;
                }
            }

            // 2. Find the gas element that has the HIGHEST accumulated mass
            Element targetGasElement = null;
            float maxMassFound = 0f;

            foreach (var kvp in elementMassMap)
            {
                if (kvp.Value >= BATCH_MASS_KG && kvp.Value > maxMassFound)
                {
                    maxMassFound = kvp.Value;
                    targetGasElement = kvp.Key;
                }
            }

            // 3. Process the chosen gas element if it has at least 10 kg
            if (targetGasElement != null)
            {
                Element liquidElement = targetGasElement.lowTempTransition;
                float massToConvert = BATCH_MASS_KG;
                float targetTempKelvin = Mathf.Max(targetGasElement.lowTemp - 14f, 1f);

                // Consume exactly 10 kg of this specific gas
                float remainingToConsume = massToConvert;
                float totalGasTempSum = 0f;
                byte diseaseIdx = 0;
                int diseaseCount = 0;

                for (int i = storage.items.Count - 1; i >= 0; i--)
                {
                    GameObject item = storage.items[i];
                    if (item == null) continue;

                    PrimaryElement pe = item.GetComponent<PrimaryElement>();
                    if (pe != null && pe.Element == targetGasElement && pe.Mass > 0f)
                    {
                        float amountFromThisItem = Mathf.Min(pe.Mass, remainingToConsume);
                        totalGasTempSum += pe.Temperature * amountFromThisItem;
                        diseaseIdx = pe.DiseaseIdx;
                        diseaseCount += pe.DiseaseCount;

                        remainingToConsume -= amountFromThisItem;
                        pe.Mass -= amountFromThisItem;

                        if (pe.Mass <= 0f)
                        {
                            storage.ConsumeIgnoringDisease(item);
                        }

                        if (remainingToConsume <= 0f) break;
                    }
                }

                float averageGasTemp = totalGasTempSum / massToConvert;
                float tempDiff = averageGasTemp - targetTempKelvin;

                if (tempDiff > 0f)
                {
                    // Calculate dynamic wattage
                    float heatExtractedDTU = massToConvert * targetGasElement.specificHeatCapacity * tempDiff;
                    float heatRateDTUperSec = heatExtractedDTU * 5f; // 5 ticks / sec

                    float dynamicWattage = heatRateDTUperSec * WATTS_PER_DTU_PER_SEC;
                    energyConsumer.BaseWattageRating = dynamicWattage;

                    // Building frame heating
                    float buildingMass = primaryElement.Mass;
                    float buildingSHC = primaryElement.Element.specificHeatCapacity;

                    if (buildingMass > 0f && buildingSHC > 0f)
                    {
                        primaryElement.Temperature += heatExtractedDTU / (buildingMass * buildingSHC);
                    }
                }

                operational.SetActive(true);

                // Dispense converted liquid packet
                SimHashes liquidHash = liquidElement.id;
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
                // Idle state: set active false and 0W draw
                energyConsumer.BaseWattageRating = 0f;
                operational.SetActive(false);
            }
        }
    }
}