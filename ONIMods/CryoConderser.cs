using KSerialization;
using UnityEngine;

namespace OxygenNotIncluded.Mods.ModTemplate
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class CryoCondenser : KMonoBehaviour, ISim200ms
    {
        [MyCmpReq]
        private Storage storage;
        [MyCmpReq]
        private Operational operational;
        [MyCmpReq]
        private PrimaryElement primaryElement;

        private const float BATCH_MASS_KG = 10f; // 10 kg per processing tick
        private const float HIGH_WATER_MARK_KG = 100f; // Wait until 100 kg accumulated to start!

        [Serialize]
        private bool isBufferingFull = false;

        public void Sim200ms(float dt)
        {
            if (!operational.IsOperational)
            {
                operational.SetActive(false);
                return;
            }

            // 1. Calculate stored gas mass per element
            System.Collections.Generic.Dictionary<Element, float> elementMassMap = new System.Collections.Generic.Dictionary<Element, float>();
            float totalAllGasMass = 0f;

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
                    totalAllGasMass += pe.Mass;
                }
            }

            // 2. Buffer State Logic (Hysteresis)
            if (!isBufferingFull)
            {
                if (totalAllGasMass >= HIGH_WATER_MARK_KG)
                {
                    isBufferingFull = true;
                }
                else
                {
                    operational.SetActive(false);
                    return;
                }
            }

            // 3. Find target element
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

            if (targetGasElement == null)
            {
                isBufferingFull = false;
                operational.SetActive(false);
                return;
            }

            // 4. Process Batch
            Element liquidElement = targetGasElement.lowTempTransition;
            float massToConvert = BATCH_MASS_KG;

            // Select output temperature based on configured CoolingMode
            float targetTempKelvin;
            if (PlayerConfig.Instance?.OutputCoolingMode == CoolingMode.Safe)
            {
                // Safe Mode: Freezing point + 4K buffer (prevents pipe bursts)
                targetTempKelvin = liquidElement.lowTemp + 4f;
            }
            else
            {
                // Legacy Mode: Boiling point - 14K
                targetTempKelvin = Mathf.Max(targetGasElement.lowTemp - 14f, 1f);
            }

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
                float heatExtractedDTU = massToConvert * targetGasElement.specificHeatCapacity * tempDiff;
                float buildingMass = primaryElement.Mass;
                float buildingSHC = primaryElement.Element.specificHeatCapacity;

                if (buildingMass > 0f && buildingSHC > 0f)
                {
                    primaryElement.Temperature += heatExtractedDTU / (buildingMass * buildingSHC);
                }
            }

            operational.SetActive(true);

            // Dispense liquid packet
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
    }
}