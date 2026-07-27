using UnityEngine;

public class GripperController : MonoBehaviour
{
    public GripperArea area;

    public bool isOn;


    public void Pick()
    {
        if (area.triggerList.Count == 0)
            return;

        foreach (Collider c in area.triggerList)
        {
            c.attachedRigidbody.isKinematic = true;
            c.transform.SetParent(area.transform);
        }
    }
    public void Drop()
    {
        foreach (Collider c in area.triggerList)
        {
            c.attachedRigidbody.isKinematic = false;
            c.transform.SetParent(null);
            c.attachedRigidbody.linearVelocity = area.currentVelocity;
        }
    }
}