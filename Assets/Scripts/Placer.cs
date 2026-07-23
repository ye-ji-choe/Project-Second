using UnityEngine;
using System.Collections.Generic;
public class Placer : MonoBehaviour
{
    public List<Collider> triggerList = new List<Collider>();   //영역 안에 존재하는 콜라이더 리스트
    public Transform PlacePosition;
    private void OnTriggerEnter(Collider other)
    {
        if (triggerList.Contains(other))
            return;

        triggerList.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!triggerList.Contains(other))
            return;

        triggerList.Remove(other);
    }
    public void place()
    {
        if(triggerList.Count > 0)
        {
            triggerList[0].transform.SetPositionAndRotation(PlacePosition.position, PlacePosition.rotation);
        }
    }

}
