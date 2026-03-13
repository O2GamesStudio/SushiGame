using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PassDataBase", menuName = "SushiMerge/PassDataBase")]
public class PassDataBase : ScriptableObject
{
    [SerializeField] private List<PassLevelData> levels;

    public int MaxLevel => levels.Count;

    public PassLevelData Get(int level)
    {
        int index = level - 1;
        if (index < 0 || index >= levels.Count) return null;
        return levels[index];
    }

    public int GetRequiredXP(int level)
    {
        var data = Get(level);
        return data?.requiredXP ?? 0;
    }
}