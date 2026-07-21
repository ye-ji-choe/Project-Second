using System.Collections;
using UnityEngine;
using UnityEngine.Events;


public class ButtonController : MonoBehaviour
{
    public UnityEvent<bool> onChangedDetected;
    private bool hasDetected;
    [Header("버튼 설정")]
    [Tooltip("제어할 버튼의 Animator를 연결하세요.")]
    public Animator buttonAnimator;

    [Tooltip("센서 감지 후 대기할 시간 (초)")]
    public float delayTime = 10f;

    [Tooltip("버튼이 눌려있는 유지 시간 (초)")]
    public float pressDuration = 0.5f;

    private bool isRunning = false;

    // 레이저 센서가 감지 상태가 변할 때마다 이 함수를 호출할 것입니다.
    // isDetected가 true면 감지됨, false면 감지 안 됨
    public void OnSensorTriggered(bool isDetected)
    {
        // 센서에 물체가 감지되었고(true), 애니메이션 시퀀스가 실행 중이 아닐 때만 시작
        if (isDetected && !isRunning)
        {
            StartCoroutine(ButtonRoutine());
        }
    }

    private IEnumerator ButtonRoutine()
    {
        isRunning = true;

        // 1. 센서 감지 후 10초 대기
        yield return new WaitForSeconds(delayTime);

        // 2. 버튼 내려가는 애니메이션 실행
        buttonAnimator.SetBool("Complete", true);

        // 3. 버튼이 내려간 상태 유지
        yield return new WaitForSeconds(pressDuration);

        // 4. 올라가는 애니메이션 실행
        buttonAnimator.SetBool("Complete", false);

        isRunning = false;
    }

    public bool HasDetected
    {
        get => hasDetected;

        private set
        {
            //결과가 동일하면 무시
            if (hasDetected == value)
                return;
            hasDetected = value;
            //등록된 콜백 함수들에게 최신 결과를 알림
            onChangedDetected?.Invoke(value);
        }
    }
}