using UnityEngine;
using System.Collections.Generic;

public class VcumRobotSequenceTask : Task
{
    [Header("티칭 포인트 설정 (0:원점, 1:경유, 2:픽업, 4:AGV배치)")]
    [SerializeField] private List<Target> _targets;

    [Header("커넥터 연결")]
    public VcumRobotConnector connector;

    public float defaultSpeed = 2f;

    override protected void Program()
    {
        this.Log("기동 조건 충족. AGV 커버 조립 VcumRobot 시퀀스 시작!");

        // ================= [ 0번: 원점 출발 ] =================
        this.Log("Home 포인트로 이동 시작");
        this.LIN(_targets[0], defaultSpeed);
        this.Wait(500);

        // ================= [ 메인 작업: 1 -> 2 -> 4 ] =================
        this.Log("경유지점 1번 이동");
        this.LIN(_targets[1], defaultSpeed);

        // -------- 2번: 커버 픽업 --------
        this.Log("Z축 상승/하강 작업 (픽업 이동)");
        this.Offset(_targets[2], new Vector3(0, -400f, 0f), defaultSpeed);
        this.Log("Y축 하강/상승 작업 (픽업 완료)");
        this.Offset(_targets[2], new Vector3(0, 0f, 0f), defaultSpeed);
        this.Offset(_targets[2], new Vector3(-300f, -400f, 0), defaultSpeed);

        // -------- 4번: AGV 위 배치 --------
        this.Log("Z축 상승/하강 작업 (배치 이동)");
        this.Offset(_targets[3], new Vector3(0f, -400f, 0), defaultSpeed);
        this.Log("Y축 하강/상승 작업 (배치 완료)");
        this.Offset(_targets[3], new Vector3(0, 20f, 0f), defaultSpeed);
        this.Offset(_targets[3], new Vector3(0, -400f, 0), defaultSpeed);

        // ================= [ 0번: 원점 복귀 ] =================
        this.Log("해당 AGV 작업 완료. 대기를 위해 Home 포인트로 이동 시작");
        this.LIN(_targets[0], defaultSpeed);
        this.Wait(500);

        this.Log("로봇 시퀀스 종료");

        this.DoAction(() =>
        {
            // 작업 완료 신호를 커넥터로 전달
            if (connector != null)
            {
                connector.OnCycleCompleted();
            }
            Debug.Log("[VcumRobot] 공통 작업 완료 및 원점 복귀. PLC로 완료 신호 전송.");
        });
    }
}