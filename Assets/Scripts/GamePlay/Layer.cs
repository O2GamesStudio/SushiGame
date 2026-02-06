using System.Collections.Generic;
using UnityEngine;

public class Layer
{
    public List<int> SushiTypes { get; private set; }
    public int Count => SushiTypes.Count;

    public Layer(List<int> sushiTypes)
    {
        SushiTypes = sushiTypes;
        
        if (Count == 3 && sushiTypes[0] == sushiTypes[1] && sushiTypes[1] == sushiTypes[2])
        {
            Debug.LogError("Layer validation failed: Same type 3 sushis in one layer!");
        }
    }

    public List<int> GetAllTypes()
    {
        return new List<int>(SushiTypes);
    }
}
