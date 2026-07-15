using System.Collections.Generic;
using UnityEngine;

public class AGVController : MonoBehaviour
{
    public AGVConnector connector;

    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float stopDistance = 0.05f;
    public float rotationSpeed = 120.0f;
    public float rotationOffset = 90.0f;

    [Header("Stations (D00: 0~15)")]
    public Transform[] stations;

    [System.Serializable]
    public struct Waypoint
    {
        public Vector3 position;
        public bool isReverse;

        public Waypoint(Vector3 pos, bool reverse = false)
        {
            position = pos;
            isReverse = reverse;
        }
    }

    private Dictionary<int, Waypoint[]> approachPaths = new Dictionary<int, Waypoint[]>();
    private bool isMoving = false;
    private int currentWaypointIndex = 0;
    private List<Waypoint> currentPath = new List<Waypoint>();

    private Quaternion uprightTilt;

    private void Awake()
    {
        // [수정] 2번 경로는 Positioning에서 동적으로 생성하므로 Awake에서는 삭제하여 꼬임을 방지합니다.
        // approachPaths[2] = new Waypoint[] { }; 

        // 씬 시작 시, 모델이 바닥에 똑바로 서 있는 상태의 X, Z 기울기 저장
        uprightTilt = Quaternion.Euler(transform.eulerAngles.x, 0, transform.eulerAngles.z);
    }

    public void Positioning(int plcCommand)
    {
        // [중요 확인] PLC에서 들어오는 원본 값과 계산된 인덱스를 로그로 확인합니다.
        int targetIndex = plcCommand / 100;
        Debug.Log($"[AGV] 수신된 PLC 명령: {plcCommand} -> 계산된 타겟 인덱스: {targetIndex}");

        if (stations == null || targetIndex < 0 || targetIndex >= stations.Length)
        {
            Debug.LogError($"[AGV] 알 수 없는 PLC 명령 또는 인덱스 범위 초과 (인덱스: {targetIndex})");
            return;
        }

        currentPath.Clear();

        // ==========================================
        // 2번 설비 전용 동적 경로 생성 로직
        // ==========================================
        if (targetIndex == 2)
        {
            Debug.Log("[AGV] 2번 설비 이동 시퀀스 작동 (동적 경로 생성)");

            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right; // AGV의 정면 (-X축)
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            Vector3 rightPoint = reversePoint + (rightDir * 2.2f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 7f);
            Vector3 leftPoint = forwardPoint + (leftDir * 2.2f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));
        }
        // 2번이 아닌 다른 목적지일 경우, 딕셔너리에 정의된 사전 경로가 있다면 가져옵니다.
        else if (approachPaths.TryGetValue(targetIndex, out Waypoint[] waypoints))
        {
            Debug.Log($"[AGV] {targetIndex}번 설비 사전 정의된 접근 경로 적용");
            foreach (Waypoint wp in waypoints)
            {
                currentPath.Add(wp);
            }
        }
        else
        {
            Debug.Log($"[AGV] {targetIndex}번 설비는 경유지 없이 최종 목적지로 직행합니다.");
        }

        // 최종 목적지 추가
        currentPath.Add(new Waypoint(stations[targetIndex].position, false));

        currentWaypointIndex = 0;
        isMoving = true;
    }

    private void Update()
    {
        if (isMoving && currentPath.Count > 0)
        {
            Waypoint currentWaypoint = currentPath[currentWaypointIndex];
            Vector3 currentTargetPos = currentWaypoint.position;
            Vector3 targetPos = new Vector3(currentTargetPos.x, transform.position.y, currentTargetPos.z);

            Vector3 dirToTarget = (targetPos - transform.position).normalized;

            if (dirToTarget != Vector3.zero)
            {
                if (currentWaypoint.isReverse)
                {
                    // 후진: 회전 없이 좌표만 이동
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                }
                else
                {
                    // 전진: 조향 후 직진
                    float targetAngleY = Mathf.Atan2(dirToTarget.x, dirToTarget.z) * Mathf.Rad2Deg;
                    targetAngleY += rotationOffset;

                    Quaternion targetRotation = Quaternion.Euler(0, targetAngleY, 0) * uprightTilt;
                    float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);

                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                    if (angleDiff <= 5.0f)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                    }
                }
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            }

            // 도착 판정
            if (Vector3.Distance(transform.position, targetPos) <= stopDistance)
            {
                currentWaypointIndex++;

                if (currentWaypointIndex >= currentPath.Count)
                {
                    isMoving = false;
                    Debug.Log("[AGV] 최종 목적지 도착 완료!");

                    if (connector != null)
                    {
                        connector.OnArrivalCompleted();
                    }
                }
            }
        }
    }
}