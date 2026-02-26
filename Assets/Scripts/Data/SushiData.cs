using UnityEngine;

public enum SushiType
{
    [Tooltip("초밥")] Nigiri,
    [Tooltip("군함초밥")] Gunkan,
    [Tooltip("롤초밥")] Roll,
    [Tooltip("일체형")] Integrated
}

[CreateAssetMenu(fileName = "SushiData", menuName = "Game/SushiData")]
public class SushiData : ScriptableObject
{
    public int typeId;
    public SushiType sushiType = SushiType.Nigiri;
    public Sprite riceSprite;
    public Sprite toppingSprite;
    public float toppingOffsetX = 0f;
    public float toppingOffsetY = 0f;
    public float plateOffsetY = 0f;

}