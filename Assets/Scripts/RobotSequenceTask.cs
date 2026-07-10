using UnityEngine;
using System.Collections.Generic;

public class RobotSequenceTask : Task
{
    [Header("티칭 포인트 설정")]
    [SerializeField] private List<Target> _targets;

    [Header("커넥터 연결")]
    public RobotConnector connector; // 완료 신호를 다시 돌려주기 위해 연결

    private bool canStartSequence = false; // 커넥터가 켜줄 스위치

    // 커넥터(Update)에서 호출하여 큐의 대기를 풀어주는 함수
    public void ResumeSequence()
    {
        canStartSequence = true;
    }

    override protected void Program()
    {

        this.Log("PLC 기동 신호 및 D0=1 대기 중...");

        // 1. 커넥터가 canStartSequence를 true로 만들어 줄 때까지 무한 대기
        this.WaitUntil(() => canStartSequence);

        this.Log("기동 조건 충족 (D0=1). 로봇 시퀀스 시작!");
        // 이제 이 로그들은 명령 큐에 순서대로 들어가서
        // 실제 로봇이 움직일 때 하나씩 실행됩니다.
        this.Log("로봇 시퀀스 시작");

        this.Log("Home 포인트로 이동 시작");
        this.LIN(_targets[0]);
        this.Wait(500);

        this.Log("경유지점 이동");
        this.LIN(_targets[1]);

        this.Log("작업 위치 1번 이동");
        this.LIN(_targets[2]);




        this.Log("Z축 50mm 상승 작업");
        // 새롭게 추가한 함수 사용: 타겟[2] 기준으로 Z축으로 50f 오프셋, 속도 0.1f
        this.Offset(_targets[2], new Vector3(0, 0, 50f), 0.1f);

        this.Log("X축으로 20mm, Z축으로 -10mm 진입 작업");
        this.Offset(_targets[2], new Vector3(20f, 0, -10f), 0.2f);


        this.Log("작업 위치 2번 정밀 이동");
        this.LIN(_targets[3]);

        this.Log("경유지점 이동");
        this.LIN(_targets[1]);

        this.Log("작업 위치 1번 이동");
        this.LIN(_targets[2]);

        this.Log("경유지점 이동");
        this.LIN(_targets[1]);

        this.Log("작업 위치 3번 이동");
        this.LIN(_targets[4]);

        this.Log("경유지점 이동");
        this.LIN(_targets[1]);

        this.Log("Home으로 복귀");
        this.LIN(_targets[0]);

        this.Log("로봇 시퀀스 종료");

        // 2. 작업 종료 시: 커넥터에 완료 통보 및 다음 사이클을 위해 스위치 초기화
        this.DoAction(() =>
        {
            connector.OnCycleCompleted(); // M1094 펄스 발생 & BUSY OFF
            canStartSequence = false;     // 다음 작업을 위해 스위치 리셋
            Debug.Log("[로봇] 작업 완료. PLC로 M1094 신호 전송 완료.");
        });
    }
}