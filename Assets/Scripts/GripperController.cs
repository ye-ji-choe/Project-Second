using UnityEngine;

public class GripperController : MonoBehaviour
{
    public Animator anim;
    public GripperArea area;

    public bool isOn;

    private void Start()
    {
        //anim변수가 비어있으면 찾아서 넣어라.
        if (anim == null)
            anim = GetComponent<Animator>();
    }

    public void TurnOn(bool isOn)
    {
        this.isOn = isOn;
        //애니메이터가 변수에 참조되어 있지않으면 Return
        if (anim == null)
            return;

        //bool타입 파라메터에 참 혹은 거짓 데이터 적용.
        anim.SetBool("IsOn", isOn);
    }
    public void PickUp()
    {
        //애니메이터가 변수에 참조되어 있지않으면 Return
        if (anim == null)
            return;

        //Trigger타입 파마메터 발동
        anim.SetTrigger("PickUp");
    }

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