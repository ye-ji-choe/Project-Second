using System.Collections.Generic;
using UnityEngine;

public class GripperArea : MonoBehaviour
{
    public List<Collider> triggerList = new List<Collider>();
    public float multiplier = 1f;
    private void OnTriggerEnter(Collider other)
    {
        //트리거 리스트에 트리거된 콜라이더 추가.
        triggerList.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        //트리거 리스트에서 나간 콜라이더 제거.
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
        currentVelocity =
            (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;
    }

}