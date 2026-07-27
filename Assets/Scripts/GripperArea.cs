using System.Collections; // 코루틴을 사용하기 위해 반드시 필요합니다.
using System.Collections.Generic;
using UnityEngine;

public class GripperArea : MonoBehaviour
{
    public List<Collider> triggerList = new List<Collider>();
    public float multiplier = 1f;
    public Vector3 currentVelocity;
    private Vector3 lastPosition;

    // [추가] 방금 물건을 놓았는지 확인하는 깃발(플래그) 역할
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
                Grab(); // 집기 명령!
            }
        }

        // 2. 닿은 것이 AGV면 들고 있던 물건을 놓는다. (코루틴으로 대기 시간 부여)
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
    // [추가] 물건을 놓고 잠시 대기하는 코루틴 함수
    // ==========================================
    private IEnumerator DropRoutine()
    {
        // 1. 상태를 '방금 놓음'으로 변경 (집기 기능 일시정지)
        isJustDropped = true;

        // 2. 실제 놓기 동작 수행
        Drop();

        // 3. 0.5초 동안 대기 (로봇 팔이 위로 올라가거나 AGV가 이동할 시간을 벌어줌)
        // (필요하다면 0.5f 숫자를 늘리거나 줄이셔도 됩니다)
        yield return new WaitForSeconds(0.5f);

        // 4. 상태를 원상복구 (이제 다시 물건을 집을 수 있음)
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

            // 2. 이동 중 물리 충돌로 인한 떨림 방지
            Rigidbody rb = targetObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            Debug.Log($"[Gripper] {targetObj.name}을(를) 잡았습니다!");
        }
    }

    // ==========================================
    // 물체를 놓는 함수
    // ==========================================
    public void Drop()
    {
        // 현재 그리퍼의 자식으로 있는 물체가 있다면
        if (transform.childCount > 0)
        {
            Transform targetObj = transform.GetChild(0);

            // 1. 부모-자식 관계 해제
            targetObj.SetParent(null);

            // 2. 물리 연산 다시 활성화 (중력 등 적용)
            Rigidbody rb = targetObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            Debug.Log($"[Gripper] {targetObj.name}을(를) 놓았습니다!");
        }
    }
}