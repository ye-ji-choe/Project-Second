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
    // 1공정: 판(Plate) 세팅
    // ==========================================
    [Header("1공정: Part(판) 세팅")]
    public Vector3 partLoadPosition = new Vector3(0f, 0f, 0.38f);
    public Vector3 partLoadRotation = new Vector3(0f, -90f, -90f);

    // 💡 1공정 판과 12공정 판을 구분하기 위한 스위치
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
    // 마지막 공정: 12공정 Part 덮기 (새로 추가됨!)
    // ==========================================
    [Header("12공정: 마지막 Part 세팅")]
    public Vector3 finalPartLoadPosition = new Vector3(0f, 0f, 0.7f); // 맨 위에 덮히도록 높이 조절
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

        if (plcCommand == 200)
        {
            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

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
            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

            Vector3 reversePoint = currentPos - (forwardDir * 2f);
            Vector3 rightPoint = reversePoint + (rightDir * 2f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 5f);
            Vector3 leftPoint = forwardPoint + (leftDir * 2f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));
        }
        else if (plcCommand == 400)
        {
            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            Vector3 rightPoint = reversePoint + (rightDir * 3f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 7f);
            Vector3 leftPoint = forwardPoint + (leftDir * 3f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));
        }
        else if (plcCommand == 499)
        {

            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

            Vector3 reversePoint = currentPos - (forwardDir * 2.5f);
            Vector3 rightPoint = reversePoint + (rightDir * 2.5f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));

        }
        else if (plcCommand == 500 || plcCommand == 510 || plcCommand == 520 || plcCommand == 530)
        {

            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

            Vector3 rightPoint = currentPos + (rightDir * 1f);

            currentPath.Add(new Waypoint(rightPoint, false));

        }
        else if (plcCommand == 501 || plcCommand == 511 || plcCommand == 521 || plcCommand == 531)
        {

            float offsetDistance = 2.5f;

            Vector3 forwardDir = -transform.right;
            Vector3 preApproachPoint = targetStation.position + (forwardDir * offsetDistance);

            currentPath.Add(new Waypoint(preApproachPoint, false));
        }
        else if (plcCommand == 502 || plcCommand == 512 || plcCommand == 522 || plcCommand == 532)
        {
            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;
            Vector3 reversePoint = currentPos - (forwardDir * 2.5f);

            currentPath.Add(new Waypoint(reversePoint, true));
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

            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

            Vector3 reversePoint = currentPos - (forwardDir * 3f);

            currentPath.Add(new Waypoint(reversePoint, true));

        }
        else if (plcCommand == 800)
        {

            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

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

            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

            Vector3 reversePoint = currentPos - (forwardDir * 2f);
            Vector3 rightPoint = reversePoint + (rightDir * 2f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 6f);
            Vector3 leftPoint = forwardPoint + (leftDir * 2f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));

        }
        else if (plcCommand == 1000)
        {

            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

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

            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            Vector3 rightPoint = reversePoint + (rightDir * 2f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 6f);
            Vector3 leftPoint = forwardPoint + (leftDir * 2f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));

        }
        else if (plcCommand == 1200)
        {

            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            Vector3 rightPoint = reversePoint + (rightDir * 3f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 6f);
            Vector3 leftPoint = forwardPoint + (leftDir * 3f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));

        }
        else if (plcCommand == 1400)
        {

            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

            Vector3 reversePoint = currentPos - (forwardDir * 3f);
            Vector3 rightPoint = reversePoint + (rightDir * 2f);
            Vector3 forwardPoint = rightPoint + (forwardDir * 5f);
            Vector3 leftPoint = forwardPoint + (leftDir * 3f);

            currentPath.Add(new Waypoint(reversePoint, true));
            currentPath.Add(new Waypoint(rightPoint, false));
            currentPath.Add(new Waypoint(forwardPoint, false));
            currentPath.Add(new Waypoint(leftPoint, false));

        }
        else if (plcCommand == 1300)
        {

            Vector3 currentPos = transform.position;
            Vector3 forwardDir = -transform.right;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 leftDir = -rightDir;

            Vector3 reversePoint = currentPos - (forwardDir * 3f);

            currentPath.Add(new Waypoint(reversePoint, true));

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
    // 각 공정별 적재 처리 로직 (1~12공정 완벽 통합)
    // ==========================================
    private void ProcessItem(GameObject item)
    {
        // 💡 1공정 & 12공정 처리: 동일한 "Part" 태그를 사용할 때 스위치(isFirstPartLoaded)로 구분!
        if (item.CompareTag("Part"))
        {
            if (!isFirstPartLoaded)
            {
                // [1공정 처리]
                item.transform.SetParent(this.transform);
                item.transform.localPosition = partLoadPosition;
                item.transform.localRotation = Quaternion.Euler(partLoadRotation);

                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                item.tag = "Untagged"; // 적재 완료된 부품은 태그 해제
                isFirstPartLoaded = true; // 1공정 완료 스위치 ON

                Debug.Log($"[AGV] 1공정 판({item.name}) 탑재 완료!");
            }
            else
            {
                // [12공정 처리] 이미 1공정 판이 들어온 적 있다면 마지막 판으로 간주!
                item.transform.SetParent(this.transform);
                item.transform.localPosition = finalPartLoadPosition;
                item.transform.localRotation = Quaternion.Euler(finalPartLoadRotation);

                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                item.tag = "Untagged";
                Debug.Log($"[AGV] 12공정 Part({item.name}) 탑재 완료! 덮개 조립 끝!");
            }
        }

        // 3공정: 배터리 (Battery)
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

        // 7공정: CCS
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

        // 9공정: STR
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

        // 11공정: BMS
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