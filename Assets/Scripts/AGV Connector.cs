using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Hierarchy;

public class AGVConnector : MXObject
{
    public AGVController controller; // AGV 제어를 담당할 컨트롤러 (별도 구현 필요)

    [Header("PLC Addresses from Image")]
    // --- 이미지에 명시된 PLC 주소들 ---
    public DeviceAddress busyAddress = new DeviceAddress("BUSY 신호"); // X200
    public DeviceAddress startOperationAddress = new DeviceAddress("기동 시작"); // Y225
    public DeviceAddress destinationAddress = new DeviceAddress("목적지 설비 번호"); // D00
    public DeviceAddress homePositionAddress = new DeviceAddress("Home Position"); // X206
    public DeviceAddress arrivalCompleteAddress = new DeviceAddress("도착 완료"); // X207

    public float feedbackTime = 0.3f;

    private bool haveToExecute = false;
    private int currentDestination = 0;

    private bool completedArrival = false;
    private float remainFeedbackTime;

    private void Start()
    {
        // PLC로부터 읽어올 데이터 (기동 시작, 목적지 번호)
        if (startOperationAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(startOperationAddress.address, StartOperation);

        if (destinationAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(destinationAddress.address, SetDestination);
    }

    private void Update()
    {
        // 기동 시작 신호(Y225)를 받았을 때의 처리
        if (haveToExecute)
        {
            if (controller != null)
            {
                // 컨트롤러에 목적지(D00) 전달 및 이동 명령
                controller.MoveToDestination(currentDestination);
            }

            // BUSY 신호(X200) ON
            if (busyAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(busyAddress.address, 1);

            haveToExecute = false;
        }

        // 도착 완료(X207) 신호를 보낸 후 일정 시간이 지나면 신호 OFF
        if (completedArrival && remainFeedbackTime < Time.time)
        {
            completedArrival = false;
            if (arrivalCompleteAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(arrivalCompleteAddress.address, 0);
        }
    }

    // --- PLC에서 Unity로 들어오는 신호 처리 (Read) ---
    private void SetDestination(short data)
    {
        // D00: 목적지 설비 번호 (0~15)
        currentDestination = data;
    }

    private void StartOperation(short data)
    {
        // Y225: 기동 시작
        haveToExecute = data != 0;
    }

    // --- Unity에서 PLC로 보내는 신호 처리 (Write) ---

    /// <summary>
    /// AGV가 목적지에 도착했을 때 컨트롤러에서 호출해주는 함수
    /// </summary>
    public void OnArrivalCompleted()
    {
        // 도착 완료 신호(X207) ON
        if (arrivalCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(arrivalCompleteAddress.address, 1);

        // BUSY 신호(X200) OFF
        if (busyAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(busyAddress.address, 0);

        completedArrival = true;
        remainFeedbackTime = Time.time + feedbackTime;
    }

    /// <summary>
    /// AGV가 Home Position에 있는지 여부를 갱신할 때 사용하는 함수
    /// </summary>
    public void UpdateHomePosition(bool isHome)
    {
        // Home Position 신호(X206) 업데이트
        if (homePositionAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(homePositionAddress.address, (short)(isHome ? 1 : 0));
    }
}