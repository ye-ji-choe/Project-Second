using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ProductionStatsTracker의 통계 값 중 하나를 골라서,
/// 이 오브젝트의 AddressText(Text)에 실시간으로 표시한다.
///
/// [배치]
///   각 통계 행(총 생산량, 목표 생산량, 달성률, 구근 불량, 화분 불량, 불량률)
///   └── AddressText (Text)
///
/// [사용법]
///   Stat Field 드롭다운에서 이 행이 어떤 통계를 표시할지 선택하면 끝.
/// </summary>
[AddComponentMenu("Factory UI/Stat Value Display")]
public class StatValueDisplay : MonoBehaviour
{
    public enum StatField
    {
        TotalProduction,        // 총 생산량 (구근 정상 누적)
        TargetProduction,       // 목표 생산량
        AchievementRate,        // 달성률(%)
        BulbDefectCount,        // 구근 불량 (누적)
        RemainingEmptySlots,    // 화분 불량 (현재 케이스의 남은 빈 칸, 실시간)
        DefectRate              // 불량률(%)
    }

    [Tooltip("이 행이 표시할 통계 항목.")]
    public StatField statField = StatField.TotalProduction;

    [Tooltip("숫자를 표시할 Text. 비워두면 같은 오브젝트 또는 자식에서 자동으로 찾는다.")]
    public Text targetText;

    [Header("표시 형식")]
    [Tooltip("퍼센트 항목(달성률/불량률)의 소수점 자리수.")]
    public int percentDecimals = 1;

    [Tooltip("퍼센트 항목에 '%' 기호를 붙일지 여부. (라벨에 '[%]'가 이미 있다면 꺼도 됨)")]
    public bool appendPercentSign = true;

    [Header("갱신 주기")]
    public bool updateEveryFrame = true;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<Text>();
        if (targetText == null)
            targetText = GetComponentInChildren<Text>(true);

        if (targetText == null)
            Debug.LogWarning($"[{name}] 표시할 Text(AddressText)를 찾지 못했습니다. Target Text를 직접 연결하세요.", this);
    }

    private void OnEnable()
    {
        RefreshNow();
    }

    private void Update()
    {
        if (updateEveryFrame)
            RefreshNow();
    }

    public void RefreshNow()
    {
        if (targetText == null) return;

        ProductionStatsTracker tracker = ProductionStatsTracker.Instance;
        if (tracker == null)
        {
            targetText.text = "-";
            return;
        }

        targetText.text = ResolveDisplayString(tracker);
    }

    private string ResolveDisplayString(ProductionStatsTracker tracker)
    {
        switch (statField)
        {
            case StatField.TotalProduction:
                return tracker.TotalProduction.ToString();

            case StatField.TargetProduction:
                return tracker.targetProduction.ToString();

            case StatField.AchievementRate:
                return FormatPercent(tracker.TotalProduction, tracker.targetProduction);

            case StatField.BulbDefectCount:
                return tracker.BulbDefectCount.ToString();

            case StatField.RemainingEmptySlots:
                return tracker.RemainingEmptySlots.ToString();

            case StatField.DefectRate:
                int attempted = tracker.TotalProduction + tracker.BulbDefectCount;
                return FormatPercent(tracker.BulbDefectCount, attempted);

            default:
                return "-";
        }
    }

    private string FormatPercent(int numerator, int denominator)
    {
        if (denominator <= 0)
            return appendPercentSign ? "0%" : "0";

        float percent = (float)numerator / denominator * 100f;
        string formatted = percent.ToString($"F{percentDecimals}");
        return appendPercentSign ? formatted + "%" : formatted;
    }
}