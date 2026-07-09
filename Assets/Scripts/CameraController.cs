using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Cam cam;
    public Transform[] positions;
    public void OnCamera1()
    {
        Debug.Log("1번 버튼 클릭");
        cam.MoveToDestination(positions[0]);
    }    
}
