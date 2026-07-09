using UnityEngine;
using System.Collections.Generic;
using System;

public class PressRobotConnector : MXObject
{
    public PressRobotController controller; // 프레스 설비를 제어할 가상의 컨트롤러

    [Header("PLC Addresses (프레스_제10공정_프레스)")]
    // --- Unity가 PLC로 보내는 신호 (Read Only for PLC) ---
    public DeviceAddress busyAddress = new DeviceAddress("BUSY 신호"); // X17A
    public DeviceAddress pressReadyAddress = new DeviceAddress("프레스 READY"); // X17B
    public DeviceAddress cycleCompleteAddress = new DeviceAddress("사이클 작업 완료"); // X17E
    public DeviceAddress homePositionAddress = new DeviceAddress("Home Position"); // X17F

    // --- PLC가 Unity로 보내는 신호 (Write Only for PLC) ---
    public DeviceAddress startOperationAddress = new DeviceAddress("기동 시작"); // Y19D

    public float feedbackTime = 0.3f;

    private bool haveToExecute = false;
    private bool completedCycle = false;
    private float remainFeedbackTime;

    private void Start()
    {
        // PLC로부터 읽어올 데이터 구독 (기동 시작)
        if (startOperationAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(startOperationAddress.address, StartOperation);
    }

    private void Update()
    {
        // 기동 시작(Y19D) 신호 처리
        if (haveToExecute)
        {
            if (controller != null)
            {
                // 컨트롤러에 프레스 압착/성형 사이클 시작 명령
                controller.StartCycle();
            }

            // 작업 시작과 동시에 BUSY 신호(X17A) ON
            if (busyAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(busyAddress.address, 1);

            haveToExecute = false;
        }

        // 사이클 완료(X17E) 신호를 보낸 후 일정 시간이 지나면 신호 OFF
        if (completedCycle && remainFeedbackTime < Time.time)
        {
            completedCycle = false;
            if (cycleCompleteAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(cycleCompleteAddress.address, 0);
        }
    }

    // --- PLC에서 Unity로 들어오는 신호 처리 (Read) ---
    private void StartOperation(short data)
    {
        // Y19D: 기동 시작
        haveToExecute = data != 0;
    }

    // --- Unity에서 PLC로 보내는 신호 처리 (Write) ---

    /// <summary>
    /// 프레스 사이클 작업이 완료되었을 때 컨트롤러에서 호출해주는 함수
    /// </summary>
    public void OnCycleCompleted()
    {
        // 사이클 작업 완료 신호(X17E) ON
        if (cycleCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(cycleCompleteAddress.address, 1);

        // BUSY 신호(X17A) OFF
        if (busyAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(busyAddress.address, 0);

        completedCycle = true;
        remainFeedbackTime = Time.time + feedbackTime;
    }

    /// <summary>
    /// 프레스의 현재 상태(Ready, Home)를 갱신할 때 사용하는 함수
    /// </summary>
    public void UpdatePressStatus(bool isReady, bool isHome)
    {
        // 프레스 READY 신호(X17B)
        if (pressReadyAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(pressReadyAddress.address, (short)(isReady ? 1 : 0));

        // Home Position 신호(X17F)
        if (homePositionAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(homePositionAddress.address, (short)(isHome ? 1 : 0));
    }
}