using UnityEngine;
using System.Collections.Generic;

public class VcumRobotSequenceTask : Task
{
    [Header("티칭 포인트 설정 (0:원점, 1:경유, 2~3:픽업, 4:AGV배치)")]
    [SerializeField] private List<Target> _targets;

    [Header("커넥터 연결")]
    // 💡 연결할 커넥터 스크립트 이름에 맞춰 타입을 수정해 주세요. (예: RobotConnector)
    public VcumRobotConnector connector;

    public float defaultSpeed = 2f;

    // 현재 진입한 AGV가 1번인지 2번인지 추적 (true = 1번 AGV, false = 2번 AGV)
    private bool isFirstAGVTurn = true;

    override protected void Program()
    {
        this.Log($"기동 조건 충족. {(isFirstAGVTurn ? "1번" : "2번")} AGV 커버 조립 VcumRobot 시퀀스 시작!");

        // ================= [ 0번: 원점 출발 ] =================
        this.Log("Home 포인트로 이동 시작");
        this.LIN(_targets[0], defaultSpeed);
        this.Wait(500);

        if (isFirstAGVTurn)
        {
            // ================= [ 1번 AGV 작업: 1 -> 2 -> 4 ] =================
            this.Log("경유지점 1번 이동");
            this.LIN(_targets[1], defaultSpeed);

            // -------- 2번: 1번 커버 픽업 --------
            this.Log("작업 위치 2번 이동 (1번 커버 픽업)");
            this.LIN(_targets[2], defaultSpeed);
            this.Log("Z축 상승/하강 작업");
            this.Offset(_targets[2], new Vector3(0, -300f, -600f), defaultSpeed);
            this.Offset(_targets[2], new Vector3(0, -300f, 0f), defaultSpeed);
            this.Log("Y축 하강/상승 작업");
            this.Offset(_targets[2], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Offset(_targets[2], new Vector3(0, -300f, 0f), defaultSpeed);

            // -------- 4번: AGV 위 배치 --------
            this.Log("작업 위치 4번 이동 (AGV 위 커버 배치)");
            this.LIN(_targets[4], defaultSpeed);
            this.Log("Z축 상승/하강 작업");
            this.Offset(_targets[4], new Vector3(0, -300f, -600f), defaultSpeed);
            this.Offset(_targets[4], new Vector3(0, -300f, 0f), defaultSpeed);
            this.Log("Y축 하강/상승 작업");
            this.Offset(_targets[4], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Offset(_targets[4], new Vector3(0, -300f, 0f), defaultSpeed);
        }
        else
        {
            // ================= [ 2번 AGV 작업: 1 -> 3 -> 4 ] =================
            this.Log("경유지점 1번 이동");
            this.LIN(_targets[1], defaultSpeed);

            // -------- 3번: 2번 커버 픽업 --------
            this.Log("작업 위치 3번 이동 (2번 커버 픽업)");
            this.LIN(_targets[3], defaultSpeed);
            this.Log("Z축 상승/하강 작업");
            this.Offset(_targets[3], new Vector3(0, -300f, -600f), defaultSpeed);
            this.Offset(_targets[3], new Vector3(0, -300f, 0f), defaultSpeed);
            this.Log("Y축 하강/상승 작업");
            this.Offset(_targets[3], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Offset(_targets[3], new Vector3(0, -300f, 0f), defaultSpeed);

            // -------- 4번: AGV 위 배치 --------
            this.Log("작업 위치 4번 이동 (AGV 위 커버 배치)");
            this.LIN(_targets[4], defaultSpeed);
            this.Log("Z축 상승/하강 작업");
            this.Offset(_targets[4], new Vector3(0, -300f, -600f), defaultSpeed);
            this.Offset(_targets[4], new Vector3(0, -300f, 0f), defaultSpeed);
            this.Log("Y축 하강/상승 작업");
            this.Offset(_targets[4], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Offset(_targets[4], new Vector3(0, -300f, 0f), defaultSpeed);
        }

        // ================= [ 0번: 원점 복귀 ] =================
        this.Log("해당 AGV 작업 완료. 대기를 위해 Home 포인트로 이동 시작");
        this.LIN(_targets[0], defaultSpeed);
        this.Wait(500);

        this.Log("로봇 시퀀스 종료");

        this.DoAction(() =>
        {
            // 다음 번 기동 시에는 다른 AGV 작업을 수행하도록 상태 반전
            isFirstAGVTurn = !isFirstAGVTurn;

            // 작업 완료 신호를 커넥터로 전달
            if (connector != null)
            {
                connector.OnCycleCompleted();
            }
            Debug.Log("[VcumRobot] 작업 완료 및 원점 복귀. PLC로 완료 신호 전송.");
        });
    }
}