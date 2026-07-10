using UnityEngine;
using System.Collections.Generic;

public class RobotSequenceTask : Task
{
    [Header("티칭 포인트 설정")]
    [SerializeField] private List<Target> _targets;

    override protected void Program()
    {
        // 이제 이 로그들은 명령 큐에 순서대로 들어가서
        // 실제 로봇이 움직일 때 하나씩 실행됩니다.
        this.Log("로봇 시퀀스 시작");

        this.Log("Home 포인트로 이동 시작");
        this.PTP(_targets[0]);
        this.Wait(500);

        this.Log("작업 위치 1번 이동");
        this.PTP(_targets[1]);

        this.Log("작업 위치 2번 정밀 이동");
        this.LIN(_targets[2]);





        this.Log("Z축 5mm 상승 작업");
        var upMove = new LIN(new Vector3(0, 0, 0.05f), Quaternion.Euler(0,0,0));
        upMove.Speed(0.1f);
        upMove.RelTool();
        this.Move(upMove);



        this.Move(upMove);

        this.Log("Home으로 복귀");
        this.PTP(_targets[0]);

        this.Log("로봇 시퀀스 종료");
    }
}