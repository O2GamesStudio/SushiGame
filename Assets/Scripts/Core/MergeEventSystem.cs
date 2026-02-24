using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MergeEventSystem : MonoBehaviour
{
    public static MergeEventSystem Instance { get; private set; }

    [SerializeField] private PlateManager plateManager;
    [SerializeField] private MergeEventUI eventUI;
    [SerializeField] private float timePerSushi = 20f;

    private MergeEventData[] eventDataList;
    private int currentEventIndex = 0;
    private bool isEventActive = false;
    private List<int> targetSushiTypes = new List<int>();
    private float eventTimeRemaining = 0f;
    private float totalEventTime = 0f;

    public bool IsEventActive => isEventActive;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!isEventActive) return;

        eventTimeRemaining -= Time.deltaTime;
        eventUI?.UpdateTimer(eventTimeRemaining, totalEventTime);

        if (eventTimeRemaining <= 0f)
        {
            isEventActive = false;
            GameManager.Instance?.OnGameLose();
        }
    }

    public void Initialize(MergeEventData[] events)
    {
        eventDataList = events;
        currentEventIndex = 0;
        isEventActive = false;
        eventUI?.HideEvent();
    }

    public void OnSushiMerged(int mergedCount)
    {
        if (eventDataList == null || currentEventIndex >= eventDataList.Length) return;
        if (isEventActive) return;

        var nextEvent = eventDataList[currentEventIndex];
        if (mergedCount >= nextEvent.mergeTriggerCount)
            StartEvent(nextEvent);
    }

    public void OnSushiMergedDuringEvent(int typeId)
    {
        if (!isEventActive) return;

        Debug.Log($"[MergeEventSystem] 이벤트 중 머지 - typeId:{typeId} / 현재 타겟:{string.Join(", ", targetSushiTypes)}");

        if (!targetSushiTypes.Contains(typeId)) return;

        targetSushiTypes.Remove(typeId);
        eventUI?.RemoveSushi(typeId);

        Debug.Log($"[MergeEventSystem] 타겟 제거 후 남은 타겟:{string.Join(", ", targetSushiTypes)}");

        if (targetSushiTypes.Count == 0)
            CompleteEvent();
    }

    private void StartEvent(MergeEventData data)
    {
        var availableTypes = GetAvailableSushiTypes();
        if (availableTypes.Count == 0) return;

        targetSushiTypes.Clear();

        var shuffled = availableTypes.OrderBy(_ => Random.value).ToList();
        int count = Mathf.Min(data.eventSushiCount, shuffled.Count);

        for (int i = 0; i < count; i++)
            targetSushiTypes.Add(shuffled[i]);

        totalEventTime = targetSushiTypes.Count * timePerSushi;
        eventTimeRemaining = totalEventTime;
        isEventActive = true;

        Debug.Log($"[MergeEventSystem] 이벤트 시작 - index:{currentEventIndex} 타입:{string.Join(", ", targetSushiTypes)} 시간:{totalEventTime}");

        eventUI?.ShowEvent(targetSushiTypes);
    }

    private void CompleteEvent()
    {
        Debug.Log($"[MergeEventSystem] 이벤트 완료 - index:{currentEventIndex}");

        isEventActive = false;
        currentEventIndex++;
        eventUI?.HideEvent();
    }

    private List<int> GetAvailableSushiTypes()
    {
        var types = new HashSet<int>();

        foreach (var plate in plateManager.GetAllPlates())
        {
            if (!plate.gameObject.activeSelf || plate.IsLocked) continue;

            foreach (var sushi in plate.GetActiveSushis())
                types.Add(sushi.TypeId);

            foreach (var layer in plate.GetAllLayers())
                foreach (var typeId in layer.SushiTypes)
                    types.Add(typeId);
        }

        return types.ToList();
    }
}