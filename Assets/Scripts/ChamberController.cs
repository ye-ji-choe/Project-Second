using System.Collections;
using UnityEngine;

public class ChamberController : MonoBehaviour
{
    public ChamberConnector connector; // 인스펙터 연결 필수
    public Animator doorAnimator;      // 챔버 도어 모델 할당 필수

    [Header("Sensors (인스펙터에서 할당)")]
    // 두 센서가 같은 MagneticSensor 스크립트를 쓴다고 가정 (이름이 다르다면 타입을 변경하세요)
    public MagneticSensor magneticSensor;
    public OpticalSensor laserSensor;

    [Header("Animation Settings")]
    public float doorAnimDuration = 2.0f;

    private bool isDoorOpen = false;

    private void Awake()
    {
        // 1. 마그네틱 센서 이벤트 연결
        if (magneticSensor != null)
        {
            magneticSensor.onChangedDetected.AddListener(OnMagneticSensorStateChanged);
        }
        else
        {
            Debug.LogError("에러: 마그네틱 센서가 인스펙터에 할당되지 않았습니다.");
        }

        // 2. 레이저 센서 이벤트 연결 (추가된 부분)
        if (laserSensor != null)
        {
            laserSensor.onChangedDetected.AddListener(OnLaserSensorStateChanged);
        }
        else
        {
            Debug.LogError("에러: 레이저 센서가 인스펙터에 할당되지 않았습니다.");
        }
    }

    private void OnDestroy()
    {
        // 스크립트 파괴 시 메모리 누수 방지를 위해 이벤트 연결 해제
        if (magneticSensor != null)
        {
            magneticSensor.onChangedDetected.RemoveListener(OnMagneticSensorStateChanged);
        }
        if (laserSensor != null)
        {
            laserSensor.onChangedDetected.RemoveListener(OnLaserSensorStateChanged);
        }
    }

    private void Update()
    {
        if (connector == null) return;

        if (connector.HaveToOpenDoor && !isDoorOpen)
        {
            OpenDoorAnimation();
        }
        else if (connector.HaveToCloseDoor && isDoorOpen)
        {
            CloseDoorAnimation();
        }
    }

    private void OpenDoorAnimation()
    {
        isDoorOpen = true;
        Debug.Log("M2020 상태 감지됨 -> 유니티 도어 오픈 애니메이션 실행");

        if (doorAnimator != null)
        {
            doorAnimator.SetBool("IsOpen", true);
        }
    }

    private void CloseDoorAnimation()
    {
        isDoorOpen = false;
        Debug.Log("M2026 상태 감지됨 -> 유니티 도어 클로즈 애니메이션 실행");

        if (doorAnimator != null)
        {
            doorAnimator.SetBool("IsOpen", false);
        }
    }

    // ==================================================
    // 마그네틱 센서 감지 콜백
    // ==================================================
    private void OnMagneticSensorStateChanged(bool isDetected)
    {
        if (isDetected)
        {
            Debug.Log("마그네틱 센서 ON -> PLC로 1 전송");
            connector.SendMagneticSensorSignal(1);
        }
        else
        {
            Debug.Log("마그네틱 센서 OFF -> PLC로 0 전송");
            connector.SendMagneticSensorSignal(0);
        }
    }

    // ==================================================
    // 레이저 센서 감지 콜백 (추가된 부분)
    // ==================================================
    private void OnLaserSensorStateChanged(bool isDetected)
    {
        if (isDetected)
        {
            Debug.Log("레이저 센서 ON -> PLC로 1 전송");
            connector.SendLaserSensorSignal(1);
        }
        else
        {
            Debug.Log("레이저 센서 OFF -> PLC로 0 전송");
            connector.SendLaserSensorSignal(0);
        }
    }
}