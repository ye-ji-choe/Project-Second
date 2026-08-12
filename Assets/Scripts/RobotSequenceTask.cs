using UnityEngine;
using System.Collections.Generic;

public class RobotSequenceTask : Task
{
    [Header("티칭 포인트 설정")]
    [SerializeField] private List<Target> _targets;

    [Header("커넥터 연결")]
    public RobotConnector connector;

    public float defaultSpeed = 2f;

    // 현재 진입한 AGV가 1번인지 2번인지 추적하기 위한 변수 (true = 1번, false = 2번)
    private bool isFirstAGVTurn = true;

    override protected void Program()
    {
        this.Log($"기동 조건 충족. {(isFirstAGVTurn ? "1번" : "2번")} AGV 로봇 시퀀스 시작!");

        // [추가/확인] 시퀀스 시작 시 원점에서 시작
        this.Log("Home 포인트(원점)로 이동 및 확인");
        this.LIN(_targets[0], defaultSpeed);
        this.Wait(500);

        if (isFirstAGVTurn)
        {
            // ================= [ 1번 AGV 작업: 1 -> 3 -> 5 ] =================
            this.Log("1번 경유지점 이동");
            this.LIN(_targets[1], defaultSpeed);

            this.Log("1번 잡는 곳(3) 픽업 작업");
            this.Offset(_targets[3], new Vector3(0, -500f, -600f), defaultSpeed);
            this.Offset(_targets[3], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Offset(_targets[3], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Offset(_targets[3], new Vector3(0, -500f, 0f), defaultSpeed);

            this.Log("놓는 곳(5) 이동");
            this.LIN(_targets[5], defaultSpeed);

            this.Log("놓는 곳(5) 배치 작업");
            this.Offset(_targets[5], new Vector3(0, -200f, 0f), defaultSpeed);
            this.Offset(_targets[5], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Offset(_targets[5], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Offset(_targets[5], new Vector3(0, -400f, 0f), defaultSpeed);
        }
        else
        {
            // ================= [ 2번 AGV 작업: 2 -> 4 -> 5 ] =================
            this.Log("2번 경유지점 이동");
            this.LIN(_targets[2], defaultSpeed);

            this.Log("2번 잡는 곳(4) 픽업 작업");
            this.Offset(_targets[4], new Vector3(0, -600f, -600f), defaultSpeed);
            this.Offset(_targets[4], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Offset(_targets[4], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Offset(_targets[4], new Vector3(0, -600f, 0f), defaultSpeed);

            this.Log("놓는 곳(5) 이동");
            this.LIN(_targets[5], defaultSpeed);

            this.Log("놓는 곳(5) 배치 작업");
            this.Offset(_targets[5], new Vector3(0, -400f, 0f), defaultSpeed);
            this.Offset(_targets[5], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Offset(_targets[5], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Offset(_targets[5], new Vector3(0, -400f, 0f), defaultSpeed);
        }

        // ================= [ 작업 종료 후 원점 복귀 ] =================
        // 1번이든 2번이든 놓는 곳(5) 작업이 끝나면 다음 AGV를 위해 무조건 원점으로 돌아갑니다.
        this.Log("작업 완료. 다음 AGV 대기를 위해 원점(Home)으로 복귀");
        this.LIN(_targets[0], defaultSpeed);
        this.Wait(500);

        this.Log("로봇 시퀀스 종료");

        this.DoAction(() =>
        {
            // 다음 AGV가 호출되었을 때 반대쪽 분기를 타도록 상태를 뒤집습니다.
            isFirstAGVTurn = !isFirstAGVTurn;

            // 작업이 완료되면 커넥터에 알려 Busy를 끄고 완료 신호를 보냅니다.
            connector.OnCycleCompleted();
            Debug.Log("[로봇] 원점 복귀 및 작업 완료. PLC로 M1094 신호 전송 완료.");
        });
    }
}