using UnityEngine;

public class Door : MonoBehaviour
{
    public Animator animator;   

    public void OpenDoor(bool isOpen)
    {
        animator.SetBool("IsOpen", isOpen);
    }
}
