using UnityEngine;

public class PLCButtonController : MonoBehaviour
{
    // 시작 버튼을 눌렀을 때 실행될 함수
    public void TurnOnM0()
    {
        // MXRequester가 씬에 존재하는지 확인
        if (MXRequester.Get != null)
        {
            // M0 주소에 1(ON) 값을 쓰도록 요청합니다.
            MXRequester.Get.AddSetDeviceRequest("M0", 1);
            Debug.Log("전체라인 가동 시작! (M0 ON)");
        }
        else
        {
            Debug.LogError("MXRequester를 찾을 수 없습니다. 씬에 배치되어 있는지 확인하세요.");
        }
    }

    // (참고) 만약 버튼을 한 번 더 눌러서 정지(OFF)도 시키고 싶다면 아래 함수를 쓰면 됩니다.
    public void TurnOffM0()
    {
        if (MXRequester.Get != null)
        {
            MXRequester.Get.AddSetDeviceRequest("M0", 0);
            Debug.Log("전체라인 가동 정지! (M0 OFF)");
        }
    }
}