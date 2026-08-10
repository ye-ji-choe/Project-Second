using UnityEngine;
using System.Collections.Generic;

public class ScanRobotSequenceTask : Task
{
    [Header("티칭 포인트 설정 (0:원점, 1~8:스캔 작업 지점)")]
    [SerializeField] private List<Target> _targets;

    [Header("커넥터 연결")]
    public ScanRobotConnector connector;

    [Header("시퀀스 설정")]
    public float defaultSpeed = 2f;

    // 💡 양불 판정 테스트를 위한 랜덤 확률 설정 (기본값 80% 양품)
    [Header("시뮬레이션 판정 설정")]
    [Range(0, 100)]
    [Tooltip("스캔 완료 시 PASS가 뜰 확률 (0~100%)")]
    public int passProbability = 80;

    override protected void Program()
    {
        this.Log("기동 조건 충족. 스캔(ScanRobot) 시퀀스 시작!");

        // ================= [ 0번: 원점 출발 ] =================
        this.Log("Home 포인트(0번)로 이동 시작");
        this.LIN(_targets[0], defaultSpeed);

        // ================= [ 1~8번: 스캔 지점 순차 이동 ] =================
        for (int i = 1; i <= 8; i++)
        {
            this.Log($"스캔 작업 위치 {i}번 이동");
            this.LIN(_targets[i], defaultSpeed);
        }

        // ================= [ 0번: 원점 복귀 ] =================
        this.Log("스캔 작업 완료. Home 포인트(0번)로 복귀 시작");
        this.LIN(_targets[0], defaultSpeed);

        this.Log("로봇 시퀀스 종료 대기 중...");

        // ================= [ 랜덤 판정 신호 및 완료 신호 전송 ] =================
        this.DoAction(() =>
        {
            if (connector != null)
            {
                // 1. 0 ~ 99 사이의 난수를 발생시켜 지정된 확률과 비교
                int randomValue = UnityEngine.Random.Range(0, 100);
                bool isScanPass = (randomValue < passProbability);

                // 2. 결과 판정 신호 전송 (M1166 / M1167)
                if (isScanPass)
                {
                    connector.SendPassSignal(1); // Pass ON
                    connector.SendNGSignal(0);   // NG OFF (인터록 방지)
                    Debug.Log($"[ScanRobot] 스캔 판정 PASS (확률: {passProbability}%, 난수: {randomValue}) -> M1166 전송.");
                }
                else
                {
                    connector.SendPassSignal(0); // Pass OFF
                    connector.SendNGSignal(1);   // NG ON (인터록 방지)
                    Debug.Log($"[ScanRobot] 스캔 판정 NG (확률: {passProbability}%, 난수: {randomValue}) -> M1167 전송.");
                }

                // 3. 사이클 완료 신호 전송
                connector.OnCycleCompleted();
                Debug.Log("[ScanRobot] 사이클 완료 신호 전송 됨.");
            }
            else
            {
                Debug.LogWarning("[ScanRobot] Connector가 연결되지 않아 판정 신호를 보낼 수 없습니다.");
            }
        });
    }
}