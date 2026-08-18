using UnityEngine;
using System.Collections.Generic;

public class HousingRobotSequenceTask : Task
{
    [Header("티칭 포인트 설정")]
    [SerializeField] private List<Target> _targets;

    [Header("커넥터 연결")]
    public HousingRobotConnector connector;

    public float defaultSpeed = 2f;

    // 현재 진입한 AGV가 1번인지 2번인지 추적 (true = 1번 AGV, false = 2번 AGV)
    private bool isFirstAGVTurn = true;

    override protected void Program()
    {
        this.Log($"기동 조건 충족. {(isFirstAGVTurn ? "1번" : "2번")} AGV 로봇 시퀀스 시작!");

        this.Log("Home 포인트로 이동 시작");
        this.LIN(_targets[0], defaultSpeed);
        this.Wait(500);

        if (isFirstAGVTurn)
        {
            // ================= [ 1번 AGV 작업: 1번, 2번 배터리 ] =================
            this.Log("경유지점 1번 이동");
            this.LIN(_targets[1], defaultSpeed);

            // -------- [1번 배터리 (1 -> 3 -> 4)] --------
            this.Log("Z축 40mm 상승 작업");
            this.Offset(_targets[3], new Vector3(0, -400f, 0f), defaultSpeed);
            this.Log("Z축 40mm 하강 작업");
            this.Offset(_targets[3], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Log("Z축 40mm 상승 작업");
            this.Offset(_targets[3], new Vector3(0, -400f, 0f), defaultSpeed);

            this.Log("Z축 30mm 상승 작업");
            this.Offset(_targets[4], new Vector3(0, -300f, 0f), defaultSpeed);
            this.Log("Z축 60mm 하강 작업");
            this.Offset(_targets[4], new Vector3(0, 20f, 0f), defaultSpeed);
            this.Log("Z축 60mm 상승 작업");
            this.Offset(_targets[4], new Vector3(0, -300f, 0f), defaultSpeed);

            this.Log("경유지점 1번 복귀");
            this.LIN(_targets[1], defaultSpeed);

            // -------- [2번 배터리 (1 -> 5 -> 6)] --------
            this.Log("Z축 40mm 상승 작업");
            this.Offset(_targets[5], new Vector3(0, -400f, 0f), defaultSpeed);
            this.Log("Z축 40mm 하강 작업");
            this.Offset(_targets[5], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Log("그랩 안정화 대기 (2초)");
            this.Wait(1000);
            this.Log("Z축 40mm 상승 작업");
            this.Offset(_targets[5], new Vector3(0, -400f, 0f), defaultSpeed);

            // 💡 만약 이전 공정처럼 5번에서 6번으로 이동할 때 구조물에 걸려 충돌이 발생한다면
            // 이곳에 this.LIN(_targets[1], defaultSpeed); 를 추가하여 경유지를 거쳐가도록 하세요.

            this.Log("Z축 30mm 상승 작업");
            this.Offset(_targets[6], new Vector3(0, -300f, 0f), defaultSpeed);
            this.Log("Z축 60mm 하강 작업");
            this.Offset(_targets[6], new Vector3(0, 20f, 0f), defaultSpeed);
            this.Log("Z축 60mm 상승 작업");
            this.Offset(_targets[6], new Vector3(0, -300f, 0f), defaultSpeed);
        }
        else
        {
            // ================= [ 2번 AGV 작업: 3번, 4번 배터리 ] =================
            this.Log("경유지점 2번 이동");
            this.LIN(_targets[2], defaultSpeed);

            // -------- [3번 배터리 (2 -> 7 -> 4)] --------
            this.Log("Z축 40mm 상승 작업");
            this.Offset(_targets[7], new Vector3(0, -400f, 0f), defaultSpeed);
            this.Log("Z축 40mm 하강 작업");
            this.Offset(_targets[7], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Log("Z축 40mm 상승 작업");
            this.Offset(_targets[7], new Vector3(0, -400f, 0f), defaultSpeed);

            this.Log("Z축 30mm 상승 작업");
            this.Offset(_targets[4], new Vector3(0, -300f, 0f), defaultSpeed);
            this.Log("Z축 60mm 하강 작업");
            this.Offset(_targets[4], new Vector3(0, 20f, 0f), defaultSpeed);
            this.Log("Z축 60mm 상승 작업");
            this.Offset(_targets[4], new Vector3(0, -300f, 0f), defaultSpeed);

            this.Log("경유지점 2번 복귀");
            this.LIN(_targets[2], defaultSpeed);

            // -------- [4번 배터리 (2 -> 8 -> 6)] --------
            this.Log("Z축 40mm 상승 작업");
            this.Offset(_targets[8], new Vector3(0, -400f, 0f), defaultSpeed);
            this.Log("Z축 40mm 하강 작업");
            this.Offset(_targets[8], new Vector3(0, 0f, 0f), defaultSpeed);
            this.Log("Z축 40mm 상승 작업");
            this.Offset(_targets[8], new Vector3(0, -400f, 0f), defaultSpeed);

            // 💡 여기도 마찬가지로 8번에서 6번으로 갈 때 물리적 충돌이 생긴다면
            // this.LIN(_targets[2], defaultSpeed); 를 추가하여 우회하도록 설정해 주세요.

            this.Log("Z축 30mm 상승 작업");
            this.Offset(_targets[6], new Vector3(0, -300f, 0f), defaultSpeed);
            this.Log("Z축 60mm 하강 작업");
            this.Offset(_targets[6], new Vector3(0, 20f, 0f), defaultSpeed);
            this.Log("Z축 60mm 상승 작업");
            this.Offset(_targets[6], new Vector3(0, -300f, 0f), defaultSpeed);
        }

        // ================= [ 작업 종료 후 원점 복귀 ] =================
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
            Debug.Log("[로봇] 작업 완료 및 원점 대기. PLC로 완료 신호 전송.");
        });
    }
}