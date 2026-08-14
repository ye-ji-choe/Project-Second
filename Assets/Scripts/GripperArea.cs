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
    }

    private void Update()
    {
        // 쿨타임이 끝났고 센서에 물체가 있으면 다시 줍기
        if (currentGrabbedObject == null && !isJustDropped && triggerList.Count > 0)
        {
            Grab();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Part") || other.CompareTag("BATTERY"))
        {
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
        triggerList.RemoveAll(item => item == null || !item.gameObject.activeInHierarchy);

        if (triggerList.Count > 0)
        {
            Collider targetCollider = triggerList[0];

            // 💡 유저님의 원래 방식 복구! (attachedRigidbody 사용)
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
            }
        }
    }

    public void Drop()
    {
        if (currentGrabbedObject != null)
        {
            // 💡 유저님의 원래 방식 복구! (정확히 그 몸통의 물리를 다시 켭니다)
            Rigidbody rb = currentGrabbedObject.GetComponent<Rigidbody>();

            currentGrabbedObject.transform.SetParent(null);

            if (rb != null)
            {
                rb.isKinematic = false; // 물리 켜기
                rb.linearVelocity = currentVelocity; // 유저님이 원래 짜셨던 관성 코드
            }

            currentGrabbedObject = null; // 손 비우기
        }
    }
}