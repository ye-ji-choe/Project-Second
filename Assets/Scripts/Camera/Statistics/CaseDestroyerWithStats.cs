using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 트리거 박스에 실제로 닿은 콜라이더를 근거로 삭제 대상을 판단한다.
///
/// [통계 연동 방식 변경 안내]
/// 이전 버전은 이 스크립트가 케이스를 삭제하는 시점에 통계를 등록했지만,
/// 이제 통계는 PotTrayBinder가 화분을 적재하는 "그 순간" 실시간으로 처리한다
/// (PotTrayBinder.onPotBound → ProductionStatsTracker.OnPotBound 이벤트 연결).
/// 따라서 이 스크립트는 순수하게 "삭제"만 담당하고, 통계 관련 코드는 없다.
///
/// [핵심 전제]
/// 화분 케이스(Pot Part 0~5, PotCase 레이어)와 화분/구근(Pot(10)(Clone) 등, Pot 레이어)은
/// 부모-자식 관계가 아니라 완전히 독립된 오브젝트일 수도, PotTrayBinder에 의해
/// 자식으로 편입됐을 수도 있다. 이 스크립트는 레이어별로 삭제 대상을 다르게 판단한다:
///   - PotCase 레이어에 닿으면 → caseLayerName 조상을 찾아 케이스 전체(Pot Part들) 삭제
///   - Pot 레이어(화분 자신)에 닿으면 → 그 화분 오브젝트 자신을 삭제
///
/// [Enter/Exit 깜빡임 대응]
/// 화분 케이스는 파츠 여러 개(콜라이더 여러 개)로 이루어져 있어, 트리거 경계에서
/// 개별 파츠가 Enter/Exit를 반복할 수 있다. "삭제 대상 단위로 겹친 콜라이더 개수
/// (overlapCount)"를 세어, 그게 0이 될 때만 예약을 취소하므로 안정적으로 삭제된다.
/// </summary>
[AddComponentMenu("Factory/Case Destroyer With Stats")]
public class CaseDestroyerWithStats : MonoBehaviour
{
    [Header("Destroy Settings")]
    [Tooltip("트리거에 닿은 후 지연 시간(초) 뒤에 삭제합니다.")]
    [SerializeField] private float destroyDelay;

    [Tooltip("삭제할 대상의 레이어 필터. 화분 케이스(PotCase)와 화분(Pot) 레이어를 " +
             "모두 체크해야, 어느 쪽이 트리거에 닿아도 각각 인식되어 삭제된다.")]
    public LayerMask targetLayer = ~0;

    [Header("화분 케이스 처리")]
    [Tooltip("이 레이어의 콜라이더가 닿으면, 이 레이어를 가진 조상까지 올라가서 " +
             "케이스 전체(파츠들)를 삭제 대상으로 삼는다.")]
    public string caseLayerName = "PotCase";

    [Header("화분(구근) 처리")]
    [Tooltip("이 레이어의 콜라이더가 닿으면, 조상을 찾지 않고 그 오브젝트 자신을 " +
             "삭제 대상으로 삼는다. 케이스와 부모-자식 관계가 아니므로 독립적으로 처리한다.")]
    public string potLayerName = "Pot";

    [Header("Debug")]
    [Tooltip("트리거 접촉, 레이어 판정, 삭제 시점을 전부 Console에 출력한다.")]
    public bool verboseLog = false;

    private class PendingInfo
    {
        public Coroutine coroutine;
        public int overlapCount;
    }

    private readonly Dictionary<GameObject, PendingInfo> pending = new Dictionary<GameObject, PendingInfo>();
    private int caseLayer = -1;
    private int potLayer = -1;

    private void Awake()
    {
        caseLayer = LayerMask.NameToLayer(caseLayerName);
        potLayer = LayerMask.NameToLayer(potLayerName);

        if (caseLayer < 0)
            Debug.LogWarning($"[{name}] '{caseLayerName}' 레이어를 찾을 수 없습니다. Case Layer Name 설정을 확인하세요.", this);
        if (potLayer < 0)
            Debug.LogWarning($"[{name}] '{potLayerName}' 레이어를 찾을 수 없습니다. Pot Layer Name 설정을 확인하세요.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject target = ResolveDestroyTarget(other);
        if (target == null) return;

        bool allowed = IsLayerAllowed(target);
        if (verboseLog)
        {
            Debug.Log(
                $"[{name}] Enter: 콜라이더={other.name}(Layer={LayerMask.LayerToName(other.gameObject.layer)}), " +
                $"삭제 대상={target.name}(Layer={LayerMask.LayerToName(target.layer)}), 통과={allowed}", this);
        }

        if (!allowed) return;

        if (!pending.TryGetValue(target, out PendingInfo info))
        {
            info = new PendingInfo { overlapCount = 0, coroutine = null };
            pending[target] = info;
        }

        info.overlapCount++;

        if (info.coroutine == null)
        {
            if (verboseLog)
                Debug.Log($"[{name}] 삭제 대기 시작: {target.name} (겹친 콜라이더 수={info.overlapCount}, {destroyDelay}초 후 삭제)", this);

            info.coroutine = StartCoroutine(DestroyAfterDelay(target));
        }
        else if (verboseLog)
        {
            Debug.Log($"[{name}] {target.name}에 콜라이더 추가 겹침 (현재 {info.overlapCount}개). 기존 예약 유지.", this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject target = ResolveDestroyTarget(other);
        if (target == null) return;

        if (!pending.TryGetValue(target, out PendingInfo info))
            return;

        info.overlapCount = Mathf.Max(0, info.overlapCount - 1);

        if (verboseLog)
            Debug.Log($"[{name}] Exit: {other.name} ({target.name} 기준 남은 겹침 수={info.overlapCount})", this);

        if (info.overlapCount <= 0)
        {
            if (info.coroutine != null)
                StopCoroutine(info.coroutine);

            pending.Remove(target);

            if (verboseLog)
                Debug.Log($"[{name}] {target.name} 완전히 트리거를 벗어남 → 삭제 예약 취소", this);
        }
    }

    private System.Collections.IEnumerator DestroyAfterDelay(GameObject target)
    {
        yield return new WaitForSeconds(destroyDelay);

        if (target != null)
        {
            if (verboseLog)
                Debug.Log($"[{name}] 삭제 실행: {target.name}", this);

            Destroy(target);
        }

        pending.Remove(target);
    }

    /// <summary>
    /// 닿은 콜라이더의 레이어에 따라 삭제 대상을 다르게 판단한다.
    /// - PotCase 레이어: 조상을 찾아 케이스 전체를 대상으로 삼는다 (파츠들이 모여 케이스를 이루므로).
    /// - Pot 레이어: 화분 자신을 대상으로 삼는다 (케이스와 독립된 개체이므로 조상을 찾지 않는다).
    /// - 그 외 레이어: 콜라이더가 속한 오브젝트 자신을 그대로 대상으로 삼는다.
    /// </summary>
    private GameObject ResolveDestroyTarget(Collider other)
    {
        GameObject start = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (start == null) return null;

        int startLayer = start.layer;

        if (potLayer >= 0 && startLayer == potLayer)
            return start;

        if (caseLayer >= 0)
        {
            Transform t = start.transform;
            while (t != null)
            {
                if (t.gameObject.layer == caseLayer)
                    return t.gameObject;

                t = t.parent;
            }
        }

        return start;
    }

    private bool IsLayerAllowed(GameObject obj)
    {
        return (targetLayer.value & (1 << obj.layer)) != 0;
    }
}