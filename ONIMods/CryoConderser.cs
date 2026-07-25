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

        public void Sim200ms(float dt)
        {
            // Must be powered and turned on
            if (!operational.IsOperational) return;

            for (int i = storage.items.Count - 1; i >= 0; i--)
            {
                GameObject item = storage.items[i];
                if (item == null) continue;

                PrimaryElement pe = item.GetComponent<PrimaryElement>();
                if (pe != null && pe.Mass > 0f)
                {
                    Element element = pe.Element;

                    // Check if incoming element is gas and has a liquid state transition
                    if (element.IsGas && element.lowTempTransition != null)
                    {
                        // Target liquid temperature: slightly below condensation point
                        float targetTemp = element.lowTemp - 5f;
                        float tempDifference = pe.Temperature - targetTemp;

                        if (tempDifference > 0f)
                        {
                            // Heat extracted in DTU
                            float heatExtractedDTU = pe.Mass * element.specificHeatCapacity * tempDifference;

                            // Heat the building up
                            float buildingMass = primaryElement.Mass;
                            float buildingSHC = primaryElement.Element.specificHeatCapacity;

                            if (buildingMass > 0f && buildingSHC > 0f)
                            {
                                float buildingTempDelta = heatExtractedDTU / (buildingMass * buildingSHC);
                                primaryElement.Temperature += buildingTempDelta;
                            }
                        }

                        // Store conversion properties
                        SimHashes liquidHash = element.lowTempTransition.id;
                        byte diseaseIdx = pe.DiseaseIdx;
                        int diseaseCount = pe.DiseaseCount;
                        float mass = pe.Mass;

                        // Consume the gas item from storage
                        storage.ConsumeIgnoringDisease(item);

                        // Add converted liquid straight into Storage 
                        // (ConduitDispenser will automatically pull liquid from Storage into the output pipe)
                        Element liquidElement = ElementLoader.FindElementByHash(liquidHash);
                        storage.AddLiquid(
                            liquidHash,
                            mass,
                            targetTemp,
                            diseaseIdx,
                            diseaseCount,
                            false,
                            true
                        );

                        break; // Process 1 gas unit per tick
                    }
                }
            }
        }
    }
}