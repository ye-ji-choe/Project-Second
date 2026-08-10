using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ChamberController : MonoBehaviour
{
    public ChamberConnector connector;
    public Animator doorAnimator;

    [Header("Sensors (마그네틱 1개, 레이저 4개 할당)")]
    public MagneticSensor magneticSensor;
    public OpticalSensor[] laserSensors = new OpticalSensor[4];

    [Header("Animation Settings")]
    public float doorAnimDuration = 2.0f;

    private bool isDoorOpen = false;

    // 레이저 센서 배열 이벤트 구독 해제를 위한 캐싱
    private UnityAction<bool>[] laserActions = new UnityAction<bool>[4];

    private void Awake()
    {
        // 1. 단일 마그네틱 센서 이벤트 연결
        if (magneticSensor != null)
        {
            magneticSensor.onChangedDetected.AddListener(OnMagneticSensorStateChanged);
        }
        else
        {
            Debug.LogWarning("[ChamberController] 마그네틱 센서가 인스펙터에 할당되지 않았습니다.");
        }

        // 2. 레이저 센서 배열 4개 이벤트 연결
        for (int i = 0; i < laserSensors.Length; i++)
        {
            if (laserSensors[i] != null)
            {
                int index = i; // 클로저 이슈 방지를 위한 캡처
                laserActions[i] = (isDetected) => OnLaserSensorStateChanged(index, isDetected);
                laserSensors[i].onChangedDetected.AddListener(laserActions[i]);
            }
            else
            {
                Debug.LogWarning($"[ChamberController] 레이저 센서 [{i}]가 인스펙터에 할당되지 않았습니다.");
            }
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 해제 작업
        if (magneticSensor != null)
        {
            magneticSensor.onChangedDetected.RemoveListener(OnMagneticSensorStateChanged);
        }

        for (int i = 0; i < laserSensors.Length; i++)
        {
            if (laserSensors[i] != null && laserActions[i] != null)
            {
                laserSensors[i].onChangedDetected.RemoveListener(laserActions[i]);
            }
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
    // 단일 마그네틱 센서 감지 콜백
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
    // 다중 레이저 센서 감지 콜백
    // ==================================================
    private void OnLaserSensorStateChanged(int index, bool isDetected)
    {
        if (isDetected)
        {
            Debug.Log($"레이저 센서 [{index}] ON -> PLC로 1 전송");
            connector.SendLaserSensorSignal(index, 1);
        }
        else
        {
            Debug.Log($"레이저 센서 [{index}] OFF -> PLC로 0 전송");
            connector.SendLaserSensorSignal(index, 0);
        }
    }
}