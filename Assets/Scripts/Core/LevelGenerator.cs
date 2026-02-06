using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator
{
    private LevelData levelData;
    private List<int> allSushiTypes;

    public LevelGenerator(LevelData data)
    {
        levelData = data;
        GenerateSushiPool();
    }

    private void GenerateSushiPool()
    {
        allSushiTypes = new List<int>();

        int sushiPerType = Mathf.CeilToInt((float)levelData.totalSushiCount / levelData.sushiTypeCount / 3) * 3;

        for (int typeId = 1; typeId <= levelData.sushiTypeCount; typeId++)
        {
            for (int i = 0; i < sushiPerType; i++)
            {
                allSushiTypes.Add(typeId);
            }
        }

        while (allSushiTypes.Count > levelData.totalSushiCount)
        {
            for (int typeId = levelData.sushiTypeCount; typeId >= 1 && allSushiTypes.Count > levelData.totalSushiCount; typeId--)
            {
                int lastIndex = allSushiTypes.LastIndexOf(typeId);
                if (lastIndex >= 0)
                {
                    allSushiTypes.RemoveAt(lastIndex);
                }
            }
        }

        Shuffle(allSushiTypes);
    }

    public List<PlateData> GeneratePlates()
    {
        var plates = new List<PlateData>();
        int totalCapacity = levelData.plateCount * 3;
        int usedSlots = 0;
        int index = 0;

        for (int i = 0; i < levelData.plateCount; i++)
        {
            var plateData = new PlateData();

            int activeCount = Random.Range(1, 4);
            plateData.ActiveTypes = new List<int>();
            for (int j = 0; j < activeCount && index < allSushiTypes.Count; j++)
            {
                plateData.ActiveTypes.Add(allSushiTypes[index++]);
            }
            usedSlots += activeCount;

            int layerCount = Random.Range(levelData.minLayersPerPlate, levelData.maxLayersPerPlate + 1);
            plateData.Layers = new List<Layer>();

            for (int j = 0; j < layerCount && index < allSushiTypes.Count; j++)
            {
                int layerSize = Random.Range(1, 4);
                var layerTypes = new List<int>();

                for (int k = 0; k < layerSize && index < allSushiTypes.Count; k++)
                {
                    layerTypes.Add(allSushiTypes[index++]);
                }

                if (layerTypes.Count == 3 && layerTypes[0] == layerTypes[1] && layerTypes[1] == layerTypes[2])
                {
                    int swapIdx = Random.Range(0, allSushiTypes.Count - index);
                    if (swapIdx + index < allSushiTypes.Count)
                    {
                        (layerTypes[2], allSushiTypes[index + swapIdx]) = (allSushiTypes[index + swapIdx], layerTypes[2]);
                    }
                }

                usedSlots += layerTypes.Count;
                plateData.Layers.Add(new Layer(layerTypes));
            }

            plates.Add(plateData);
        }

        int freeSpace = totalCapacity - usedSlots;

        return plates;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

public class PlateData
{
    public List<int> ActiveTypes;
    public List<Layer> Layers;
}