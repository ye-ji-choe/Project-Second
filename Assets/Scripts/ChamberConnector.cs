using UnityEngine;
using System.Collections.Generic;
using System;

public class ChamberConnector : MXObject
{
    public ChamberController controller; // 인스펙터 연결 필수

    public DeviceAddress doorOpenCommand = new DeviceAddress("도어 오픈 명령 (M2020)");
    public DeviceAddress doorCloseCommand = new DeviceAddress("도어 클로즈 명령 (M2026)");
    public DeviceAddress magSensorAddress = new DeviceAddress("마그네틱 센서 인식 (AGV 앞)");
    public DeviceAddress laserSensorAddress = new DeviceAddress("레이저 센서 인식 (AGV 안)");
    public DeviceAddress chamberCompleteAddress = new DeviceAddress("챔버 완료 (M1010)");

    // 상태 플래그를 public 속성으로 변경하여 Controller가 Update문에서 읽어갈 수 있게 함
    public bool HaveToOpenDoor { get; private set; } = false;
    public bool HaveToCloseDoor { get; private set; } = false;

    private void Start()
    {
        if (doorOpenCommand.useDevice)
            MXRequester.Get.AddDeviceAddress(doorOpenCommand.address, OnReceiveDoorOpen);

        if (doorCloseCommand.useDevice)
            MXRequester.Get.AddDeviceAddress(doorCloseCommand.address, OnReceiveDoorClose);
    }

    private void OnReceiveDoorOpen(short data)
    {
        // 통신 콜백에서는 플래그(bool) 상태만 변경합니다.
        HaveToOpenDoor = (data != 0);
    }

    private void OnReceiveDoorClose(short data)
    {
        HaveToCloseDoor = (data != 0);
    }

    public void SendMagneticSensorSignal(short value)
    {
        if (magSensorAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(magSensorAddress.address, value);
    }

    public void SendLaserSensorSignal(short value)
    {
        if (laserSensorAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(laserSensorAddress.address, value);
    }

    public void SendChamberCompleteSignal(short value)
    {
        if (chamberCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(chamberCompleteAddress.address, value);
    }
}