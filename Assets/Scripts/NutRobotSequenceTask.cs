using UnityEngine;
using System.Collections.Generic;

public class NutRobotSequenceTask : Task
{
    [Header("티칭 포인트 설정 (0:원점, 1:경유, 2:공급, 3~6:체결지점)")]
    [SerializeField] private List<Target> _targets;

    [Header("커넥터 연결")]
    // 💡 연결될 Connector 스크립트에 맞춰서 타입을 지정해 두었습니다.
    public NutRobotConnector connector;

    public float defaultSpeed = 2f;

    override protected void Program()
    {
        this.Log("기동 조건 충족. 볼트 체결(NutRobot) 시퀀스 시작!");

        // ================= [ 0번: 원점 출발 ] =================
        this.Log("Home 포인트로 이동 시작 (PLC 딜레이 의존)");
        this.LIN(_targets[0], defaultSpeed);

        // ================= [ 1번: 경유지점 이동 ] =================
        this.Log("경유지점 1번 이동");
        this.LIN(_targets[1], defaultSpeed);

        // ================= [ 2번 -> 3번 작업 ] =================

        this.Offset(_targets[2], new Vector3(-300f, 0, 0), defaultSpeed);
        this.Offset(_targets[2], new Vector3(0, 0f, 0f), 1f);
        this.Offset(_targets[2], new Vector3(-300f, 0, 0), 1f);

        this.Log("작업 위치 3번 이동 (첫 번째 볼트 체결)");
        this.Offset(_targets[3], new Vector3(-300f, 0, 0), defaultSpeed);
        this.Offset(_targets[3], new Vector3(0, 0f, 0f), 1f);
        this.Offset(_targets[3], new Vector3(-300f, 0, 0), 1f);

        // ================= [ 2번 -> 4번 작업 ] =================
        this.Log("작업 위치 2번 이동 (볼트 픽업)");
        this.Offset(_targets[2], new Vector3(-300f, 0, 0), defaultSpeed);
        this.Offset(_targets[2], new Vector3(0, 0f, 0f), 1f);
        this.Offset(_targets[2], new Vector3(-300f, 0, 0), 1f);

        this.Log("작업 위치 4번 이동 (두 번째 볼트 체결)");
        this.Offset(_targets[4], new Vector3(-300f, 0, 0), defaultSpeed);
        this.Offset(_targets[4], new Vector3(0, 0f, 0f), 1f);
        this.Offset(_targets[4], new Vector3(-300f, 0, 0), 1f);

        // ================= [ 2번 -> 5번 작업 ] =================
        this.Log("작업 위치 2번 이동 (볼트 픽업)");
        this.Offset(_targets[2], new Vector3(-300f, 0, 0), defaultSpeed);
        this.Offset(_targets[2], new Vector3(0, 0f, 0f), 1f);
        this.Offset(_targets[2], new Vector3(-300f, 0, 0), 1f);

        this.Log("작업 위치 5번 이동 (세 번째 볼트 체결)");
        this.Offset(_targets[5], new Vector3(-300f, 0, 0), defaultSpeed);
        this.Offset(_targets[5], new Vector3(0, 0f, 0f), 1f);
        this.Offset(_targets[5], new Vector3(-300f, 0, 0), 1f);

        // ================= [ 2번 -> 6번 작업 ] =================
        this.Log("작업 위치 2번 이동 (볼트 픽업)");
        this.Offset(_targets[2], new Vector3(-300f, 0, 0), defaultSpeed);
        this.Offset(_targets[2], new Vector3(0, 0f, 0f), 1f);
        this.Offset(_targets[2], new Vector3(-300f, 0, 0), 1f);

        this.Log("작업 위치 6번 이동 (네 번째 볼트 체결)");
        this.Offset(_targets[6], new Vector3(-300f, 0, 0), defaultSpeed);
        this.Offset(_targets[6], new Vector3(0, 0f, 0f), 1f);
        this.Offset(_targets[6], new Vector3(-300f, 0, 0), 1f);

        // ================= [ 0번: 원점 복귀 ] =================
        this.Log("작업 완료. Home 포인트로 복귀 시작");
        this.LIN(_targets[0], defaultSpeed);

        this.Log("로봇 시퀀스 종료");

        this.DoAction(() =>
        {
            // 작업 완료 신호를 커넥터로 전달 (null 체크 포함)
            if (connector != null)
            {
                connector.OnCycleCompleted();
            }
            Debug.Log("[NutRobot] 작업 완료 및 원점 복귀. PLC로 완료 신호 전송.");
        });
    }
}