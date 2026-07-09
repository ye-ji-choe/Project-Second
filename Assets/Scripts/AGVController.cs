using UnityEngine;

public class AGVController : MonoBehaviour
{
    public AGVConnector connector;

    [Header("Movement Settings")]
    public float moveSpeed = 5.0f; // 속도를 올려서 확실히 보이게 세팅
    public float stopDistance = 0.05f;

    [Header("Stations (D00: 0~15)")]
    // 여기에 0번부터 15번까지의 목적지 Transform(빈 게임오브젝트)을 순서대로 넣어주세요.
    public Transform[] stations;

    private bool isMoving = false;
    private Transform targetStation;

    // Connector에서 기동 신호(Y225)를 받으면 실행됨
    public void Positioning(int destIndex)
    {
        if (stations == null || destIndex < 0 || destIndex >= stations.Length)
        {
            Debug.LogError($"[AGV] 이동 불가! D00({destIndex})에 해당하는 목적지가 Inspector에 없습니다.");
            return;
        }

        targetStation = stations[destIndex];
        isMoving = true;
        Debug.Log($"[AGV] {destIndex}번 목적지로 이동 시작...");
    }

    private void Update()
    {
        if (isMoving && targetStation != null)
        {
            // 목표를 향해 이동
            transform.position = Vector3.MoveTowards(transform.position, targetStation.position, moveSpeed * Time.deltaTime);

            // 도착 판정
            if (Vector3.Distance(transform.position, targetStation.position) <= stopDistance)
            {
                isMoving = false; // 정지
                Debug.Log("[AGV] 목적지 도착 완료!");

                // Connector에 도착했음을 알려 PLC에 신호 전송(BUSY OFF, 도착 ON)
                if (connector != null)
                {
                    connector.OnArrivalCompleted();
                }
            }
        }
    }
}