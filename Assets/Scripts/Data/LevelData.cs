using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "SushiMerge/LevelData")]
public class LevelData : ScriptableObject
{
    public int plateCount = 9;
    public int sushiTypeCount = 4;
    public int totalSushiCount = 36;
    public int minLayersPerPlate = 1;
    public int maxLayersPerPlate = 3;
    public float timeLimitSeconds = 300f;
    public int guaranteedMergeSets = 2;
}