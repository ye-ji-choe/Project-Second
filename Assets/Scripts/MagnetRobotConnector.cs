using UnityEngine;
using System.Collections.Generic;
using System;

public class MagnetRobotConnector : MXObject
{
    public MagnetRobotController controller; // 하우징 공정 로봇을 제어할 가상의 컨트롤러

    [Header("PLC Addresses (로봇1_제1공정_하우징)")]
    // --- Unity가 PLC로 보내는 신호 (Read Only for PLC) ---
    public DeviceAddress busyAddress = new DeviceAddress("BUSY 신호"); // X80
    public DeviceAddress robotReadyAddress = new DeviceAddress("ROBOT READY"); // X81
    public DeviceAddress cycleCompleteAddress = new DeviceAddress("사이클 작업 완료"); // X84
    public DeviceAddress homePositionAddress = new DeviceAddress("Home Position"); // X88
    public DeviceAddress areaSensorAddress = new DeviceAddress("Area Sensor"); // X89

    // --- PLC가 Unity로 보내는 신호 (Write Only for PLC) ---
    public DeviceAddress startOperationAddress = new DeviceAddress("기동 시작"); // Y100
    public DeviceAddress motorOnAddress = new DeviceAddress("Motor ON"); // Y101
    public DeviceAddress motorOffAddress = new DeviceAddress("Motor OFF"); // Y102

    public float feedbackTime = 0.3f;

    private bool haveToExecute = false;
    private bool completedCycle = false;
    private float remainFeedbackTime;

    private void Start()
    {
        // PLC로부터 읽어올 데이터 구독 (기동 시작, 모터 ON/OFF)
        if (startOperationAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(startOperationAddress.address, StartOperation);

        if (motorOnAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(motorOnAddress.address, MotorOn);

        if (motorOffAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(motorOffAddress.address, MotorOff);
    }

    private void Update()
    {
        // 기동 시작(Y100) 신호 처리
        if (haveToExecute)
        {
            if (controller != null)
            {
                // 컨트롤러에 로봇 사이클 시작 명령
                controller.StartCycle();
            }

            // 작업 시작과 동시에 BUSY 신호(X80) ON
            if (busyAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(busyAddress.address, 1);

            haveToExecute = false;
        }

        // 사이클 완료(X84) 신호를 보낸 후 일정 시간이 지나면 신호 OFF
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
        // Y100: 기동 시작
        haveToExecute = data != 0;
    }

    private void MotorOn(short data)
    {
        // Y101: Motor ON
        if (data != 0 && controller != null)
            controller.SetMotorState(true);
    }

    private void MotorOff(short data)
    {
        // Y102: Motor OFF
        if (data != 0 && controller != null)
            controller.SetMotorState(false);
    }

    // --- Unity에서 PLC로 보내는 신호 처리 (Write) ---

    /// <summary>
    /// 로봇의 하우징 공정 사이클이 완료되었을 때 컨트롤러에서 호출해주는 함수
    /// </summary>
    public void OnCycleCompleted()
    {
        // 사이클 작업 완료 신호(X84) ON
        if (cycleCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(cycleCompleteAddress.address, 1);

        // BUSY 신호(X80) OFF
        if (busyAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(busyAddress.address, 0);

        completedCycle = true;
        remainFeedbackTime = Time.time + feedbackTime;
    }

    /// <summary>
    /// 로봇의 현재 상태(Ready, Home, Sensor 등)를 갱신할 때 사용하는 함수
    /// </summary>
    public void UpdateRobotStatus(bool isReady, bool isHome, bool isSensorTriggered)
    {
        // ROBOT READY 신호(X81)
        if (robotReadyAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(robotReadyAddress.address, (short)(isReady ? 1 : 0));

        // Home Position 신호(X88)
        if (homePositionAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(homePositionAddress.address, (short)(isHome ? 1 : 0));

        // Area Sensor 신호(X89)
        if (areaSensorAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(areaSensorAddress.address, (short)(isSensorTriggered ? 1 : 0));
    }
}