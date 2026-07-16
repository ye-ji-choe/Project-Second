using UnityEngine;
using System.Collections.Generic;
using System;

public class PressConnector : MXObject
{
    public PressController controller;

    [Header("Write Addresses (Unity -> PLC 송신용)")]
    public DeviceAddress PressReady = new DeviceAddress("프레스 READY (M89)"); // M89
    public DeviceAddress laserSensorAddress = new DeviceAddress("레이저 센서 인식");
    public DeviceAddress pressCompleteAddress = new DeviceAddress("프레스 완료");

    [Header("Read Addresses (PLC -> Unity 수신용)")]
    public DeviceAddress UpCommand = new DeviceAddress("기동 시작 (M2130)"); // M2130
    public DeviceAddress AreaSensor = new DeviceAddress("구역 센서");
    public DeviceAddress HomePosition = new DeviceAddress("원점 상태");

    // 컨트롤러에서 참조할 기동 신호 상태 플래그
    [HideInInspector] public bool isStartCommandReceived = false;

    private void Start()
    {
        // PLC로부터 상태를 계속 읽어와야 하는 어드레스들만 콜백 등록 (Write용은 등록 금지)
        if (UpCommand.useDevice)
            MXRequester.Get.AddDeviceAddress(UpCommand.address, OnReceiveStartCommand);

        if (AreaSensor.useDevice)
            MXRequester.Get.AddDeviceAddress(AreaSensor.address, OnReceiveAreaSensor);

        if (HomePosition.useDevice)
            MXRequester.Get.AddDeviceAddress(HomePosition.address, OnReceiveHomePosition);
    }

    // ==================================================
    // [READ] PLC -> Unity 수신 콜백 함수
    // ==================================================
    private void OnReceiveStartCommand(short data)
    {
        // M2130 값이 1(ON)로 들어오면 기동 플래그 활성화
        isStartCommandReceived = (data != 0);
    }

    private void OnReceiveAreaSensor(short data) { /* 필요시 로직 추가 */ }
    private void OnReceiveHomePosition(short data) { /* 필요시 로직 추가 */ }

    // ==================================================
    // [WRITE] Unity -> PLC 송신 함수
    // ==================================================

    // READY 신호 (M89) 전송
    public void SendReadySignal(short value)
    {
        if (PressReady.useDevice)
            MXRequester.Get.AddSetDeviceRequest(PressReady.address, value);
    }

    // 레이저 센서 신호 전송
    public void SendLaserSensorSignal(short value)
    {
        if (laserSensorAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(laserSensorAddress.address, value);
    }

    // 프레스 작업 완료 신호 전송
    public void SendChamberCompleteSignal(short value)
    {
        if (pressCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(pressCompleteAddress.address, value);
    }
}