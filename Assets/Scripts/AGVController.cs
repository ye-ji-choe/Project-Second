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

    // ==========================================
    // PLC 상태 추적
    // ==========================================
    [Header("PLC Status")]
    [Tooltip("현재(또는 목적지) 스테이션 번호를 저장하여 커넥터로 전달합니다.")]
    public int currentStationId = 0;
    
    // 직전 스테이션 ID를 기억하여 경로 분기에 사용합니다.
    private int previousStationId = 0; 

    // ==========================================
    // 충돌 방지 및 경로 탐색 센서 설정
    // ==========================================
    [Header("Sensor & Collision Avoidance")]
    [Tooltip("감지할 앞차의 레이어를 선택하세요 (예: BATTERY)")]
    public LayerMask obstacleLayer;
    
    [Tooltip("전진 및 후진 시 레이저 센서 감지 거리")]
    public float frontSensorDistance = 2.0f; // 전진 시 거리
    public float rearSensorDistance = 1.0f;  // 후진 시 거리
    public float sensorHeightOffset = 0.5f;

    [Tooltip("전면 4개의 레이저 센서 가로 오프셋 간격")]
    public float[] laserSensorOffsets = new float[4] { -0.4f, -0.15f, 0.15f, 0.4f };

    [Tooltip("하단 1개의 마그네틱 센서 활성화 상태")]
    public bool isMagneticSensorActive = true;

    // ==========================================
    // 1공정: 판(Plate) 세팅
    // ==========================================
    [Header("1공정: Part(판) 세팅")]
    public Vector3 partLoadPosition = new Vector3(0f, 0f, 0.38f);
    public Vector3 partLoadRotation = new Vector3(0f, -90f, -90f);
    private bool isFirstPartLoaded = false;

    // ==========================================
    // 2공정: 배터리(Battery) 2개 세팅
    // ==========================================
    [Header("3공정: BATTERY(배터리) 세팅")]
    public Vector3[] batterySlotPositions = new Vector3[2];
    public Vector3[] batterySlotRotations = new Vector3[2];
    private int currentBatteryCount = 0;

    // ==========================================
    // 3공정: CCS 세팅
    // ==========================================
    [Header("7공정: CCS 세팅")]
    public Vector3 ccsLoadPosition = new Vector3(0f, 0f, 0.45f);
    public Vector3 ccsLoadRotation = new Vector3(0f, 0f, -180f);

    // ==========================================
    // 4공정: STR 2개 세팅 
    // ==========================================
    [Header("9공정: STR 세팅")]
    public Vector3[] strSlotPositions = new Vector3[2];
    public Vector3[] strSlotRotations = new Vector3[2];
    private int currentStrCount = 0;

    // ==========================================
    // 5공정: BMS 세팅 
    // ==========================================
    [Header("11공정: BMS 세팅")]
    public Vector3 bmsLoadPosition = new Vector3(0f, 0f, 0.6f);
    public Vector3 bmsLoadRotation = new Vector3(0f, 0f, 0f);

    // ==========================================
    // 마지막 공정: 12공정 Part 덮기 
    // ==========================================
    [Header("12공정: 마지막 Part 세팅")]
    public Vector3 finalPartLoadPosition = new Vector3(0f, 0f, 0.7f);
    public Vector3 finalPartLoadRotation = new Vector3(0f, -90f, -90f);

    [System.Serializable]
    public struct StationMapping
    {
        public int plcId;
        public Transform stationTransform;
    }

    [Header("Stations Mapping")]
    public StationMapping[] stationMappings;

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
        uprightTilt = Quaternion.Euler(transform.eulerAngles.x, 0, transform.eulerAngles.z);
    }

    public void Positioning(int plcCommand)
    {
        Debug.Log($"[AGV] 수신된 원본 PLC 명령: {plcCommand}");

        // 새로운 목적지로 가기 전, 현재 위치(목적지)를 이전 스테이션으로 백업
        previousStationId = currentStationId;

        // 현재 수신한 목적지 번호 저장
        currentStationId = plcCommand;

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

        Vector3 currentPos = transform.position;
        Vector3 forwardDir = -transform.right;
        Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
        Vector3 leftDir = -rightDir;

        if (plcCommand == 200)
        {
            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            Vector3 rightPoint = reversePoint + (rightDir * 3f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 7f);
            Vector3 leftPoint = forwardPoint + (leftDir * 3f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));
        }
        if (plcCommand == 400)
        {
            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            Vector3 rightPoint = reversePoint + (rightDir * 3f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 7f);
            Vector3 leftPoint = forwardPoint + (leftDir * 3f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));
        }
        else if (plcCommand == 300)
        {
            Vector3 reversePoint = currentPos - (forwardDir * 2f);
            Vector3 rightPoint = reversePoint + (rightDir * 2f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 4.5f);
            Vector3 leftPoint = forwardPoint + (leftDir * 2f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));
        }
        else if (plcCommand == 499)
        {
            Vector3 reversePoint = currentPos - (forwardDir * 2.5f);
            Vector3 rightPoint = reversePoint + (rightDir * 3.2f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
        }
        else if (plcCommand == 501 || plcCommand == 521)
        {
            Vector3 leftPoint = currentPos + (leftDir * 6.1f);
            currentPath.Add(new Waypoint(leftPoint, false));
        }
        else if (plcCommand == 511 || plcCommand == 531)
        {
            Vector3 leftPoint = currentPos + (leftDir * 2.8f);
            currentPath.Add(new Waypoint(leftPoint, false));
        }
        else if (plcCommand == 502)
        {
            if (previousStationId == 501 || previousStationId == 511)
            {
                // 501 -> 502로 갈 때의 경로 설정
                Vector3 reversePoint = currentPos - (forwardDir * 4.1f);

                currentPath.Add(new Waypoint(reversePoint, true));

                Debug.Log("[AGV] 501번에서 502번으로 이동 경로 생성");
            }
            else if (previousStationId == 521 || previousStationId == 531)
            {
                // 521 -> 502로 갈 때의 경로 설정
                Vector3 reversePoint = currentPos - (forwardDir * 2.3f);

                currentPath.Add(new Waypoint(reversePoint, true));

                Debug.Log("[AGV] 521번에서 502번으로 이동 경로 생성");
            }
        }

        else if (approachPaths.TryGetValue(plcCommand, out Waypoint[] waypoints))
        {
            foreach (Waypoint wp in waypoints)
            {
                currentPath.Add(wp);
            }
        }
        else if (plcCommand == 710)
        {
            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            currentPath.Add(new Waypoint(reversePoint, true));
        }
        else if (plcCommand == 800)
        {
            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            Vector3 rightPoint = reversePoint + (rightDir * 2f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 6f);
            Vector3 leftPoint = forwardPoint + (leftDir * 2f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));
        }
        else if (plcCommand == 900)
        {
            Vector3 reversePoint = currentPos - (forwardDir * 2f);
            Vector3 rightPoint = reversePoint + (rightDir * 2f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 5f);
            Vector3 leftPoint = forwardPoint + (leftDir * 2f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));
        }

        else if (plcCommand == 1000)
        {
            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            Vector3 rightPoint = reversePoint + (rightDir * 2.5f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 6f);
            Vector3 leftPoint = forwardPoint + (leftDir * 2.5f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));
        }
        else if (plcCommand == 1100)
        {
            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            Vector3 rightPoint = reversePoint + (rightDir * 2f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 6f);
            Vector3 leftPoint = forwardPoint + (leftDir * 2f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));
        }
        if (plcCommand == 1200)
        {
            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            Vector3 rightPoint = reversePoint + (rightDir * 3f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 6f);
            Vector3 leftPoint = forwardPoint + (leftDir * 3f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));
        }
        else if (plcCommand == 1300)
        {
            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            currentPath.Add(new Waypoint(reversePoint, true));
        }
        else if (plcCommand == 1400)
        {
            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            Vector3 rightPoint = reversePoint + (rightDir * 2f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 6f);
            Vector3 leftPoint = forwardPoint + (leftDir * 3f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));
        }

        currentPath.Add(new Waypoint(targetStation.position, false));
        currentWaypointIndex = 0;
        isMoving = true;
    }

    private void Update()
    {
        if (isMoving && currentPath.Count > 0)
        {
            Waypoint currentWaypoint = currentPath[currentWaypointIndex];

            // ==========================================
            // 4채널 레이저 양방향(전/후진) 감지 로직
            // ==========================================
            Vector3 detectDir = currentWaypoint.isReverse ? transform.right : -transform.right;

            // 👇 현재 방향(isReverse)에 따라 사용할 센서 거리를 결정합니다.
            float currentSensorDistance = currentWaypoint.isReverse ? rearSensorDistance : frontSensorDistance;

            Vector3 lateralDir = transform.forward; // AGV의 가로 방향
            bool isObstacleAhead = false;

            for (int i = 0; i < laserSensorOffsets.Length; i++)
            {
                Vector3 sensorStartPos = transform.position + (Vector3.up * sensorHeightOffset) + (lateralDir * laserSensorOffsets[i]);

                // 👇 기존 sensorDistance 대신 currentSensorDistance를 적용합니다.
                if (Physics.Raycast(sensorStartPos, detectDir, out RaycastHit hit, currentSensorDistance, obstacleLayer))
                {
                    if (!hit.transform.IsChildOf(this.transform))
                    {
                        isObstacleAhead = true;
                        // Debug.DrawRay(sensorStartPos, detectDir * currentSensorDistance, Color.red);
                        break;
                    }
                }
            }

            if (isObstacleAhead)
            {
                return;
            }
            // ==========================================

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

            // 목적지 도달 확인
            if (Vector3.Distance(transform.position, targetPos) <= stopDistance)
            {
                currentWaypointIndex++;

                // [마그네틱 센서 연동 및 PLC 데이터 전송 준비]
                if (isMagneticSensorActive && currentWaypointIndex >= currentPath.Count)
                {
                    isMoving = false;
                    Debug.Log($"[AGV] {currentStationId}번 노드 마그네틱 센서 인식 완료 - 최종 목적지 도착!");
                    if (connector != null) connector.OnArrivalCompleted();
                }
                else if (currentWaypointIndex >= currentPath.Count)
                {
                    isMoving = false;
                    Debug.Log($"[AGV] {currentStationId}번 노드 도착 완료!");
                    if (connector != null) connector.OnArrivalCompleted();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessItem(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessItem(collision.gameObject);
    }

    // ==========================================
    // 각 공정별 적재 처리 로직
    // ==========================================
    private void ProcessItem(GameObject item)
    {
        if (item.CompareTag("Part"))
        {
            if (!isFirstPartLoaded)
            {
                item.transform.SetParent(this.transform);
                item.transform.localPosition = partLoadPosition;
                item.transform.localRotation = Quaternion.Euler(partLoadRotation);

                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                item.tag = "Untagged";
                isFirstPartLoaded = true;

                Debug.Log($"[AGV] 1공정 판({item.name}) 탑재 완료!");
            }
            else
            {
                item.transform.SetParent(this.transform);
                item.transform.localPosition = finalPartLoadPosition;
                item.transform.localRotation = Quaternion.Euler(finalPartLoadRotation);

                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                item.tag = "Untagged";
                Debug.Log($"[AGV] 12공정 Part({item.name}) 탑재 완료! 덮개 조립 끝!");
            }
        }
        else if (item.CompareTag("BATTERY"))
        {
            if (currentBatteryCount < batterySlotPositions.Length)
            {
                item.transform.SetParent(this.transform);
                item.transform.localPosition = batterySlotPositions[currentBatteryCount];
                item.transform.localRotation = Quaternion.Euler(batterySlotRotations[currentBatteryCount]);

                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                item.tag = "Untagged";
                currentBatteryCount++;
                Debug.Log($"[AGV] 2공정 배터리 {currentBatteryCount}번째 탑재 완료!");
            }
            else
            {
                Debug.LogWarning("[AGV] 배터리 슬롯 2개가 이미 꽉 찼습니다!");
            }
        }
        else if (item.CompareTag("CCS"))
        {
            item.transform.SetParent(this.transform);
            item.transform.localPosition = ccsLoadPosition;
            item.transform.localRotation = Quaternion.Euler(ccsLoadRotation);

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            item.tag = "Untagged";
            Debug.Log($"[AGV] 7공정 CCS({item.name}) 탑재 완료!");
        }
        else if (item.CompareTag("STR"))
        {
            if (currentStrCount < strSlotPositions.Length)
            {
                item.transform.SetParent(this.transform);
                item.transform.localPosition = strSlotPositions[currentStrCount];
                item.transform.localRotation = Quaternion.Euler(strSlotRotations[currentStrCount]);

                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                item.tag = "Untagged";
                currentStrCount++;
                Debug.Log($"[AGV] 9공정 STR {currentStrCount}번째 탑재 완료!");
            }
            else
            {
                Debug.LogWarning("[AGV] STR 슬롯 2개가 이미 꽉 찼습니다!");
            }
        }
        else if (item.CompareTag("BMS"))
        {
            item.transform.SetParent(this.transform);
            item.transform.localPosition = bmsLoadPosition;
            item.transform.localRotation = Quaternion.Euler(bmsLoadRotation);

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            item.tag = "Untagged";
            Debug.Log($"[AGV] 11공정 BMS({item.name}) 탑재 완료!");
        }
    }
}