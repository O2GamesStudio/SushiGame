using UnityEngine;
using DG.Tweening;
using System.Linq;

public class InGameTutorialController : MonoBehaviour
{
    public static InGameTutorialController Instance { get; private set; }

    [SerializeField] private FingerIconAnimator fingerIconPrefab;
    [SerializeField] private PlateManager plateManager;
    [SerializeField] private float reShowDelay = 4f;
    [SerializeField] private Vector3 fingerOffset = new Vector3(0.3f, -0.3f, 0f);


    private FingerIconAnimator fingerIcon;
    private bool isTutorialActive = false;
    private bool isTutorialCompleted = false;
    private int tutorialTypeId = -1;
    private Vector3 fromPos;
    private Vector3 toPos;
    private float timeSinceHidden = 0f;
    private bool isHiddenByTouch = false;

    private void Awake() => Instance = this;

    private void Update()
    {
        if (!isTutorialActive || isTutorialCompleted) return;
        if (!isHiddenByTouch) return;

        timeSinceHidden += Time.deltaTime;
        if (timeSinceHidden >= reShowDelay)
        {
            isHiddenByTouch = false;
            timeSinceHidden = 0f;
            ShowFinger();
        }
    }

    public void StartTutorial()
    {
        if (isTutorialCompleted) return;

        // plate[0] = 2개짜리, plate[1] = 1개짜리 (ExtractGuaranteedSushis에서 {2,1} 순서 보장)
        var plates = plateManager.GetAllPlates();
        Plate twoPlate = null;
        Plate onePlate = null;
        int foundType = -1;

        foreach (var plate in plates)
        {
            if (!plate.gameObject.activeSelf || plate.IsLocked) continue;
            var activeSushis = plate.GetActiveSushis();
            if (activeSushis.Count == 2 && activeSushis[0].TypeId == activeSushis[1].TypeId)
            {
                twoPlate = plate;
                foundType = activeSushis[0].TypeId;
            }
        }

        if (twoPlate == null || foundType < 0) return;

        foreach (var plate in plates)
        {
            if (!plate.gameObject.activeSelf || plate.IsLocked || plate == twoPlate) continue;
            var activeSushis = plate.GetActiveSushis();
            if (activeSushis.Count > 0 && activeSushis.Any(s => s.TypeId == foundType))
            {
                onePlate = plate;
                break;
            }
        }

        if (onePlate == null) return;

        tutorialTypeId = foundType;
        fromPos = onePlate.transform.position + Vector3.up * 0.5f + fingerOffset;
        toPos = twoPlate.transform.position + Vector3.up * 0.5f + fingerOffset;

        fingerIcon = Instantiate(fingerIconPrefab);
        isTutorialActive = true;
        ShowFinger();
    }

    private void ShowFinger()
    {
        if (fingerIcon == null) return;
        fingerIcon.PlayMoveLoop(fromPos, toPos);
    }

    // InputHandler에서 터치 시 호출
    public void OnScreenTouched()
    {
        if (!isTutorialActive || isTutorialCompleted) return;
        fingerIcon?.Hide();
        isHiddenByTouch = true;
        timeSinceHidden = 0f;
    }

    // GameManager.OnSushiMerged에서 머지 타입 전달
    public void OnSushiMerged(int mergedTypeId)
    {
        if (!isTutorialActive || isTutorialCompleted) return;
        if (mergedTypeId != tutorialTypeId) return;

        isTutorialCompleted = true;
        isTutorialActive = false;
        fingerIcon?.Hide();
        if (fingerIcon != null) Destroy(fingerIcon.gameObject, 0.5f);
    }

    public bool IsActive => isTutorialActive && !isTutorialCompleted;
}