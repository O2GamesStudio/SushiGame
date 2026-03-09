using UnityEngine;
using DG.Tweening;

public class TrainCtrl : MonoBehaviour
{
    [Header("Train Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float startX = 12f;
    [SerializeField] private float endX = -12f;

    [Header("Smoke")]
    [SerializeField] private Transform smokeSpawnPoint1;
    [SerializeField] private Transform smokeSpawnPoint2;
    [SerializeField] private Sprite smokeSprite;
    [SerializeField] private string smokeSortingLayer = "Default";
    [SerializeField] private int smokeSortingOrder = 5;
    [SerializeField] private float smokeSpawnInterval = 0.8f;
    [SerializeField] private float smokeDuration = 1.2f;
    [SerializeField] private float smokeRiseHeight = 1.5f;
    [SerializeField] private float smokeStartScale = 0.4f;

    private float smokeTimer;

    private void OnEnable()
    {
        transform.position = new Vector3(startX, transform.position.y, transform.position.z);
        smokeTimer = 0f;
    }

    private void Update()
    {
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        if (transform.position.x <= endX)
            transform.position = new Vector3(startX, transform.position.y, transform.position.z);

        smokeTimer -= Time.deltaTime;
        if (smokeTimer <= 0f)
        {
            smokeTimer = smokeSpawnInterval;
            SpawnSmoke(smokeSpawnPoint1);
            SpawnSmoke(smokeSpawnPoint2);
        }
    }

    private void SpawnSmoke(Transform spawnPoint)
    {
        if (spawnPoint == null || smokeSprite == null) return;

        var smokeObj = new GameObject("Smoke");
        smokeObj.transform.position = spawnPoint.position;
        smokeObj.transform.localScale = Vector3.one * smokeStartScale;

        var sr = smokeObj.AddComponent<SpriteRenderer>();
        sr.sprite = smokeSprite;
        sr.sortingLayerName = smokeSortingLayer;
        sr.sortingOrder = smokeSortingOrder;

        smokeObj.transform
            .DOMove(spawnPoint.position + Vector3.up * smokeRiseHeight, smokeDuration)
            .SetEase(Ease.OutQuad);

        smokeObj.transform
            .DOScale(Vector3.zero, smokeDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => Destroy(smokeObj));
    }
}