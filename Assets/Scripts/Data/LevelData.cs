using UnityEngine;
using System;

[CreateAssetMenu(fileName = "LevelData", menuName = "SushiMerge/LevelData")]
public class LevelData : ScriptableObject
{
    [Header("기본 설정")]
    [Tooltip("접시 개수")] public int plateCount = 9;
    [Tooltip("초밥 종류 수")] public int sushiTypeCount = 4;
    [Tooltip("총 초밥 개수")] public int totalSushiCount = 36;
    [Tooltip("접시당 최소 레이어")] public int minLayersPerPlate = 1;
    [Tooltip("접시당 최대 레이어")] public int maxLayersPerPlate = 3;
    [Tooltip("제한 시간 (초)")] public float timeLimitSeconds = 300f;
    [Tooltip("보장 머지 세트 수")] public int guaranteedMergeSets = 2;

    [Header("타입 분포")]
    [Tooltip("집중 배치할 타입 수")] public int concentratedTypeCount = 2;

    [Header("초기 빈 슬롯")]
    [Tooltip("시작 시 제거할 초밥 수")] public int sushiInitEraseCount = 0;

    [Header("레이어 크기 확률 (합계 100)")]
    [Tooltip("레이어 초밥 1개 확률")] public int layerSize1Weight = 25;
    [Tooltip("레이어 초밥 2개 확률")] public int layerSize2Weight = 50;
    [Tooltip("레이어 초밥 3개 확률")] public int layerSize3Weight = 25;

    [Header("특수판")]
    [Tooltip("특수판 수")] public int specialPlateCount = 0;

    [Header("히든 예비 초밥")]
    [Tooltip("숨겨진 예비 초밥 수")] public int hiddenReserveCount = 0;

    [Header("잠긴 접시")]
    [Tooltip("잠긴 접시 수")] public int lockedPlateCount = 0;
    [Tooltip("잠금 해제에 필요한 머지 수")] public int mergeUnlockCount = 0;

    [Header("잠긴 초밥")]
    [Tooltip("잠긴 초밥 수")] public int lockedSushiCount = 0;

    [Header("단일 슬롯 접시")]
    [Tooltip("슬롯이 1개인 접시 수")] public int singleSlotPlateCount = 0;
    [Header("순차 활성화")]
    [Tooltip("true시 plate가 순서대로 활성화됨")] public bool sequentialActivation = false;

    [Header("머지 이벤트")]
    public MergeEventData[] mergeEvents;
}

[Serializable]
public class MergeEventData
{
    [Tooltip("이벤트 발동 머지 횟수")] public int mergeTriggerCount;
    [Tooltip("이벤트 시 추가 초밥 수")] public int eventSushiCount;
    [Tooltip("특수판에서 머지해야 하는 횟수")] public int requiredSpecialMergeCount = 0;
}