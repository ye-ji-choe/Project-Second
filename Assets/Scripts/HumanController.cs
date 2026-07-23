using System.Collections;
using UnityEngine;

public class HumanController : MonoBehaviour
{
    [Header("사람 애니메이션 설정")]
    [Tooltip("제어할 사람의 Animator를 연결하세요.")]
    public Animator humanAnimator;
    public Placer placer; // Placer 스크립트 참조

    // AGV 센서에 감지되었을 때 호출할 함수
    public void OnSensorTriggered(bool isDetected)
    {
        // 센서에 감지되면 무조건 처음부터 다시 시작
        if (isDetected)
        {
            // 혹시라도 실행 중이던 이전 애니메이션 코루틴을 강제 종료
            StopAllCoroutines();
            StartCoroutine(WorkCycleRoutine());
        }
    }

    private IEnumerator WorkCycleRoutine()
    {
        // 1. 애니메이션을 강제로 가장 처음 상태(Idle)로 되돌림
        humanAnimator.Play("Idle", 0, 0f);

        // 2. 작업 시작 신호 켜기
        humanAnimator.SetBool("isWork", true);

        // 3. 중간 과정을 거쳐 'Pushing' 상태에 진입할 때까지 대기
        yield return new WaitUntil(() => humanAnimator.GetCurrentAnimatorStateInfo(0).IsName("Pushing"));

        // 4. 'Pushing' 애니메이션을 완전히 끝마칠 때까지 대기
        yield return new WaitWhile(() => humanAnimator.GetCurrentAnimatorStateInfo(0).IsName("Pushing"));

        // 5. Pushing 이후 작업 신호 끄기
        // (이후 남은 애니메이션이 진행되며 자연스럽게 Idle로 복귀합니다)
        humanAnimator.SetBool("isWork", false);
    }
}