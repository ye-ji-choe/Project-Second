using System.Collections;
using UnityEngine;

public class ChamberController : MonoBehaviour
{
    public ChamberConnector connector; // 인스펙터에서 할당
    public Animator doorAnimator;

    [Header("Animation Settings")]
    public float doorAnimDuration = 2.0f; // 문이 완전히 열리거나 닫히는 데 걸리는 시간

    // ==================================================
    // 센서 감지부 (유니티 물리 엔진 -> 커넥터로 전달)
    // ==================================================

    // AGV가 마그네틱 센서(바깥)에 닿았을 때 센서 스크립트에서 이 함수를 호출
    public void OnMagneticSensorTriggered()
    {
        Debug.Log("마그네틱 센서 감지됨 -> PLC로 신호 전송");
        // 마그네틱 센서 ON (1) 전송. (PLC가 이 신호를 받고 도어 오픈 명령을 줄 것입니다)
        connector.SendMagneticSensorSignal(1);
    }

    // AGV가 레이저 센서(안쪽)에 닿았을 때 센서 스크립트에서 이 함수를 호출
    public void OnLaserSensorTriggered()
    {
        Debug.Log("레이저 센서 감지됨 -> PLC로 신호 전송");
        // 레이저 센서 ON (1) 전송. (PLC가 이 신호를 받고 도어 클로즈 명령을 줄 것입니다)
        connector.SendLaserSensorSignal(1);
    }

    // ==================================================
    // 동작 실행부 (커넥터 -> 유니티 애니메이션 실행)
    // ==================================================

}