using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GripperArea : MonoBehaviour
{
    [Header("Grab Settings")]
    public bool useSnap = false;
    public Vector3 grabPosition = Vector3.zero;
    public Vector3 grabRotation = Vector3.zero;

    public Vector3 currentVelocity;
    private Vector3 lastPosition;

    [Header("디버그 (건드리지 마세요)")]
    public List<Collider> triggerList = new List<Collider>();
    public GameObject currentGrabbedObject = null;
    private bool isJustDropped = false;

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        currentVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;
        // 쿨타임이 끝났고 센서에 물체가 있으면 다시 줍기
        if (currentGrabbedObject == null && !isJustDropped && triggerList.Count > 0)
        {
            Grab();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 태그 확인
        if (other.CompareTag("Part") || other.CompareTag("BATTERY"))
        {
            // 💡 수정됨: Rigidbody가 없는 '가짜' 오브젝트는 리스트에 넣지 않고 사전 차단
            if (other.attachedRigidbody == null)
            {
                Debug.LogWarning($"[경고] '{other.gameObject.name}' 오브젝트가 태그는 맞지만 Rigidbody가 없어 무시됩니다.");
                return;
            }

            if (!triggerList.Contains(other))
            {
                triggerList.Add(other);
            }
        }

        if (other.CompareTag("AGV") && currentGrabbedObject != null && !isJustDropped)
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

    private IEnumerator DropRoutine()
    {
        isJustDropped = true;
        Drop();
        yield return new WaitForSeconds(2.0f);
        isJustDropped = false;
    }

    public void Grab()
    {
        // 비활성화되거나 삭제된 오브젝트 리스트에서 정리
        triggerList.RemoveAll(item => item == null || !item.gameObject.activeInHierarchy);

        // 💡 수정됨: triggerList[0]만 보지 않고, 리스트 전체를 순회하며 진짜 잡을 수 있는 객체를 탐색
        for (int i = 0; i < triggerList.Count; i++)
        {
            Collider targetCollider = triggerList[i];
            Rigidbody rb = targetCollider.attachedRigidbody;

            if (rb != null)
            {
                currentGrabbedObject = rb.gameObject; // 진짜 물리 몸통을 잡아냅니다.

                rb.isKinematic = true; // 물리 끄기
                currentGrabbedObject.transform.SetParent(this.transform);

                if (useSnap)
                {
                    currentGrabbedObject.transform.localPosition = grabPosition;
                    currentGrabbedObject.transform.localRotation = Quaternion.Euler(grabRotation);
                }

                return; // 하나를 성공적으로 잡았다면 즉시 함수를 종료하여 불필요한 연산 방지
            }
        }
    }

    public void Drop()
    {
        if (currentGrabbedObject != null)
        {
            Rigidbody rb = currentGrabbedObject.GetComponent<Rigidbody>();

            currentGrabbedObject.transform.SetParent(null);

            if (rb != null)
            {
                rb.isKinematic = false; // 물리 켜기
                rb.linearVelocity = currentVelocity;
            }

            currentGrabbedObject = null; // 손 비우기
        }
    }
}