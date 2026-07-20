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

    // [수정 1] PLC ID와 목적지 Transform을 연결하는 구조체 선언
    [System.Serializable]
    public struct StationMapping
    {
        public int plcId;               // 예: 100, 200, 601, 610 등
        public Transform stationTransform; // 해당 ID의 목적지 좌표
    }

    [Header("Stations Mapping")]
    public StationMapping[] stationMappings; // 기존 Transform[] stations 대체

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
        // 씬 시작 시, 모델이 바닥에 똑바로 서 있는 상태의 X, Z 기울기 저장
        uprightTilt = Quaternion.Euler(transform.eulerAngles.x, 0, transform.eulerAngles.z);
    }

    public void Positioning(int plcCommand)
    {
        Debug.Log($"[AGV] 수신된 원본 PLC 명령: {plcCommand}");

        // [수정 2] 배열 인덱스 대신, plcCommand와 일치하는 plcId를 가진 매핑 데이터를 검색
        Transform targetStation = null;
        foreach (var mapping in stationMappings)
        {
            if (mapping.plcId == plcCommand)
            {
                targetStation = mapping.stationTransform;
                break;
            }
        }

        if (targetStation == null)
        {
            Debug.LogError($"[AGV] 알 수 없는 PLC 명령입니다. 매핑되지 않은 ID: {plcCommand}");
            return;
        }

        currentPath.Clear();

        // ==========================================
        // [수정 3] 기존 인덱스 2번 기준이었던 동적 생성 로직을 PLC ID '200' 기준으로 변경
        // ==========================================
        if (plcCommand == 200)
        {
            Debug.Log("[AGV] 200번 설비 이동 시퀀스 작동 (동적 경로 생성)");

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
        // 동적 경로가 아닌 경우, 딕셔너리에 정의된 사전 경로 검사 (Key값도 plcCommand 기준)
        else if (approachPaths.TryGetValue(plcCommand, out Waypoint[] waypoints))
        {
            Debug.Log($"[AGV] {plcCommand}번 설비 사전 정의된 접근 경로 적용");
            foreach (Waypoint wp in waypoints)
            {
                currentPath.Add(wp);
            }
        }
        else
        {
            Debug.Log($"[AGV] {plcCommand}번 설비는 경유지 없이 최종 목적지로 직행합니다.");
        }

        // 최종 목적지 추가
        currentPath.Add(new Waypoint(targetStation.position, false));

        currentWaypointIndex = 0;
        isMoving = true;
    }

    private void Update()
    {
        // Update 문 내부의 이동 및 회전 로직은 변경할 필요가 없습니다.
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
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                }
                else
                {
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