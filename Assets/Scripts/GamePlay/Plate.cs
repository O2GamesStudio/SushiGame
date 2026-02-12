using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public enum PlateState
{
    Normal,
    LockedAd,
    LockedSushi
}

[RequireComponent(typeof(BoxCollider2D))]
public class Plate : MonoBehaviour
{
    [SerializeField] private Transform[] sushiSlots = new Transform[3];

    [Header("Plate State")]
    [SerializeField] private PlateState plateState = PlateState.Normal;
    [SerializeField] private int requiredSushiTypeId = -1;

    [Header("Refill Animation")]
    [SerializeField] private float refillDuration = 0.5f;
    [SerializeField] private float refillStartOffsetY = 2f;

    [Header("VFX")]
    [SerializeField] private GameObject mergeParticleVFXPrefab;

    private List<Sushi> activeSushis = new List<Sushi>(3) { null, null, null };
    private Queue<Layer> layerQueue = new Queue<Layer>();
    private PlateUI plateUI;
    private HashSet<Sushi> animatingSushis = new HashSet<Sushi>();

    public int ActiveCount => activeSushis.Count(s => s != null);
    public int LayerCount => layerQueue.Count;
    public bool IsFull => ActiveCount >= 3;
    public bool IsEmpty => ActiveCount == 0 && LayerCount == 0;
    public Layer CurrentNextLayer => layerQueue.Count > 0 ? layerQueue.Peek() : null;
    public PlateState State => plateState;
    public bool IsLocked => plateState != PlateState.Normal;
    public int RequiredSushiTypeId => requiredSushiTypeId;

    private void Awake()
    {
        plateUI = GetComponent<PlateUI>();
    }

    public void Initialize(List<int> activeTypes, List<Layer> layers, List<int> activeLockStages = null)
    {
        activeSushis = new List<Sushi>(3) { null, null, null };
        layerQueue.Clear();

        foreach (var layer in layers)
        {
            layerQueue.Enqueue(layer);
        }

        int index = 0;
        foreach (var typeId in activeTypes)
        {
            var sushi = SushiPool.Instance.Get(typeId);
            activeSushis[index] = sushi;
            sushi.SetCurrentPlate(this);

            if (activeLockStages != null && index < activeLockStages.Count && activeLockStages[index] > 0)
            {
                sushi.SetLockStage(activeLockStages[index]);
                SushiLockSystem.Instance?.RegisterLockedSushi(sushi);
            }

            index++;
        }
        UpdateVisuals();
    }

    public int GetClosestSlotIncludingCurrent(Vector3 worldPosition, Sushi currentSushi)
    {
        if (IsLocked) return -1;

        int closestSlot = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < 3; i++)
        {
            if (activeSushis[i] == null || activeSushis[i] == currentSushi)
            {
                float distance = Vector3.Distance(sushiSlots[i].position, worldPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestSlot = i;
                }
            }
        }

