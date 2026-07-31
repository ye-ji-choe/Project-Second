using UnityEngine;
using System.Collections.Generic;

public class ShoulderGlueRobotSequenceTask : Task
{
    [Header("티칭 포인트 설정 (0번: Home, 1~5번: 작업/경유점)")]
    [SerializeField] private List<Target> _targets;

    [Header("커넥터 연결")]
    // 💡 연결할 커넥터 스크립트 이름에 맞춰 타입을 수정해 주세요. 
    public ShoulderGlueRobotConnector connector;

    public float defaultSpeed = 2f;

    override protected void Program()
    {
        this.Log("기동 조건 충족. 숄더 글루(Shoulder Glue) 로봇 시퀀스 시작!");

        // ================= [ 0번: 원점 출발 ] =================
        this.Log("Home 포인트로 이동 시작");
        this.LIN(_targets[0], defaultSpeed);
        this.Wait(500);

        // ================= [ 1번: 첫 번째 경유 ] =================
        this.Log("경유지점 1번 이동");
        this.LIN(_targets[1], defaultSpeed);

        // ================= [ 2번: 작업 진행 ] =================
        this.Log("작업 위치 2번 이동");
        this.LIN(_targets[2], defaultSpeed);

        // ================= [ 3번: 작업 진행 ] =================
        this.Log("작업 위치 3번 이동");
        this.LIN(_targets[3], defaultSpeed);

        // ================= [ 1번: 두 번째 경유 (우회) ] =================
        this.Log("경유지점 1번 다시 이동 (충돌 방지 우회)");
        this.LIN(_targets[1], defaultSpeed);

        // ================= [ 4번: 작업 진행 ] =================
        this.Log("작업 위치 4번 이동");
        this.LIN(_targets[4], defaultSpeed);

        // ================= [ 5번: 작업 진행 ] =================
        this.Log("작업 위치 5번 이동");
        this.LIN(_targets[5], defaultSpeed);

        // ================= [ 0번: 원점 복귀 ] =================
        this.Log("작업 완료. Home 포인트로 복귀 시작");
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
            Debug.Log("[로봇] 숄더 글루(Shoulder Glue) 작업 완료. PLC로 완료 신호 전송.");
        });
    }
}