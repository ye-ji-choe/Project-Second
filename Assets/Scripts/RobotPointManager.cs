using UnityEngine;
using System.Collections.Generic;
using Preliy.Flange; // Flange 패키지 네임스페이스

// 포인트별 좌표 데이터를 저장하기 위한 직렬화 구조체
[System.Serializable]
public struct PointData
{
    [Tooltip("PLC D0 값과 매치되는 포인트 번호")]
    public int pointNumber;

    [Tooltip("목표 위치 (X, Y, Z)")]
    public Vector3 position;

    [Tooltip("목표 회전 자세 (Rx, Ry, Rz)")]
    public Vector3 rotation;
}

public class RobotPointManager : MonoBehaviour
{
    [Header("Robotics Controller")]
    public Controller flangeController;      // Preliy.Flange.Controller 참조

    [Header("PLC Connection")]
    public string d0Address = "D0";          // 목적지 포인트를 수신할 PLC 주소 (명령 레지스터)

    [Header("Point Teaching Table")]
    public List<PointData> pointTable = new List<PointData>(); // 좌표값 리스트

    private int currentPointNumber = -1;

    private void Start()
    {
        // PLC의 D0 워드 영역 데이터를 실시간 모니터링
        if (!string.IsNullOrEmpty(d0Address))
        {
            MXRequester.Get.AddDeviceAddress(d0Address, OnReceivePointNumber);
        }
    }

    /// <summary>
    /// PLC로부터 D0 값을 읽어올 때마다 호출되는 콜백 함수
    /// </summary>
    private void OnReceivePointNumber(short data)
    {
        // PLC로부터 수신된 값이 변경되었을 때만 트리거 (Rising Edge 역할, 불필요한 연산 방지)
        if (currentPointNumber != data)
        {
            currentPointNumber = data;
            ExecuteMoveCommand(currentPointNumber);
        }
    }

    /// <summary>
    /// 포인트 번호에 해당하는 좌표를 찾아 로봇 제어기에 직접 명령을 내리는 함수
    /// </summary>
    public void ExecuteMoveCommand(int pointNum)
    {
        // 1. 포인트 테이블에서 번호와 일치하는 좌표 데이터 검색
        int index = pointTable.FindIndex(p => p.pointNumber == pointNum);

        // 데이터가 없을 경우의 예외 처리
        if (index == -1)
        {
            Debug.LogWarning($"[RobotPointManager] PLC 명령 오류: 테이블에 정의되지 않은 포인트입니다. D0 = {pointNum}");
            return;
        }

        PointData targetData = pointTable[index];

        // 2. 가상 객체 추종 방식(ikTargetObject) 제거. 
        //    PLC에서 지시한 목표 절대 좌표를 Flange 제어기에 직접 인가.
        if (flangeController != null && flangeController.IsValid.Value)
        {
            Quaternion targetRotation = Quaternion.Euler(targetData.rotation);

            // 주의: 아래 주석 처리된 부분은 Flange 패키지의 실제 API에 맞게 수정하여 사용하십시오.
            // (예: Solver에 좌표를 직접 넣고 Solve 명령을 호출하는 방식)

            // flangeController.Solver.TargetPosition = targetData.position;
            // flangeController.Solver.TargetRotation = targetRotation;
            // flangeController.Solver.Solve(); 

            Debug.Log($"[RobotPointManager] PLC 명령 수신: 포인트 {pointNum} (X:{targetData.position.x}, Y:{targetData.position.y}) 이동 지시 완료.");
        }
        else
        {
            Debug.LogError("[RobotPointManager] 제어기(Flange Controller)가 할당되지 않았거나 유효하지 않습니다.");
        }
    }
}