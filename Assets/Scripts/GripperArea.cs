using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GripperArea : MonoBehaviour
{
    public List<Collider> triggerList = new List<Collider>();
    public float multiplier = 1f;
    public Vector3 currentVelocity;
    private Vector3 lastPosition;

    // ==========================================
    // [추가된 설정] 물체를 잡을 때 위치/회전 강제 정렬 기능
    // ==========================================
    [Header("Grab Settings")]
    [Tooltip("체크하면 아래의 Position과 Rotation 값으로 부품의 각도를 강제로 맞춥니다.")]
    public bool useSnap = false;                     // 기본값: 꺼짐 (체크 시 켜짐)
    public Vector3 grabPosition = Vector3.zero;      // 부품이 잡힐 위치 (그리퍼 중심 기준)
    public Vector3 grabRotation = Vector3.zero;      // 부품이 잡힐 회전 각도

    // 방금 물건을 놓았는지 확인하는 플래그 (재포착 방지용)
    private bool isJustDropped = false;

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        currentVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 닿은 것이 부품(Part)이고, 방금 막 놓은 상태가 아닐 때만 잡는다.
        if (other.CompareTag("Part") && !isJustDropped)
        {
            if (!triggerList.Contains(other))
            {
                triggerList.Add(other);
                Grab(); // 집기 명령
            }
        }

        // 2. 닿은 것이 AGV면 들고 있던 물건을 놓는다.
        if (other.CompareTag("AGV"))
        {
            StartCoroutine(DropRoutine());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (triggerList.Contains(other))
        {
            triggerList.Remove(other);
        }
    }

    // ==========================================
    // 물건을 놓고 잠시 대기하는 코루틴 (다시 줍기 방지)
    // ==========================================
    private IEnumerator DropRoutine()
    {
        // 상태를 '방금 놓음'으로 변경 (집기 일시 정지)
        isJustDropped = true;

        // 실제 놓기 동작 수행
        Drop();

        // 대기 시간: 로봇 팔이 빠져나올 때까지 2초 대기
        yield return new WaitForSeconds(2.0f);

        // 상태 원상복구 (이제 다시 새로운 부품을 집을 수 있음)
        isJustDropped = false;
    }

    // ==========================================
    // 물체를 잡는 함수 
    // ==========================================
    public void Grab()
    {
        if (triggerList.Count > 0)
        {
            Collider targetCollider = triggerList[0];
            GameObject targetObj = targetCollider.gameObject;

            // 1. 물체를 그리퍼의 자식으로 설정
            targetObj.transform.SetParent(this.transform);

            // ==========================================
            // 2. [핵심] useSnap 스위치가 켜져(true) 있을 때만 위치/각도를 강제 조정!
            // ==========================================
            if (useSnap)
            {
                targetObj.transform.localPosition = grabPosition;
                targetObj.transform.localRotation = Quaternion.Euler(grabRotation);
            }

            // 3. 물리 연산 끄기 (이동 중 떨림/튕김 방지)
            Rigidbody rb = targetObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            Debug.Log($"[Gripper] {targetObj.name} 잡기 완료! (강제 정렬 켜짐: {useSnap})");
        }
    }

    // ==========================================
    // 물체를 놓는 함수
    // ==========================================
    public void Drop()
    {
        if (transform.childCount > 0)
        {
            Transform targetObj = transform.GetChild(0);

            // 1. 부모-자식 관계 해제
            targetObj.SetParent(null);

            // 2. 강제로 수평 맞추기 (X, Z축 기울기 0)
            targetObj.rotation = Quaternion.Euler(0, targetObj.eulerAngles.y, 0);

            // 3. 파묻힘 방지를 위해 강제로 위로 살짝 띄움
            targetObj.position = new Vector3(targetObj.position.x, targetObj.position.y + 0.05f, targetObj.position.z);

            // 4. 물리 연산 다시 활성화
            Rigidbody rb = targetObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            Debug.Log($"[Gripper] {targetObj.name}을(를) 놓았습니다!");
        }
    }
}