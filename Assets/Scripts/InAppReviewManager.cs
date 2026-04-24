using UnityEngine;
using Google.Play.Review;
using System.Collections;

public class InAppReviewManager : MonoBehaviour
{
    public static InAppReviewManager Instance { get; private set; }

    private ReviewManager reviewManager;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        reviewManager = new ReviewManager();
    }

    public void TryRequestReview(UserData userData)
    {
        if (userData == null) return;
        if (userData.hasReviewed) return;
        if (userData.currentStage < 6) return; // 5스테이지 클리어 = currentStage 6

        StartCoroutine(RequestReviewCoroutine(userData));
    }

    private IEnumerator RequestReviewCoroutine(UserData userData)
    {
        reviewManager = new ReviewManager();

        var requestFlow = reviewManager.RequestReviewFlow();
        yield return requestFlow;

        if (requestFlow.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"[InAppReview] RequestReviewFlow 실패: {requestFlow.Error}");
            yield break;
        }

        var launchFlow = reviewManager.LaunchReviewFlow(requestFlow.GetResult());
        yield return launchFlow;

        if (launchFlow.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"[InAppReview] LaunchReviewFlow 실패: {launchFlow.Error}");
            yield break;
        }

        // 리뷰 플로우 완료 - 실제 제출 여부는 알 수 없음 (Google 정책)
        MarkAsReviewed(userData);
    }

    private void MarkAsReviewed(UserData userData)
    {
        userData.hasReviewed = true;
        GameDataTransfer.Instance?.SetUserData(userData);

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        UserDataService.Instance?.UpdateFields(userId,
            new System.Collections.Generic.Dictionary<string, object>
            {
                { "hasReviewed", true }
            }, null);
    }
}