using UnityEngine;

[CreateAssetMenu(fileName = "SushiData", menuName = "Game/SushiData")]
public class SushiData : ScriptableObject
{
    public int typeId;
    public Sprite riceSprite;
    public Sprite toppingSprite;
    public float toppingOffsetX = 0f;
    public float toppingOffsetY = 0f;
}