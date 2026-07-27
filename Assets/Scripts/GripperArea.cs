using System.Collections.Generic;
using UnityEngine;

public class GripperArea : MonoBehaviour
{
    public List<Collider> triggerList = new List<Collider>();
    public float multiplier = 1f;

    private void OnTriggerEnter(Collider other)
    {
        // 1. 닿은 것이 부품(Part)이면 잡는다
        if (other.CompareTag("Part"))
        {
            if (!triggerList.Contains(other))
            {
                triggerList.Add(other);
                Grab(); // 집기 명령!
            }
        }

        // 2. 닿은 것이 AGV면 들고 있던 물건을 놓는다
        if (other.CompareTag("AGV"))
        {
            Drop(); // 놓기 명령!
        }

    }

    public void Drop()
    {
        // 현재 그리퍼의 자식으로 있는 물체가 있다면
        if (transform.childCount > 0)
        {
            // 자식 객체를 가져옵니다 (보통 한 번에 하나씩 잡으므로 index 0)
            Transform targetObj = transform.GetChild(0);

            // 1. 부모-자식 관계 해제 (이게 핵심!)
            targetObj.SetParent(null);

            // 2. 물리 연산 다시 활성화 (중력 등)
            Rigidbody rb = targetObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            Debug.Log($"[Gripper] {targetObj.name}을(를) 놓았습니다!");
        }
        else
        {
            Debug.Log("[Gripper] 놓을 물건이 없습니다.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        triggerList.Remove(other);
    }

    public Vector3 currentVelocity;
    private Vector3 lastPosition;

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        currentVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;
    }

    // ==========================================
    // [추가] 실제로 물체를 잡는 함수
    // ==========================================
    public void Grab()
    {
        // 영역 안에 물체가 하나라도 있는지 확인
        if (triggerList.Count > 0)
        {
            // 리스트에 있는 첫 번째 물체를 대상으로 지정
            Transform targetPlate = triggerList[0].transform;

            // 판을 그리퍼(현재 스크립트가 있는 오브젝트)의 자식으로 설정
            targetPlate.SetParent(this.transform);

            // 이동 시 물리 충돌로 인해 떨어지는 것을 방지
            Rigidbody rb = targetPlate.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            Debug.Log($"{targetPlate.name}을(를) 잡았습니다!");
        }
    }

    // ==========================================
    // [추가] 물체를 놓는 함수
    // ==========================================
    public void Release()
    {
        if (transform.childCount > 0)
        {
            // 자식으로 있던 판을 찾아서 부모 관계 해제
            Transform attachedPlate = transform.GetChild(0);
            attachedPlate.SetParent(null);

            // 다시 물리 연산 켜기 (바닥에 떨어지거나 중력 적용을 위해)
            Rigidbody rb = attachedPlate.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            Debug.Log("물체를 놓았습니다!");
        }
    }
}