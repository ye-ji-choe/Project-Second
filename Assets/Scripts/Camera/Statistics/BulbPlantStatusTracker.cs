using UnityEngine;

/// <summary>
/// 화분(Pot) 하나에 붙어서, 그 화분에 구근(Bulb 자식 오브젝트)이 심어졌는지 상태를 관리한다.
/// (원본 PotBulbHolder를 기반으로, 이름 충돌 방지 + 통계 조회용 API를 보강한 버전.)
///
/// [핵심 동작 원리 - 중요]
/// 구근은 화분 콜라이더 안으로 물리적으로 들어오는 별도 오브젝트가 아니라,
/// 이 화분의 자식으로 이미 존재하는 "Bulb"라는 오브젝트가 SetActive(true/false)로
/// 켜지고 꺼지는 것으로 표현된다. 즉 구근 감지는 Physics.OverlapSphere 같은 물리 스캔이 아니라
/// 이 컴포넌트의 IsPlanted 값을 직접 읽는 것으로 판정해야 한다.
/// (이전에 만든 BulbDetectionSensor는 물리 스캔 방식이라 이 구조와 맞지 않았다.)
///
/// [배치]
///   Pot (1) 프리팹, 그리고 그로부터 생성되는 모든 화분 인스턴스에 이미 붙어 있다.
///   자식 중 이름이 정확히 "Bulb"인 오브젝트를 자동으로 찾는다.
///
/// [사용처]
///   PotTrayBinder가 화분을 슬롯에 바인딩하는 순간, 이 컴포넌트의 IsPlanted를 읽어서
///   구근 감지 여부(정상/불량)를 판정하고 ProductionStatsTracker에 실시간으로 반영한다.
/// </summary>
[DisallowMultipleComponent]
public class BulbPlantStatusTracker : MonoBehaviour
{
    [Header("Bulb")]
    [SerializeField] private GameObject bulbObject;
    [SerializeField] private bool hideOnStart = true;

    [Header("Status")]
    [SerializeField] private bool isPlanted;

    [Header("Debug")]
    [Tooltip("구근 상태가 바뀔 때, 그리고 외부에서 IsPlanted를 조회할 때 Console에 로그를 남긴다.")]
    public bool verboseLog = false;

    /// <summary>지금 이 화분에 구근이 심어져 있는지 여부. ProductionStatsTracker가 이 값을 읽는다.</summary>
    public bool IsPlanted => isPlanted;

    /// <summary>Bulb 오브젝트를 찾았는지 여부. false라면 애초에 판정 자체가 불가능한 상태다.</summary>
    public bool HasBulbReference => bulbObject != null;

    private void Reset()
    {
        FindBulbObject();
    }

    private void Awake()
    {
        FindBulbObject();

        if (bulbObject == null)
        {
            Debug.LogError(
                $"[{name}] Bulb Object를 찾지 못했습니다. 자식 중 이름이 정확히 'Bulb'인 " +
                $"오브젝트가 있는지 확인하세요. 이 상태에서는 구근 감지가 항상 실패로 처리될 수 있습니다.", this);
            return;
        }

        DisableBulbPhysics();

        if (hideOnStart && !isPlanted)
            bulbObject.SetActive(false);
    }

    public bool CanPlant()
    {
        return bulbObject != null && !isPlanted;
    }

    public void ShowBulb()
    {
        if (!CanPlant())
        {
            if (verboseLog)
                Debug.LogWarning($"[{name}] ShowBulb 호출됐지만 CanPlant()가 false입니다 (bulbObject={bulbObject != null}, isPlanted={isPlanted}).", this);
            return;
        }

        isPlanted = true;
        bulbObject.SetActive(true);

        if (verboseLog)
            Debug.Log($"[{name}] 구근이 심어졌습니다. IsPlanted=true", this);
    }

    [ContextMenu("Reset Bulb")]
    public void ResetBulb()
    {
        FindBulbObject();
        isPlanted = false;

        if (bulbObject != null)
            bulbObject.SetActive(false);

        if (verboseLog)
            Debug.Log($"[{name}] 구근 상태 리셋. IsPlanted=false", this);
    }

    /// <summary>
    /// 지금 이 순간 구근 심김 여부를 즉시 재확인해서 반환한다.
    /// PotTrayBinder가 화분 적재 순간 최신 상태를 확실히 얻기 위해 호출한다.
    /// Bulb 오브젝트 참조가 끊겼을 가능성까지 대비해 매번 activeSelf를 다시 확인한다.
    /// </summary>
    public bool CheckNow()
    {
        if (bulbObject == null)
        {
            FindBulbObject();
        }

        // isPlanted 플래그뿐 아니라, Bulb 오브젝트가 실제로 활성 상태인지도 함께 확인한다.
        // (혹시 외부에서 SetActive를 직접 건드려 isPlanted 플래그와 실제 상태가 어긋난 경우를 대비.)
        bool actuallyActive = bulbObject != null && bulbObject.activeSelf;

        if (actuallyActive != isPlanted)
        {
            if (verboseLog)
                Debug.LogWarning(
                    $"[{name}] IsPlanted 플래그({isPlanted})와 Bulb 오브젝트의 실제 활성 상태({actuallyActive})가 다릅니다. " +
                    $"실제 상태를 기준으로 보정합니다.", this);

            isPlanted = actuallyActive;
        }

        if (verboseLog)
            Debug.Log($"[{name}] CheckNow 호출: IsPlanted={isPlanted} (Bulb 오브젝트 존재={bulbObject != null})", this);

        return isPlanted;
    }

    private void FindBulbObject()
    {
        if (bulbObject != null)
            return;

        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == transform || children[i].name != "Bulb")
                continue;

            bulbObject = children[i].gameObject;
            return;
        }
    }

    private void DisableBulbPhysics()
    {
        Rigidbody[] bodies = bulbObject.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].useGravity = false;
            bodies[i].isKinematic = true;
        }

        Collider[] colliders = bulbObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }
}