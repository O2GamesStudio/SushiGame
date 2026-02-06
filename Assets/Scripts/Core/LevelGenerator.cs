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

        int sushisPerType = (levelData.totalSushiCount / levelData.sushiTypeCount / 3) * 3;
        int remainingSushis = levelData.totalSushiCount - (sushisPerType * levelData.sushiTypeCount);
        remainingSushis = (remainingSushis / 3) * 3;

        for (int typeId = 1; typeId <= levelData.sushiTypeCount; typeId++)
        {
            int count = sushisPerType;

            if (typeId == 1 && remainingSushis > 0)
            {
                count += remainingSushis;
            }

            for (int i = 0; i < count; i++)
            {
                allSushiTypes.Add(typeId);
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

            if (HasSameThree(plateData.ActiveTypes))
            {
                FixSameThree(plateData.ActiveTypes, ref index);
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

                if (HasSameThree(layerTypes))
                {
                    FixSameThree(layerTypes, ref index);
                }

                usedSlots += layerTypes.Count;
                plateData.Layers.Add(new Layer(layerTypes));
            }

            plates.Add(plateData);
        }

        int freeSpace = totalCapacity - usedSlots;

        return plates;
    }

    private bool HasSameThree(List<int> types)
    {
        if (types.Count != 3) return false;
        return types[0] == types[1] && types[1] == types[2];
    }

    private void FixSameThree(List<int> types, ref int currentIndex)
    {
        if (currentIndex >= allSushiTypes.Count) return;

        int swapTarget = types[2];

        for (int i = currentIndex; i < allSushiTypes.Count; i++)
        {
            if (allSushiTypes[i] != types[0])
            {
                types[2] = allSushiTypes[i];
                allSushiTypes[i] = swapTarget;
                return;
            }
        }

        for (int i = 0; i < currentIndex; i++)
        {
            if (allSushiTypes[i] != types[0])
            {
                types[2] = allSushiTypes[i];
                allSushiTypes[i] = swapTarget;
                return;
            }
        }
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