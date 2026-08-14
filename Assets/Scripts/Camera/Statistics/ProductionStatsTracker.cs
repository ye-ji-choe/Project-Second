using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화분(구근)이 케이스에 적재되는 "그 순간"마다 통계를 실시간으로 반영한다.
///
/// [연결 방법]
///   각 화분 케이스(Pot_Case 등)의 PotTrayBinder 컴포넌트 인스펙터에서:
///     - On Pot Bound (Int, Bool) 이벤트에 이 컴포넌트의 OnPotBound(int, bool)를 연결
///     - On Tray Reset (Int) 이벤트에 이 컴포넌트의 OnTrayReset(int)를 연결
///   (PotTrayBinder 프리팹에 미리 연결해두면, 그 프리팹으로 스폰되는 모든 케이스에 자동 적용된다.)
///
/// [집계 기준 - 최종 확정]
///   총 생산량   = 구근이 성공적으로 감지된(정상) 누적 개수. 심는 순간마다 감지 성공 시 +1.
///   구근 불량   = 구근을 심었는데 감지 실패한 누적 개수. 심는 순간마다 감지 실패 시 +1.
///   화분 불량   = 현재 처리 중인 케이스의 "남은 빈 칸 수" (실시간 값, 누적 아님).
///                 케이스가 대기 상태로 리셋되면 슬롯 개수(보통 6)로 되돌아가고,
///                 화분이 하나 적재될 때마다 1씩 줄어든다.
///   달성률(%)   = 총 생산량 / 목표 생산량 × 100
///   불량률(%)   = 구근 불량 / (총 생산량 + 구근 불량) × 100
///                 (분모는 "심으려고 시도한 전체 개수" = 정상 + 불량)
/// </summary>
[AddComponentMenu("Factory/Production Stats Tracker")]
public class ProductionStatsTracker : MonoBehaviour
{
    public static ProductionStatsTracker Instance { get; private set; }

    [Header("목표")]
    [Tooltip("목표 생산량(구근 개수).")]
    public int targetProduction = 100;

    [Header("생산량 UI")]
    public Text totalProductionText;
    public Text targetProductionText;
    public Text achievementRateText;

    [Header("불량개수 UI")]
    public Text bulbDefectText;        // 구근 불량 (누적)
    public Text potDefectRemainingText; // 화분 불량 (현재 케이스의 남은 빈 칸, 실시간)
    public Text defectRateText;

    [Header("표시 형식")]
    [Tooltip("퍼센트 표시 소수점 자리수.")]
    public int percentDecimals = 1;

    [Header("Debug")]
    [Tooltip("화분이 적재되거나 트레이가 리셋될 때마다 상세 로그를 출력한다.")]
    public bool verboseLog = true;

    // 누적 통계
    public int TotalProduction { get; private set; }   // 구근 정상 누적
    public int BulbDefectCount { get; private set; }    // 구근 불량 누적
    public int RemainingEmptySlots { get; private set; } // 화분 불량 (현재 케이스 기준 실시간)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[{name}] ProductionStatsTracker가 씬에 두 개 이상 있습니다. 기존 것을 사용합니다.", this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RefreshUI();
    }

    /// <summary>
    /// PotTrayBinder.onPotBound 이벤트에 연결한다.
    /// 화분(구근 포함)이 슬롯에 적재되는 순간마다 호출된다.
    /// </summary>
    /// <param name="remainingEmptySlots">이 케이스에서 아직 안 채워진 빈 칸 수.</param>
    /// <param name="bulbDetected">방금 적재된 화분에 구근이 정상적으로 감지됐는지.</param>
    public void OnPotBound(int remainingEmptySlots, bool bulbDetected)
    {
        RemainingEmptySlots = remainingEmptySlots;

        if (bulbDetected)
        {
            TotalProduction++;
        }
        else
        {
            BulbDefectCount++;
        }

        if (verboseLog)
        {
            Debug.Log(
                $"[{name}] 화분 적재 이벤트 수신: 구근감지={bulbDetected}, 남은 빈 칸={remainingEmptySlots} " +
                $"(누적: 총생산={TotalProduction}, 구근불량={BulbDefectCount})", this);
        }

        RefreshUI();
    }

    public void OnPotBound()
    {
        TotalProduction++;
        RefreshUI();
    }

    public void OnPotFailed()
    {
        BulbDefectCount++;
        RefreshUI();
    }



    /// <summary>
    /// PotTrayBinder.onTrayReset 이벤트에 연결한다.
    /// 새 케이스가 대기 위치에 들어와 트레이가 초기화될 때 호출된다.
    /// </summary>
    /// <param name="slotCount">이 트레이의 전체 슬롯 개수 (보통 6).</param>
    public void OnTrayReset(int slotCount)
    {
        RemainingEmptySlots = slotCount;

        if (verboseLog)
            Debug.Log($"[{name}] 트레이 리셋 이벤트 수신: 화분 불량(남은 빈 칸) → {slotCount}로 초기화", this);

        RefreshUI();
    }

    /// <summary>누적 통계를 전부 0으로 초기화한다. (라인 재시작 등에 사용.)</summary>
    public void ResetStats()
    {
        TotalProduction = 0;
        BulbDefectCount = 0;
        RemainingEmptySlots = 0;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (totalProductionText != null)
            totalProductionText.text = TotalProduction.ToString();

        if (targetProductionText != null)
            targetProductionText.text = targetProduction.ToString();

        if (achievementRateText != null)
            achievementRateText.text = FormatPercent(TotalProduction, targetProduction);

        if (bulbDefectText != null)
            bulbDefectText.text = BulbDefectCount.ToString();

        if (potDefectRemainingText != null)
            potDefectRemainingText.text = RemainingEmptySlots.ToString();

        if (defectRateText != null)
        {
            int attempted = TotalProduction + BulbDefectCount;
            defectRateText.text = FormatPercent(BulbDefectCount, attempted);
        }
    }

    private string FormatPercent(int numerator, int denominator)
    {
        if (denominator <= 0)
            return "0%";

        float percent = (float)numerator / denominator * 100f;
        return percent.ToString($"F{percentDecimals}") + "%";
    }
}