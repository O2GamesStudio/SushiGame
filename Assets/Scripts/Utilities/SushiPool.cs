using System.Collections.Generic;
using UnityEngine;

public class SushiPool : MonoBehaviour
{
    public static SushiPool Instance { get; private set; }

    [SerializeField] private GameObject sushiPrefab;
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private List<SushiData> sushiDataList = new List<SushiData>();

    private Queue<Sushi> pool = new Queue<Sushi>();
    private Dictionary<int, SushiData> dataMap = new Dictionary<int, SushiData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeDataMap();
        InitializePool();
    }

    private void InitializeDataMap()
    {
        dataMap.Clear();
        foreach (var data in sushiDataList)
        {
            if (data != null)
            {
                dataMap[data.typeId] = data;
            }
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            var sushi = CreateNewSushi();
            sushi.gameObject.SetActive(false);
            pool.Enqueue(sushi);
        }
    }

    private Sushi CreateNewSushi()
    {
        var sushiObj = Instantiate(sushiPrefab, transform);
        var sushi = sushiObj.GetComponent<Sushi>();
        return sushi;
    }

    public Sushi Get(int typeId)
    {
        var data = GetData(typeId);
        if (data == null)
        {
            Debug.LogError($"[SushiPool] SushiData를 찾을 수 없습니다: typeId={typeId}");
            return null;
        }

        var sushi = pool.Count > 0 ? pool.Dequeue() : CreateNewSushi();
        sushi.Initialize(typeId, data.riceSprite, data.toppingSprite, data.sushiType, data.toppingOffsetX, data.toppingOffsetY);
        sushi.gameObject.SetActive(true);
        return sushi;
    }

    public void Return(Sushi sushi)
    {
        if (sushi == null) return;

        sushi.Reset();
        sushi.gameObject.SetActive(false);
        sushi.transform.SetParent(transform);
        pool.Enqueue(sushi);
    }

    public SushiData GetData(int typeId)
    {
        return dataMap.ContainsKey(typeId) ? dataMap[typeId] : null;
    }

    public List<int> GetAllAvailableTypeIds()
    {
        return new List<int>(dataMap.Keys);
    }
}