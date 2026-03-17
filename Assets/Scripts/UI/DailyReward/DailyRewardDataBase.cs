using UnityEngine;

[CreateAssetMenu(fileName = "DailyRewardDataBase", menuName = "SushiMerge/DailyRewardDataBase")]
public class DailyRewardDataBase : ScriptableObject
{
    [SerializeField] private DailyRewardData[] rewards;

    public int TotalDays => rewards.Length;
    public DailyRewardData Get(int day)
    {
        int index = day - 1;
        if (index < 0 || index >= rewards.Length) return null;
        return rewards[index];
    }
}