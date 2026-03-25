using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MergeEventSystem : MonoBehaviour
{
    public static MergeEventSystem Instance { get; private set; }

    [SerializeField] private PlateManager plateManager;
    [SerializeField] private MergeEventUI eventUI;
    [SerializeField] private TrainCtrl trainCtrl;
    [SerializeField] private AudioClip eventSFX;

    private MergeEventData[] eventDataList;
    private int currentEventIndex = 0;
    private bool isEventActive = false;
    private bool isEventCompleting = false;
    private List<int> specialTargetTypes = new List<int>();
    private List<int> normalTargetTypes = new List<int>();
    private List<Plate> specialPlates = new List<Plate>();
    private float eventTimeRemaining = 0f;
    private float totalEventTime = 0f;

    public bool IsEventActive => isEventActive;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!isEventActive || isEventCompleting) return;

        eventTimeRemaining -= Time.deltaTime;
        eventUI?.UpdateTimer(eventTimeRemaining, totalEventTime);

        if (eventTimeRemaining <= 0f)
        {
            isEventActive = false;
            GameManager.Instance?.OnGameLose(true);
        }
    }

    public void ForceCompleteEvent()
    {
        isEventActive = false;
        isEventCompleting = false;
        currentEventIndex++;
        eventUI?.HideEvent();
    }

    public void Initialize(MergeEventData[] events, int specialPlateCount)
    {
        eventDataList = events;
        currentEventIndex = 0;
        isEventActive = false;
        isEventCompleting = false;
        specialPlates.Clear();
        eventUI?.HideEvent();

        if (specialPlateCount > 0)
        {
            var availablePlates = GetAvailableNonLockedPlates();
            var selected = availablePlates.OrderBy(_ => Random.value).Take(specialPlateCount).ToList();
            foreach (var plate in selected)
            {
                plate.SetSpecialPlate(true);
                specialPlates.Add(plate);
            }
        }
    }

    public void OnSushiMerged(int mergedCount)
    {
        if (eventDataList == null || currentEventIndex >= eventDataList.Length) return;
        if (isEventActive) return;

        var nextEvent = eventDataList[currentEventIndex];
        if (mergedCount >= nextEvent.mergeTriggerCount)
            StartEvent(nextEvent);
    }

    public void OnSushiMergedDuringEvent(int typeId, Plate plate)
    {
        if (!isEventActive || isEventCompleting) return;

        bool isSpecialPlate = plate != null && specialPlates.Contains(plate);

        Debug.Log($"[Event] 머지 감지 - typeId:{typeId} / isSpecialPlate:{isSpecialPlate} / specialTargets:{string.Join(",", specialTargetTypes)} / normalTargets:{string.Join(",", normalTargetTypes)}");

        if (isSpecialPlate)
        {
            if (!specialTargetTypes.Contains(typeId))
            {
                Debug.Log("[Event] 특수판 - 타겟 아님, 무시");
                return;
            }
            specialTargetTypes.Remove(typeId);
            eventUI?.RemoveSushi(typeId);
            Debug.Log($"[Event] 특수판 머지 완료 - 남은 특수 타겟:{string.Join(",", specialTargetTypes)}");
        }
        else
        {
            if (!normalTargetTypes.Contains(typeId))
            {
                Debug.Log("[Event] 일반판 - 타겟 아님, 무시");
                return;
            }
            normalTargetTypes.Remove(typeId);
            eventUI?.RemoveSushi(typeId);
            Debug.Log($"[Event] 일반판 머지 완료 - 남은 일반 타겟:{string.Join(",", normalTargetTypes)}");
        }

        if (specialTargetTypes.Count == 0 && normalTargetTypes.Count == 0)
        {
            isEventCompleting = true;
            CompleteEvent();
        }
    }

    private void StartEvent(MergeEventData data)
    {
        isEventActive = true;
        isEventCompleting = false;
        specialTargetTypes.Clear();
        normalTargetTypes.Clear();

        var availableTypes = GetAvailableSushiTypes();
        var shuffled = availableTypes.OrderBy(_ => Random.value).ToList();
        int totalCount = Mathf.Min(data.eventSushiCount, shuffled.Count);

        for (int i = 0; i < totalCount; i++)
        {
            if (i < data.requiredSpecialMergeCount)
                specialTargetTypes.Add(shuffled[i]);
            else
                normalTargetTypes.Add(shuffled[i]);
        }

        if (totalCount == 1)
            totalEventTime = 20f;
        else if (totalCount == 2)
            totalEventTime = 20f * 2 + 10f;
        else
            totalEventTime = 20f * 3 + 20f;

        eventTimeRemaining = totalEventTime;

        SoundManager.Instance?.PlaySFX(eventSFX);

        var allTargets = specialTargetTypes.Concat(normalTargetTypes).ToList();
        Debug.Log($"[Event] 이벤트 시작 - 특수타겟:{string.Join(",", specialTargetTypes)} / 일반타겟:{string.Join(",", normalTargetTypes)}");
        eventUI?.ShowEvent(allTargets, data.requiredSpecialMergeCount);
    }

    private void CompleteEvent()
    {
        isEventActive = false;
        isEventCompleting = false;
        currentEventIndex++;
        eventUI?.HideEvent();
        trainCtrl?.PlayOnEventComplete();
    }

    private List<Plate> GetAvailableNonLockedPlates()
    {
        return plateManager.GetAllPlates()
            .Where(p => p.gameObject.activeSelf && !p.IsLocked && p.State == PlateState.Normal && p.SlotCount == 3)
            .ToList();
    }

    private List<int> GetAvailableSushiTypes()
    {
        var types = new HashSet<int>();
        foreach (var plate in plateManager.GetAllPlates())
        {
            if (!plate.gameObject.activeSelf || plate.IsLocked) continue;
            foreach (var sushi in plate.GetActiveSushis())
                types.Add(sushi.TypeId);
        }
        return types.ToList();
    }
}