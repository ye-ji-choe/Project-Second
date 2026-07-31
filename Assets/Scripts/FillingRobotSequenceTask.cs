using UnityEngine;
using System.Collections.Generic;

public class FillingRobotSequenceTask : Task
{
    [Header("티칭 포인트 설정 (0번: Home, 1~24번: 작업점)")]
    [SerializeField] private List<Target> _targets;

    [Header("커넥터 연결")]
    // 💡 주의: 현재 사용하시는 커넥터 이름에 맞춰 타입을 수정해주세요. 
    // (예: RobotConnector 또는 FillingRobotConnector)
    public FillingRobotConnector connector;

    public float defaultSpeed = 2f;

    override protected void Program()
    {
        this.Log("기동 조건 충족. 1~24번 연속 충진(Filling) 작업 시퀀스 시작!");

        this.Log("Home 포인트로 이동 시작");
        this.LIN(_targets[0], defaultSpeed);
        this.Wait(500);

        // ================= [ 1번 지점 작업 ] =================
        this.Log("작업 위치 1번 이동");
        this.LIN(_targets[1], defaultSpeed);

        // ================= [ 2번 지점 작업 ] =================
        this.Log("작업 위치 2번 이동");
        this.LIN(_targets[2], defaultSpeed);

        // ================= [ 3번 지점 작업 ] =================
        this.Log("작업 위치 3번 이동");
        this.LIN(_targets[3], defaultSpeed);

        // ================= [ 4번 지점 작업 ] =================
        this.Log("작업 위치 4번 이동");
        this.LIN(_targets[4], defaultSpeed);

        // ================= [ 5번 지점 작업 ] =================
        this.Log("작업 위치 5번 이동");
        this.LIN(_targets[5], defaultSpeed);

        // ================= [ 6번 지점 작업 ] =================
        this.Log("작업 위치 6번 이동");
        this.LIN(_targets[6], defaultSpeed);

        // ================= [ 7번 지점 작업 ] =================
        this.Log("작업 위치 7번 이동");
        this.LIN(_targets[7], defaultSpeed);

        // ================= [ 8번 지점 작업 ] =================
        this.Log("작업 위치 8번 이동");
        this.LIN(_targets[8], defaultSpeed);

        // ================= [ 9번 지점 작업 ] =================
        this.Log("작업 위치 9번 이동");
        this.LIN(_targets[9], defaultSpeed);

        // ================= [ 10번 지점 작업 ] =================
        this.Log("작업 위치 10번 이동");
        this.LIN(_targets[10], defaultSpeed);

        // ================= [ 11번 지점 작업 ] =================
        this.Log("작업 위치 11번 이동");
        this.LIN(_targets[11], defaultSpeed);

        // ================= [ 12번 지점 작업 ] =================
        this.Log("작업 위치 12번 이동");
        this.LIN(_targets[12], defaultSpeed);

        // ================= [ 13번 지점 작업 ] =================
        this.Log("작업 위치 13번 이동");
        this.LIN(_targets[13], defaultSpeed);

        // ================= [ 14번 지점 작업 ] =================
        this.Log("작업 위치 14번 이동");
        this.LIN(_targets[14], defaultSpeed);

        // ================= [ 15번 지점 작업 ] =================
        this.Log("작업 위치 15번 이동");
        this.LIN(_targets[15], defaultSpeed);

        // ================= [ 16번 지점 작업 ] =================
        this.Log("작업 위치 16번 이동");
        this.LIN(_targets[16], defaultSpeed);

        // ================= [ 17번 지점 작업 ] =================
        this.Log("작업 위치 17번 이동");
        this.LIN(_targets[17], defaultSpeed);

        // ================= [ 18번 지점 작업 ] =================
        this.Log("작업 위치 18번 이동");
        this.LIN(_targets[18], defaultSpeed);

        // ================= [ 19번 지점 작업 ] =================
        this.Log("작업 위치 19번 이동");
        this.LIN(_targets[19], defaultSpeed);

        // ================= [ 20번 지점 작업 ] =================
        this.Log("작업 위치 20번 이동");
        this.LIN(_targets[20], defaultSpeed);

        // ================= [ 21번 지점 작업 ] =================
        this.Log("작업 위치 21번 이동");
        this.LIN(_targets[21], defaultSpeed);

        // ================= [ 22번 지점 작업 ] =================
        this.Log("작업 위치 22번 이동");
        this.LIN(_targets[22], defaultSpeed);

        // ================= [ 23번 지점 작업 ] =================
        this.Log("작업 위치 23번 이동");
        this.LIN(_targets[23], defaultSpeed);

        // ================= [ 24번 지점 작업 ] =================
        this.Log("작업 위치 24번 이동");
        this.LIN(_targets[24], defaultSpeed);

        // =======================================================

        this.Log("Home 포인트로 복귀 시작");
        this.LIN(_targets[0], defaultSpeed);
        this.Wait(500);

        this.Log("로봇 시퀀스 종료");

        this.DoAction(() =>
        {
            // 작업 완료 신호를 커넥터로 전달 (필요 시 null 체크 추가)
            if (connector != null)
            {
                connector.OnCycleCompleted();
            }
            Debug.Log("[로봇] 충진(Filling) 작업 1~24번 완료. PLC로 완료 신호 전송.");
        });
    }
}