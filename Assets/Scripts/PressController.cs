using System.Collections;
using UnityEngine;

public class PressController : MonoBehaviour
{
    public PressConnector connector;
    public Animator pRESSSSSSSSSSSSSSSSAnimator;

    [Header("Animation & Process Settings")]
    [Tooltip("프레스가 완전히 내려가거나 올라가는 데 걸리는 시간")]
    public float pRESSSSSSSSSSSSSSSSAnimDuration = 2.0f;

    [Tooltip("프레스 하강 후 유지(가압)하는 시간")]
    public float pressHoldTime = 10.0f;

    [Header("Process State")]
    public bool isPressing = false; // 현재 프레스 상태 모니터링용

    [Header("Sequence Settings")]
    public float readyPulseInterval = 0.1f; // (수정됨) 짧은 기동 펄스를 놓치지 않기 위해 0.1초로 단축

    private bool isSystemReady = true;
    private bool isRunning = true;

    private void Start()
    {
        if (connector != null)
        {
            StartCoroutine(SequenceMonitorLoop());
        }
        else
        {
            Debug.LogError("PressConnector가 할당되지 않았습니다.");
        }
    }

    // 상태 머신 역할을 하는 메인 시퀀스 루프

    private IEnumerator SequenceMonitorLoop()
    {
        while (isRunning)
        {
            if (isSystemReady)
            {
                connector.SendReadySignal(1);

                if (connector.isStartCommandReceived)
                {
                    // [수정 포인트 1] 기동 신호를 감지한 즉시 플래그를 수동으로 리셋 (신호 소비)
                    connector.isStartCommandReceived = false;

                    isSystemReady = false;
                    connector.SendReadySignal(0);

                    Debug.Log("[시퀀스] 기동 시작 신호(M2130) 수신 완료. READY OFF 및 프레스 공정 시작.");

                    StartCoroutine(ExecutePressProcess());
                }
            }
            yield return new WaitForSeconds(readyPulseInterval);
        }
    }


    // 실제 프레스 1사이클 동작 프로세스
    private IEnumerator ExecutePressProcess()
    {
        isPressing = true;
        if (pRESSSSSSSSSSSSSSSSAnimator != null)
            pRESSSSSSSSSSSSSSSSAnimator.SetBool("isPressing", true);
        Debug.Log("[동작] isPressing ON -> 프레스 하강");

        yield return new WaitForSeconds(pressHoldTime);

        isPressing = false;
        if (pRESSSSSSSSSSSSSSSSAnimator != null)
            pRESSSSSSSSSSSSSSSSAnimator.SetBool("isPressing", false);
        Debug.Log("[동작] 10초 경과: isPressing OFF -> 프레스 상승");

        yield return new WaitForSeconds(pRESSSSSSSSSSSSSSSSAnimDuration);

        // ========================================================
        // [핵심 수정] PLC가 무조건 읽을 수 있도록 시간을 두고 펄스를 줍니다.
        // ========================================================
        Debug.Log("[시퀀스] 프레스 1사이클 동작 완료. 완료 신호(ON) 전송.");
        connector.SendChamberCompleteSignal(1);

        // PLC의 스캔 타임을 충분히 고려하여 0.5초간 신호 유지
        yield return new WaitForSeconds(0.5f);

        // 0.5초 뒤에 다시 0으로 리셋
        connector.SendChamberCompleteSignal(0);
        Debug.Log("[시퀀스] 완료 신호(OFF) 리셋.");

        // 신호 리셋까지 끝난 후 상태 복귀
        isSystemReady = true;
        Debug.Log("[시퀀스] 장비 상태 초기화 완료. 다음 사이클 대기.");
    }

    public void OnLaserSensorTriggered()
    {
        Debug.Log("레이저 센서 트리거됨 -> PLC 전송");
        connector.SendLaserSensorSignal(1);
    }

    private void OnDisable()
    {
        isRunning = false;
    }
}