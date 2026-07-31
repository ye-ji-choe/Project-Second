using UnityEngine;
using System.Collections.Generic;

public class ScanRobotSequenceTask : Task
{
    [Header("티칭 포인트 설정 (0:원점, 1~8:스캔 작업 지점)")]
    [SerializeField] private List<Target> _targets;

    [Header("커넥터 연결")]
    // 💡 연결될 커넥터 스크립트 이름에 맞춰 타입을 지정하세요. (예: ScanRobotConnector)
    public ScanRobotConnector connector;

    public float defaultSpeed = 2f;

    override protected void Program()
    {
        this.Log("기동 조건 충족. 스캔(ScanRobot) 시퀀스 시작!");

        // ================= [ 0번: 원점 출발 ] =================
        this.Log("Home 포인트(0번)로 이동 시작");
        this.LIN(_targets[0], defaultSpeed);

        // ================= [ 1~8번: 스캔 지점 순차 이동 ] =================
        this.Log("스캔 작업 위치 1번 이동");
        this.LIN(_targets[1], defaultSpeed);

        this.Log("스캔 작업 위치 2번 이동");
        this.LIN(_targets[2], defaultSpeed);

        this.Log("스캔 작업 위치 3번 이동");
        this.LIN(_targets[3], defaultSpeed);

        this.Log("스캔 작업 위치 4번 이동");
        this.LIN(_targets[4], defaultSpeed);

        this.Log("스캔 작업 위치 5번 이동");
        this.LIN(_targets[5], defaultSpeed);

        this.Log("스캔 작업 위치 6번 이동");
        this.LIN(_targets[6], defaultSpeed);

        this.Log("스캔 작업 위치 7번 이동");
        this.LIN(_targets[7], defaultSpeed);

        this.Log("스캔 작업 위치 8번 이동");
        this.LIN(_targets[8], defaultSpeed);

        // ================= [ 0번: 원점 복귀 ] =================
        this.Log("스캔 작업 완료. Home 포인트(0번)로 복귀 시작");
        this.LIN(_targets[0], defaultSpeed);

        this.Log("로봇 시퀀스 종료");

        this.DoAction(() =>
        {
            // 작업 완료 신호를 커넥터로 전달 (null 체크 포함)
            if (connector != null)
            {
                connector.OnCycleCompleted();
            }
            Debug.Log("[ScanRobot] 스캔 작업 완료 및 원점 복귀. PLC로 완료 신호 전송.");
        });
    }
}