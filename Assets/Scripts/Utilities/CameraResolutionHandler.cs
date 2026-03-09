using UnityEngine;

public class CameraResolutionHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform plateSet;

    private const float ReferenceAspect = 9f / 16f;
    private const float ReferenceOrthoSize = 10f;

    // 상단 UI(타이머 등), 하단 UI(아이템 버튼) 월드 유닛 높이
    [SerializeField] private float topUIWorldHeight = 1.8f;
    [SerializeField] private float bottomUIWorldHeight = 2.2f;

    private void Awake()
    {
        AdjustCamera();
        AdjustPlateSet();
    }

    private void AdjustCamera()
    {
        float currentAspect = (float)Screen.width / Screen.height;

        // 세로 기준 유지: 현재 비율이 기준보다 좁으면(iPad 등) ortho size 축소
        if (currentAspect < ReferenceAspect)
        {
            mainCamera.orthographicSize = ReferenceOrthoSize * (ReferenceAspect / currentAspect);
        }
        else
        {
            mainCamera.orthographicSize = ReferenceOrthoSize;
        }
    }

    private void AdjustPlateSet()
    {
        if (plateSet == null) return;

        // 현재 카메라가 보여주는 세로 월드 유닛
        float visibleHeight = mainCamera.orthographicSize * 2f;

        // UI 제외 게임 영역 높이
        float gameAreaHeight = visibleHeight - topUIWorldHeight - bottomUIWorldHeight;

        // PlateSet의 현재 실제 높이 계산
        var bounds = CalculateBounds(plateSet);
        float plateSetHeight = bounds.size.y;

        // 게임 영역보다 PlateSet이 크면 scale down
        if (plateSetHeight > gameAreaHeight)
        {
            float scale = gameAreaHeight / plateSetHeight;
            plateSet.localScale = Vector3.one * scale;
        }

        // PlateSet을 게임 영역 중앙으로 정렬
        float cameraY = mainCamera.transform.position.y;
        float topBound = cameraY + mainCamera.orthographicSize - topUIWorldHeight;
        float targetCenterY = topBound - gameAreaHeight * 0.5f;
        plateSet.position = new Vector3(plateSet.position.x, targetCenterY, plateSet.position.z);
    }

    private Bounds CalculateBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }
}