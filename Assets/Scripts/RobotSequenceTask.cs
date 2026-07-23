using UnityEngine;
using System.Collections.Generic;

public class RobotSequenceTask : Task
{
    [Header("티칭 포인트 설정")]
    [SerializeField] private List<Target> _targets;

    [Header("커넥터 연결")]
    public RobotConnector connector;

    public float defaultSpeed = 2f;

    // [수정] 외부에서 변수를 조작할 필요가 없어졌으므로 ResumeSequence()와 canStartSequence 변수는 삭제합니다.

    override protected void Program()
    {
        // [수정] WaitUntil 삭제. 외부(Connector)에서 이 Task를 Play() 하는 순간 조건이 충족된 것이므로 바로 로봇을 움직입니다.
        this.Log("기동 조건 충족 (D0=1). 로봇 시퀀스 시작!");

        this.Log("Home 포인트로 이동 시작");
        this.LIN(_targets[0], 5f);
        this.Wait(500);

        this.Log("경유지점 이동");
        this.LIN(_targets[1], 5f);

        this.Log("Z축 50mm 상승 작업");
        this.Offset(_targets[2], new Vector3(0, -100f, -500f), 5f);

        this.Log("Z축 50mm 상승 작업");
        this.Offset(_targets[2], new Vector3(0, -100f, 0f), 5f);

        this.Log("작업 위치 1번 이동");
        this.LIN(_targets[2], 5f);

        this.Log("Z축 50mm 상승 작업");
        this.Offset(_targets[2], new Vector3(0, -100f, 0f), 5f);

        this.Log("Z축 50mm 상승 작업");
        this.Offset(_targets[2], new Vector3(0, -100f, -500f), defaultSpeed);

        this.Log("Z축 50mm 상승 작업");
        this.Offset(_targets[3], new Vector3(0, -100f, 0f), defaultSpeed);

        this.Log("작업 위치 2번 정밀 이동");
        this.LIN(_targets[3], defaultSpeed);

        this.Log("Z축 50mm 상승 작업");
        this.Offset(_targets[3], new Vector3(0, -100f, 0f), defaultSpeed);

        this.Log("경유지점 이동");
        this.LIN(_targets[1], defaultSpeed);

        this.Log("Z축 50mm 상승 작업");
        this.Offset(_targets[2], new Vector3(0, -100f, -500f), defaultSpeed);

        this.Log("Z축 50mm 상승 작업");
        this.Offset(_targets[2], new Vector3(0, -100f, 0f), defaultSpeed);

        this.Log("작업 위치 1번 이동");
        this.LIN(_targets[2], defaultSpeed);

        this.Log("Z축 50mm 상승 작업");
        this.Offset(_targets[2], new Vector3(0, -100f, 0f), defaultSpeed);

        this.Log("Z축 50mm 상승 작업");
        this.Offset(_targets[2], new Vector3(0, -100f, -500f), defaultSpeed);

        this.Log("경유지점 이동");
        this.LIN(_targets[1], defaultSpeed);

        this.Log("Z축 50mm 상승 작업");
        this.Offset(_targets[4], new Vector3(0, -100f, 0f), defaultSpeed);

        this.Log("작업 위치 3번 이동");
        this.LIN(_targets[4], defaultSpeed);

        this.Log("Z축 50mm 상승 작업");
        this.Offset(_targets[4], new Vector3(0, -100f, 0f), defaultSpeed);

        this.Log("경유지점 이동");
        this.LIN(_targets[1], defaultSpeed);

        this.Log("Home으로 복귀");
        this.LIN(_targets[0], defaultSpeed);

        this.Log("로봇 시퀀스 종료");

        this.DoAction(() =>
        {
            // 작업이 완료되면 커넥터에 알려 Busy를 끄고 완료 신호를 보냅니다.
            connector.OnCycleCompleted();
            Debug.Log("[로봇] 작업 완료. PLC로 M1094 신호 전송 완료.");

            // Task는 여기서 자연스럽게 종료되며, 다음 AGV가 오면 Connector가 다시 처음부터 Play() 해줍니다.
        });
    }
}