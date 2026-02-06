using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class SushiPool : MonoBehaviour
{
    public static SushiPool Instance { get; private set; }

    [SerializeField] private Sushi sushiPrefab;
    [SerializeField] private List<SushiData> sushiDataList;

    private ObjectPool<Sushi> pool;
    private Dictionary<int, SushiData> dataDict;

    private void Awake()
    {
        Instance = this;

        dataDict = new Dictionary<int, SushiData>();
        foreach (var data in sushiDataList)
        {
            if (data != null)
            {
                dataDict[data.id] = data;
            }
        }

        pool = new ObjectPool<Sushi>(
            createFunc: () => Instantiate(sushiPrefab, transform),
            actionOnGet: sushi =>
            {
                sushi.gameObject.SetActive(true);
            },
            actionOnRelease: sushi =>
            {
                sushi.Reset();
                sushi.gameObject.SetActive(false);
            },
            actionOnDestroy: sushi => Destroy(sushi.gameObject),
            defaultCapacity: 60,
            maxSize: 100
        );
    }

    public Sushi Get(int typeId)
    {
        var sushi = pool.Get();
        if (dataDict.TryGetValue(typeId, out var data))
        {
            sushi.Initialize(typeId, data.sprite);
        }
        return sushi;
    }

    public void Return(Sushi sushi)
    {
        sushi.transform.SetParent(transform);
        pool.Release(sushi);
    }

    public SushiData GetData(int typeId)
    {
        return dataDict.TryGetValue(typeId, out var data) ? data : null;
    }
}