        return closestSlot;
    }

    public void SetState(PlateState newState, int sushiTypeId = -1)
    {
        plateState = newState;
        requiredSushiTypeId = sushiTypeId;
        plateUI?.UpdateLockState(plateState, sushiTypeId);
        UpdateSushiVisibility();
    }

    public void Unlock()
    {
        plateState = PlateState.Normal;
        plateUI?.UpdateLockState(plateState, -1);
        UpdateSushiVisibility();
        UpdateVisuals();
    }

    private void UpdateSushiVisibility()
    {
        bool shouldHide = IsLocked;

        for (int i = 0; i < 3; i++)
        {
            if (activeSushis[i] != null)
            {
                activeSushis[i].gameObject.SetActive(!shouldHide);
            }
        }
    }

    public bool MoveSushiWithinPlate(Sushi sushi, int targetSlot)
    {
        if (IsLocked || sushi.IsLocked || targetSlot < 0 || targetSlot >= 3) return false;
        if (activeSushis[targetSlot] != null) return false;

        for (int i = 0; i < 3; i++)
        {
            if (activeSushis[i] == sushi)
            {
                activeSushis[i] = null;
                activeSushis[targetSlot] = sushi;
                UpdateVisuals();
                return true;
            }
        }

        return false;
    }

    public void AddSushi(Sushi sushi, int preferredSlot = -1)
    {
        if (IsFull || IsLocked) return;

        if (ContainsSushi(sushi))
        {
            Debug.LogWarning("[Plate] 이미 이 플레이트에 있는 초밥을 추가하려고 시도했습니다.");
            return;
        }

        if (preferredSlot >= 0 && preferredSlot < 3 && activeSushis[preferredSlot] == null)
        {
            activeSushis[preferredSlot] = sushi;
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                if (activeSushis[i] == null)
                {
                    activeSushis[i] = sushi;
                    break;
                }
            }
        }

        sushi.SetCurrentPlate(this);
        UpdateVisuals();
        CheckMerge();
    }

    public bool RemoveSpecificSushi(Sushi sushi, bool forceRemove = false, bool skipRefill = false)
    {
        if (!forceRemove && (IsLocked || sushi.IsLocked)) return false;

        bool removed = false;
        for (int i = 0; i < 3; i++)
        {
            if (activeSushis[i] == sushi)
            {
                activeSushis[i] = null;
                removed = true;
                break;
            }
        }

        if (removed)
        {
            UpdateVisuals();

            if (!skipRefill && ActiveCount == 0 && LayerCount > 0)
            {
                RefillFromNextLayer();
                plateUI?.UpdateNextLayerDisplay(CurrentNextLayer);
                plateUI?.UpdateReservePlates(LayerCount);
            }
        }

        return removed;
    }

    public void RemoveLayer(int index)
    {
        if (index < 0 || index >= layerQueue.Count) return;

        var layersList = new List<Layer>(layerQueue);

        if (index < layersList.Count)
        {
            layersList.RemoveAt(index);
        }

        layerQueue.Clear();
        foreach (var layer in layersList)
        {
            layerQueue.Enqueue(layer);
        }
    }

    public bool ContainsSushi(Sushi sushi)
    {
        return activeSushis.Contains(sushi);
    }

    public int GetClosestEmptySlot(Vector3 worldPosition)
    {
        if (IsLocked) return -1;

        int closestSlot = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < 3; i++)
        {
            if (activeSushis[i] == null)
            {
                float distance = Vector3.Distance(sushiSlots[i].position, worldPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestSlot = i;
                }
            }
        }

        return closestSlot;
    }

    public List<Sushi> GetActiveSushis()
    {
        return activeSushis.Where(s => s != null).ToList();
    }

    public List<Layer> GetAllLayers()
    {
        return new List<Layer>(layerQueue);
    }

    public void UpdateReserveDisplay()
    {
        plateUI?.UpdateNextLayerDisplay(CurrentNextLayer);
    }

    public void CheckAndRefill()
    {
        if (ActiveCount == 0 && LayerCount > 0)
        {
            RefillFromNextLayer();
            plateUI?.UpdateNextLayerDisplay(CurrentNextLayer);
            plateUI?.UpdateReservePlates(LayerCount);
        }
    }

    private void CheckMerge()
    {
        if (ActiveCount != 3) return;

        var nonNullSushis = activeSushis.Where(s => s != null).ToList();

        if (nonNullSushis.Any(s => s.IsLocked)) return;

        if (nonNullSushis.Count == 3 &&
            nonNullSushis[0].TypeId == nonNullSushis[1].TypeId &&
            nonNullSushis[1].TypeId == nonNullSushis[2].TypeId)
        {
            ExecuteMerge(nonNullSushis[0].TypeId);
        }
    }

    private void ExecuteMerge(int mergedTypeId)
    {
        var sushisToReturn = new HashSet<Sushi>();

        foreach (var sushi in activeSushis)
        {
            if (sushi != null)
            {
                sushisToReturn.Add(sushi);
            }
        }

        if (mergeParticleVFXPrefab != null)
        {
            var vfx = LeanPool.Spawn(mergeParticleVFXPrefab, transform.position, Quaternion.identity);

            var particleSystem = vfx.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                float duration = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
                LeanPool.Despawn(vfx, duration);
            }
            else
            {
                LeanPool.Despawn(vfx, 2f);
            }
        }

        foreach (var sushi in sushisToReturn)
        {
            SushiLockSystem.Instance?.ClearLockedSushi(sushi);
            SushiPool.Instance.Return(sushi);
        }

        activeSushis = new List<Sushi>(3) { null, null, null };

        SushiLockSystem.Instance?.OnMergeCompleted();
        PlateUnlockSystem.Instance?.OnSushiMerged(mergedTypeId);

        RefillFromNextLayer();

        plateUI?.UpdateNextLayerDisplay(CurrentNextLayer);
        plateUI?.UpdateReservePlates(LayerCount);

        if (IsEmpty)
        {
            GameStateChecker.Instance.CheckWinCondition();
        }
    }

    private void CheckNextLayerRefill()
    {
        if (ActiveCount == 0 && LayerCount > 0)
        {
            RefillFromNextLayer();
            plateUI?.UpdateNextLayerDisplay(CurrentNextLayer);
            plateUI?.UpdateReservePlates(LayerCount);
        }
    }

    public void RecheckMerge()
    {
        CheckMerge();
    }

    private void RefillFromNextLayer()
    {
        if (layerQueue.Count > 0)
        {
            var nextLayer = layerQueue.Dequeue();
            var types = nextLayer.GetAllTypes();
            var slotIndices = nextLayer.SlotIndices;
            var lockStages = nextLayer.GetLockStages();

            for (int i = 0; i < types.Count; i++)
            {
                var sushi = SushiPool.Instance.Get(types[i]);
                activeSushis[slotIndices[i]] = sushi;
                sushi.SetCurrentPlate(this);

                if (lockStages[i] > 0)
                {
                    sushi.SetLockStage(lockStages[i]);
                    SushiLockSystem.Instance?.RegisterLockedSushi(sushi);
                }

                sushi.transform.SetParent(sushiSlots[slotIndices[i]]);

                Vector3 targetPos = sushiSlots[slotIndices[i]].position;
                Vector3 startPos = targetPos + Vector3.down * refillStartOffsetY;
                sushi.transform.position = startPos;
                sushi.transform.localScale = Vector3.one * 0.5f;

                animatingSushis.Add(sushi);

                sushi.transform.DOMove(targetPos, refillDuration)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => animatingSushis.Remove(sushi));

                sushi.transform.DOScale(Vector3.one, refillDuration)
                    .SetEase(Ease.OutBack);
            }
        }
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < 3; i++)
        {
            if (activeSushis[i] != null && !animatingSushis.Contains(activeSushis[i]))
            {
                var sushi = activeSushis[i];
                sushi.transform.SetParent(sushiSlots[i]);
                sushi.transform.position = sushiSlots[i].position;
                sushi.transform.localPosition = new Vector3(0, 0, -1);
                sushi.transform.localScale = Vector3.one;
                sushi.gameObject.SetActive(!IsLocked);
            }
        }

        plateUI?.UpdateNextLayerDisplay(CurrentNextLayer);
        plateUI?.UpdateReservePlates(LayerCount);
    }
}