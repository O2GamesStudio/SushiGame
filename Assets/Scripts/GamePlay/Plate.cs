using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public enum PlateState
{
    Normal,
    LockedAd,
    LockedSushi,
    CantMerge
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
    [SerializeField] private float refillDelay = 0.3f;

    [Header("VFX")]
    [SerializeField] private GameObject mergeParticleVFXPrefab;
    [SerializeField] private SushiPackagingEffect packagingEffect;

    [Header("Hidden Reveal Animation")]
    [SerializeField] private float hiddenRevealDelay = 0.3f;
    [SerializeField] private float hiddenRevealDuration = 0.5f;

    private List<Sushi> activeSushis = new List<Sushi>();
    private Queue<Layer> layerQueue = new Queue<Layer>();
    private PlateUI plateUI;
    private HashSet<Sushi> animatingSushis = new HashSet<Sushi>();
    private int slotCount = 3;

    public bool IsSpecialPlate { get; private set; }
    public int SlotCount => slotCount;
    public int ActiveCount => activeSushis.Count(s => s != null);
    public int LayerCount => layerQueue.Count;
    public bool IsFull => ActiveCount >= slotCount;
    public bool IsEmpty => ActiveCount == 0 && LayerCount == 0;
    public Layer CurrentNextLayer => layerQueue.Count > 0 ? layerQueue.Peek() : null;
    public PlateState State => plateState;
    public bool IsLocked => plateState == PlateState.LockedAd || plateState == PlateState.LockedSushi;
    public int RequiredSushiTypeId => requiredSushiTypeId;
    private int GetSlotTransformIndex(int activeIndex) => slotCount == 1 ? 1 : activeIndex;
    private void Awake()
    {
        plateUI = GetComponent<PlateUI>();
    }

    public void Initialize(List<int> activeTypes, List<Layer> layers, List<int> activeLockStages = null, int slotCount = 3)
    {
        this.slotCount = slotCount;
        activeSushis = new List<Sushi>(slotCount);
        for (int i = 0; i < slotCount; i++)
            activeSushis.Add(null);

        layerQueue.Clear();
        foreach (var layer in layers)
            layerQueue.Enqueue(layer);

        // 같은 타입 3개 방어
        if (slotCount == 3 && activeTypes.Count == 3 &&
            activeTypes[0] == activeTypes[1] && activeTypes[1] == activeTypes[2])
            Debug.LogError($"[Plate] Initialize - 같은 타입 3개 감지: typeId={activeTypes[0]}");

        int index = 0;
        foreach (var typeId in activeTypes)
        {
            var sushi = SushiPool.Instance.Get(typeId);
            activeSushis[index] = sushi;
            sushi.SetCurrentPlate(this);

            if (activeLockStages != null && index < activeLockStages.Count && activeLockStages[index] > 0)
                SushiLockSystem.Instance?.RegisterLockedSushi(sushi, activeLockStages[index]);

            index++;
        }

        plateUI?.SetSlotCount(slotCount);
        UpdateVisuals();
    }
    public void SetSpecialPlate(bool isSpecial)
    {
        IsSpecialPlate = isSpecial;
        plateUI?.SetSpecialPlate(isSpecial);
    }

    public int GetClosestSlotIncludingCurrent(Vector3 worldPosition, Sushi currentSushi)
    {
        if (IsLocked) return -1;

        int closestSlot = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < slotCount; i++)
        {
            if (activeSushis[i] == null || activeSushis[i] == currentSushi)
            {
                int slotIdx = GetSlotTransformIndex(i);
                float distance = Vector3.Distance(sushiSlots[slotIdx].position, worldPosition);
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
        if (slotCount == 1) return;

        plateState = newState;
        requiredSushiTypeId = sushiTypeId;
        plateUI?.UpdateLockState(plateState, sushiTypeId);
        UpdateVisuals();
    }

    public void Unlock()
    {
        plateState = PlateState.Normal;
        UpdateSushiVisibility();
        plateUI?.PlayUnlockAnimation(() =>
        {
            plateUI?.UpdateLockState(plateState, -1);
            UpdateVisuals();
        });
    }

    private void UpdateSushiVisibility()
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (activeSushis[i] != null)
                activeSushis[i].gameObject.SetActive(true);
        }
    }

    public bool MoveSushiWithinPlate(Sushi sushi, int targetSlot)
    {
        if (IsLocked || sushi.IsLocked || targetSlot < 0 || targetSlot >= slotCount) return false;
        if (activeSushis[targetSlot] != null) return false;

        for (int i = 0; i < slotCount; i++)
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

        if (preferredSlot >= 0 && preferredSlot < slotCount && activeSushis[preferredSlot] == null)
        {
            activeSushis[preferredSlot] = sushi;
        }
        else
        {
            for (int i = 0; i < slotCount; i++)
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
        for (int i = 0; i < slotCount; i++)
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
        layersList.RemoveAt(index);

        layerQueue.Clear();
        foreach (var layer in layersList)
            layerQueue.Enqueue(layer);
    }

    public bool ContainsSushi(Sushi sushi) => activeSushis.Contains(sushi);

    public int GetClosestEmptySlot(Vector3 worldPosition)
    {
        if (IsLocked) return -1;

        int closestSlot = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < slotCount; i++)
        {
            if (activeSushis[i] == null)
            {
                int slotIdx = GetSlotTransformIndex(i);
                float distance = Vector3.Distance(sushiSlots[slotIdx].position, worldPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestSlot = i;
                }
            }
        }

        return closestSlot;
    }

    public void RefreshVisuals() => UpdateVisuals();
    public List<Sushi> GetActiveSushis() => activeSushis.Where(s => s != null).ToList();
    public List<Layer> GetAllLayers() => new List<Layer>(layerQueue);

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
        if (ActiveCount != slotCount) return;
        if (slotCount == 1) return;
        if (plateState == PlateState.CantMerge) return;

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
        var sushisToReturn = activeSushis.Where(s => s != null).ToList();
        activeSushis = new List<Sushi>(slotCount);
        for (int i = 0; i < slotCount; i++)
            activeSushis.Add(null);

        SushiLockSystem.Instance?.OnMergeCompleted();
        SpawnMergeParticle(transform.position);

        DOVirtual.DelayedCall(0.2f, () =>
        {
            if (packagingEffect != null)
            {
                packagingEffect.PlayPackagingEffect(
                    transform.position,
                    sushisToReturn,
                    (containerPosition) =>
                    {
                        PlateUnlockSystem.Instance?.OnSushiMerged(mergedTypeId);
                    },
                    () => OnPackagingComplete(sushisToReturn, mergedTypeId)
                );
            }
            else
            {
                PlateUnlockSystem.Instance?.OnSushiMerged(mergedTypeId);
                OnPackagingComplete(sushisToReturn, mergedTypeId);
            }
        });

        DOVirtual.DelayedCall(refillDelay, () =>
        {
            RefillFromNextLayer();
            plateUI?.UpdateNextLayerDisplay(CurrentNextLayer);
            plateUI?.UpdateReservePlates(LayerCount);
        });
    }

    private void SpawnMergeParticle(Vector3 position)
    {
        if (mergeParticleVFXPrefab == null) return;

        var vfx = LeanPool.Spawn(mergeParticleVFXPrefab, position, Quaternion.identity);
        var ps = vfx.GetComponent<ParticleSystem>();
        float duration = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f;
        LeanPool.Despawn(vfx, duration);

        SoundManager.Instance?.PlayMergeSFX();
    }

    private void OnPackagingComplete(List<Sushi> sushis, int mergedTypeId)
    {
        foreach (var sushi in sushis)
        {
            SushiLockSystem.Instance?.ClearLockedSushi(sushi);
            SushiPool.Instance.Return(sushi);
        }

        GameManager.Instance?.OnSushiMerged(mergedTypeId, this);

        if (IsEmpty)
            GameStateChecker.Instance.CheckWinCondition();
    }

    public void RecheckMerge() => CheckMerge();

    private void RefillFromNextLayer()
    {
        if (layerQueue.Count == 0) return;

        var nextLayer = layerQueue.Dequeue();
        var types = nextLayer.GetAllTypes();
        var slotIndices = nextLayer.SlotIndices;
        var lockStages = nextLayer.GetLockStages();
        var hiddenStates = nextLayer.GetHiddenStates();

        for (int i = 0; i < types.Count; i++)
        {
            var sushi = SushiPool.Instance.Get(types[i]);
            int activeIndex = slotCount == 1 ? 0 : slotIndices[i];
            int slotTransformIndex = GetSlotTransformIndex(activeIndex);

            activeSushis[activeIndex] = sushi;
            sushi.SetCurrentPlate(this);

            if (lockStages[i] > 0)
                SushiLockSystem.Instance?.RegisterLockedSushi(sushi, lockStages[i]);

            sushi.transform.SetParent(sushiSlots[slotTransformIndex]);

            Vector3 targetLocalPos = new Vector3(0f, 0.08f + sushi.PlateOffsetY, -1f);
            Vector3 targetWorldPos = sushiSlots[slotTransformIndex].TransformPoint(targetLocalPos);
            Vector3 startPos = targetWorldPos + Vector3.down * refillStartOffsetY;

            sushi.transform.position = startPos;
            sushi.transform.localScale = Vector3.one * 0.5f;

            animatingSushis.Add(sushi);

            bool isHidden = hiddenStates != null && i < hiddenStates.Count && hiddenStates[i];
            if (isHidden && plateUI != null)
                CreateHiddenOverlay(sushi, targetWorldPos);

            sushi.transform.DOLocalMove(targetLocalPos, refillDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => animatingSushis.Remove(sushi));

            sushi.transform.DOScale(Vector3.one, refillDuration)
                .SetEase(Ease.OutBack);
        }
    }

    public List<int> GetActiveTypes()
    {
        var result = new List<int>();
        for (int i = 0; i < slotCount; i++)
            if (activeSushis[i] != null)
                result.Add(activeSushis[i].TypeId);
        return result;
    }

    public List<int> GetActiveLockStages()
    {
        var result = new List<int>();
        for (int i = 0; i < slotCount; i++)
            result.Add(activeSushis[i] != null ? activeSushis[i].LockStage : 0);
        return result;
    }

    public PlateState CurrentState => plateState;
    public List<Layer> GetLayers() => new List<Layer>(layerQueue);

    private void CreateHiddenOverlay(Sushi sushi, Vector3 targetPos)
    {
        var hiddenSprite = plateUI.GetHiddenSushiSprite();
        if (hiddenSprite == null) return;

        sushi.SetRenderersVisible(false); // 올라오는 동안 숨김

        var hiddenObj = new GameObject("HiddenOverlay");
        hiddenObj.transform.position = sushi.transform.position;
        hiddenObj.transform.SetParent(sushi.transform);
        hiddenObj.transform.localScale = Vector3.one * 1.5f;

        var hiddenRenderer = hiddenObj.AddComponent<SpriteRenderer>();
        hiddenRenderer.sprite = hiddenSprite;
        hiddenRenderer.sortingLayerName = sushi.SpriteRenderer.sortingLayerName;
        hiddenRenderer.sortingOrder = 55;

        DOVirtual.DelayedCall(refillDuration, () =>
        {
            if (hiddenObj != null)
                RevealHiddenSushi(hiddenObj, sushi);
        });
    }
    private void RevealHiddenSushi(GameObject hiddenObj, Sushi sushi)
    {
        sushi.SetRenderersVisible(true); // hidden 사라지는 애니메이션 시작 시점에 표시

        Vector3 startPos = hiddenObj.transform.position;
        Vector3 targetPos = startPos + new Vector3(1.5f, 1.5f, 0);

        hiddenObj.transform.SetParent(null);

        hiddenObj.transform.DOMove(targetPos, hiddenRevealDuration).SetEase(Ease.OutQuad);
        hiddenObj.transform.DORotate(new Vector3(0, 0, -90), hiddenRevealDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad);
        hiddenObj.transform.DOScale(Vector3.zero, hiddenRevealDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => { if (hiddenObj != null) Destroy(hiddenObj); });
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < slotCount; i++)
        {
            var sushi = activeSushis[i];
            if (sushi == null || animatingSushis.Contains(sushi) || sushi.IsDragging) continue;

            int slotIdx = GetSlotTransformIndex(i);

            if (sushi.transform.parent != sushiSlots[slotIdx])
                sushi.transform.SetParent(sushiSlots[slotIdx]);

            sushi.transform.localPosition = new Vector3(0f, 0.08f + sushi.PlateOffsetY, -1f);
            sushi.transform.localScale = Vector3.one;
            sushi.gameObject.SetActive(!IsLocked);
        }

        plateUI?.UpdateNextLayerDisplay(CurrentNextLayer);
        plateUI?.UpdateReservePlates(LayerCount);
    }
}