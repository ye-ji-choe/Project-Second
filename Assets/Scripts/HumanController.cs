using System.Collections;
using UnityEngine;

public class HumanController : MonoBehaviour
{
    [Header("사람 애니메이션 설정")]
    [Tooltip("제어할 사람의 Animator를 연결하세요.")]
    public Animator humanAnimator;

    private bool isRunning = false;

    // AGV 센서에 감지되었을 때 호출할 함수
    public void OnSensorTriggered(bool isDetected)
    {
        // 센서에 감지되었고, 현재 애니메이션 사이클이 도는 중이 아닐 때만 실행
        if (isDetected && !isRunning)
        {
            StartCoroutine(WorkCycleRoutine());
        }
    }

    private IEnumerator WorkCycleRoutine()
    {
        isRunning = true;

        // 1. 작업 시작 (Idle부터 시작해서 쭉 진행되도록 켬)
        humanAnimator.SetBool("isWork", true);

        // 2. 애니메이터가 'Pushing' 상태에 진입할 때까지 대기
        // (Grab -> Turn -> Turn2 -> Walk -> Work -> Turn3 -> Walk2 를 모두 지나옴)
        yield return new WaitUntil(() => humanAnimator.GetCurrentAnimatorStateInfo(0).IsName("Pushing"));

        // 3. 'Pushing' 애니메이션이 끝날 때(상태를 벗어날 때)까지 대기
        yield return new WaitWhile(() => humanAnimator.GetCurrentAnimatorStateInfo(0).IsName("Pushing"));

        // 4. Pushing 이후에 isWork를 꺼줌
        // 이제 이후의 Turn4, Walk3가 진행되며 마지막 Walk3 -> Idle 복귀 조건을 만족하게 됨
        humanAnimator.SetBool("isWork", false);

        // 5. 애니메이션이 한 사이클을 다 돌고 완전히 'Idle' 상태로 복귀할 때까지 대기
        yield return new WaitUntil(() =>
            humanAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle") &&
            !humanAnimator.IsInTransition(0)
        );

        // 6. 다음 AGV가 와서 다시 작동할 수 있도록 초기화
        isRunning = false;
    }
}