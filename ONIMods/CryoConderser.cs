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
#pragma warning restore CS0649

        private const float BATCH_MASS_KG = 10f; // Target 10 kg output batch

        public void Sim200ms(float dt)
        {
            if (!operational.IsOperational)
            {
                operational.SetActive(false);
                return;
            }

            // Find how much gas is currently stored
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

            // Only operate when we have at least 10 kg of gas accumulated!
            if (storedGasMass >= BATCH_MASS_KG && targetGasItem != null)
            {
                operational.SetActive(true);

                PrimaryElement pe = targetGasItem.GetComponent<PrimaryElement>();
                Element element = pe.Element;
                Element liquidElement = element.lowTempTransition;

                // Process up to 10 kg at a time
                float massToConvert = Mathf.Min(pe.Mass, BATCH_MASS_KG);

                // Target temperature: 14 K below condensation point
                float targetTempKelvin = Mathf.Max(element.lowTemp - 14f, 1f);

                // Heat transfer: Dump heat energy extracted into the building body
                float tempDiff = pe.Temperature - targetTempKelvin;
                if (tempDiff > 0f)
                {
                    float heatExtractedDTU = massToConvert * element.specificHeatCapacity * tempDiff;

                    // Dump heat directly into this building
                    float buildingMass = primaryElement.Mass;
                    float buildingSHC = primaryElement.Element.specificHeatCapacity;

                    if (buildingMass > 0f && buildingSHC > 0f)
                    {
                        primaryElement.Temperature += heatExtractedDTU / (buildingMass * buildingSHC);
                    }
                }

                // Consume the gas mass
                SimHashes liquidHash = liquidElement.id;
                byte diseaseIdx = pe.DiseaseIdx;
                int diseaseCount = pe.DiseaseCount;

                storage.ConsumeIgnoringDisease(targetGasItem);

                // Add 10 kg liquid to storage for dispensing
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
                operational.SetActive(false);
            }
        }
    }
}