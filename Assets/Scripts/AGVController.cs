using UnityEngine;

public class AGVController : MonoBehaviour
{
    public AGVConnector connector; // 앞서 만든 Connector 스크립트 연결

    [Header("AGV Movement Settings")]
    public float moveSpeed = 3.0f;     // AGV 이동 속도
    public float stopDistance = 0.1f;  // 목적지 도착으로 판정할 거리 오차

    [Header("Station Destinations (D00: 0~15)")]
    // PLC의 D00 신호(0~15)에 대응하는 유니티 상의 실제 설비 위치(Transform) 배열
    // Inspector 창에서 목적지 위치들을 순서대로 넣어주세요.
    public Transform[] stations;

    private bool isMoving = false;
    private Transform currentTarget;

    private void Start()
    {
        // 초기 상태: 정지 (BUSY 신호는 Connector에서 기본적으로 꺼져있음)
        isMoving = false;
    }

    private void Update()
    {
        // AGV 이동 로직 (isMoving이 true일 때만 작동)
        if (isMoving && currentTarget != null)
        {
            // 지정된 목적지를 향해 등속 이동
            transform.position = Vector3.MoveTowards(transform.position, currentTarget.position, moveSpeed * Time.deltaTime);

            // 목적지와 내 위치 사이의 거리를 계산하여 도착 판정
            if (Vector3.Distance(transform.position, currentTarget.position) <= stopDistance)
            {
                ArriveAtDestination();
            }
        }
    }

    /// <summary>
    /// AGVConnector에서 기동 신호(Y225)를 받았을 때 호출되는 함수
    /// </summary>
    /// <param name="destinationIndex">D00에서 읽어온 목적지 설비 번호</param>
    public void MoveToDestination(int destinationIndex)
    {
        // [오류 방지 로직] 배열이 비어있거나, D00 값이 배열 범위를 벗어나면 이동 취소
        if (stations == null || stations.Length == 0)
        {
            Debug.LogError("[AGVController] 에러: stations 배열이 비어있습니다. Inspector에서 목적지를 할당해주세요.");
            return;
        }

        if (destinationIndex < 0 || destinationIndex >= stations.Length)
        {
            Debug.LogWarning($"[AGVController] 경고: 유효하지 않은 목적지 번호({destinationIndex})가 입력되었습니다. 이동을 무시합니다.");
            return;
        }

        // 유효한 목적지 설정 및 이동 시작
        currentTarget = stations[destinationIndex];
        isMoving = true;

        // 출발했으므로 Home Position이 아님을 PLC에 업데이트 (필요시)
        if (connector != null)
        {
            connector.UpdateHomePosition(false);
        }

        Debug.Log($"[AGVController] 목적지 {destinationIndex}번으로 이동 시작. (Connector에서 BUSY 신호 ON 됨)");
    }

    /// <summary>
    /// AGV가 목적지 좌표에 도달했을 때 호출되는 함수
    /// </summary>
    private void ArriveAtDestination()
    {
        isMoving = false; // AGV 이동 정지
        Debug.Log("[AGVController] 목적지 도착 완료. (BUSY OFF, 도착 완료 신호 ON 요청)");

        // Connector를 통해 PLC에 피드백 전달 
        // -> AGVConnector의 OnArrivalCompleted() 내부에서 X207(도착) ON, X200(BUSY) OFF 처리됨
        if (connector != null)
        {
            connector.OnArrivalCompleted();
        }
    }
}