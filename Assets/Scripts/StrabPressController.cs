using System.Collections;
using UnityEngine;

public class StrabPressController : MonoBehaviour
{
    public StrabPressConnector connector; // 인스펙터에서 할당
    public Animator Press_001Animator;

    [Header("Sensors (인스펙터에서 할당)")]
    public OpticalSensor laserSensor; // 레이저 센서 할당용 슬롯

    [Header("Animation Settings")]
    public float Press_001AnimDuration = 2.0f; // (현재 20초 하드코딩으로 미사용) 프레스 1사이클 소요 시간

    private bool isPressing = false; // 로컬 상태 (애니메이션 상태와 동기화됨)
    private bool isSystemReady = true; // 프레스 READY 유지 플래그

    private void Awake()
    {
        // 센서 이벤트 구독 연결
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
        // 메모리 누수 방지용 구독 해제
        if (laserSensor != null)
        {
            laserSensor.onChangedDetected.RemoveListener(OnLaserSensorStateChanged);
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1.0f); // 통신 안정화 대기
        StartCoroutine(KeepReadySignalAlive());
    }

    // READY 신호를 지속적으로 유지하는 하드와이어링 모사 함수
    private IEnumerator KeepReadySignalAlive()
    {
        while (true)
        {
            if (isSystemReady && connector != null && connector.PressReady.useDevice)
            {
                MXRequester.Get.AddSetDeviceRequest(connector.PressReady.address, 1);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void Update()
    {
        if (connector == null) return;

        // 💡 1. 애니메이션 시작 조건 검사부
        // 조건: READY 상태 유지 중(isSystemReady) + PLC에서 M2190 수신(HaveToStart) + 현재 정지 중(!isPressing)
        if (isSystemReady && connector.HaveToStart && !isPressing)
        {
            // 위 조건이 모두 맞을 때 프레스 동작 코루틴을 호출합니다.
            StartCoroutine(ExecutePressSequence());
        }
    }
    // ==================================================
    // 💡 2. 애니메이션 실행 및 IsPressing ON/OFF, M-Code 제어부
    // ==================================================
    private IEnumerator ExecutePressSequence()
    {
        // 1. M2190 신호를 받아 프레스 동작 상태(로컬)를 즉시 ON 시킵니다.
        isPressing = true;

        // READY 신호 송출 중단 및 OFF 전송 (BUSY 상태 진입)
        isSystemReady = false;
        if (connector.PressReady.useDevice)
        {
            MXRequester.Get.AddSetDeviceRequest(connector.PressReady.address, 0);
        }

        Debug.Log("PLC 기동 명령(M2190) 수신 -> local isPressing ON 및 Animator isPressing ON");

        // 유니티 애니메이션 Boolean 파라미터 "isPressing"을 true로 설정하여 애니메이션 실행
        if (Press_001Animator != null)
        {
            Press_001Animator.SetBool("isPressing", true);
        }

        // 2. 요청하신 20초 딜레이 적용 (애니메이션 구동 시간)
        yield return new WaitForSeconds(5.0f);

        // 3. 20초 경과 후 Animator의 Boolean 파라미터 "isPressing"을 꺼서 애니메이션 중지
        if (Press_001Animator != null)
        {
            Press_001Animator.SetBool("isPressing", false);
        }
        Debug.Log("20초 대기 완료 -> Animator isPressing OFF");

        // 4. 애니메이션이 꺼진 후 M1174(완료 신호) Set
        MXRequester.Get.AddSetDeviceRequest("M1174", 1);
        connector.SendPressCompleteSignal(1); // (참고용 중복 제어 방지를 위해 필요에 따라 하나만 남기셔도 됩니다)

        Debug.Log("M1174 Set 완료 -> PLC 기동 신호(M2190)가 꺼질 때까지 대기합니다.");

        // 💡 [핵심 수정] PLC가 M1174를 확인하고 HaveToStart(M2190)를 끄는 시점까지 대기합니다.
        // 이 코드가 있어야 이중 실행(Double Trigger)을 완벽히 방지할 수 있습니다.
        yield return new WaitUntil(() => connector.HaveToStart == false);

        // PLC가 기동 신호를 끈 것을 확인한 후, 0.5초 펄스 유지
        yield return new WaitForSeconds(0.5f);

        // M1174 완료 신호 Reset
        MXRequester.Get.AddSetDeviceRequest("M1174", 0);
        connector.SendPressCompleteSignal(0);

        // 💡 [핵심 수정] 모든 통신 핸드쉐이크가 끝난 이 시점에 로컬 플래그를 초기화합니다.
        isPressing = false;
        isSystemReady = true;

        Debug.Log("사이클 종료 완료 -> 통신 정상화 및 READY(ON) 복구");
    }

    // ==================================================
    // 💡 센서 감지 콜백 (READY 유지, 구역 센서만 제어)
    // ==================================================
    private void OnLaserSensorStateChanged(bool isDetected)
    {
        if (connector == null) return;

        if (isDetected)
        {
            // 센서가 감지되어도 READY는 끄지 않습니다.
            Debug.Log("레이저 센서 ON (AGV 진입) -> 구역 센서 ON (READY 상태 유지)");

            if (connector.AreaSensor.useDevice)
                MXRequester.Get.AddSetDeviceRequest(connector.AreaSensor.address, 1);
        }
        else
        {
            Debug.Log("레이저 센서 OFF (AGV 빠져나감) -> 구역 센서 OFF");

            if (connector.AreaSensor.useDevice)
                MXRequester.Get.AddSetDeviceRequest(connector.AreaSensor.address, 0);
        }
    }
}