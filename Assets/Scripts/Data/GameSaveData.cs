using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public int stageIndex;
    public float timeRemaining;
    public int mergedSetsCount;
    public long savedTimestamp;
    public List<PlateSaveData> plateStates = new List<PlateSaveData>();
    public RailSaveData railState;
}

[System.Serializable]
public class PlateSaveData
{
    public int plateIndex;
    public List<int> activeTypes = new List<int>();
    public List<LayerSaveData> layers = new List<LayerSaveData>();
    public List<int> activeLockStages = new List<int>();
    public int slotCount = 3;
    public int state;
    public int requiredSushiTypeId = -1;
}

[System.Serializable]
public class LayerSaveData
{
    public List<int> sushiTypes = new List<int>();
    public List<int> lockStages = new List<int>();
    public List<bool> hiddenStates = new List<bool>();
}

[System.Serializable]
public class RailSaveData
{
    public int rowIndex;
    public List<int> sushiTypeIds = new List<int>();
}