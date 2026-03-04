using UnityEngine;

[CreateAssetMenu(fileName = "LevelDataBase", menuName = "Game/LevelDataBase")]
public class LevelDataBase : ScriptableObject
{
    [SerializeField] private LevelData[] levels;

    public LevelData Get(int stage)
    {
        int index = stage - 1;
        if (index < 0 || index >= levels.Length) return null;
        return levels[index];
    }

    public int Count => levels.Length;
}