using UnityEngine;
using System.Collections.Generic;
using System;

public class ChamberConnector : MXObject
{
    public ChamberController controller; // 챔버를 제어할 애니메이션/센서 컨트롤러
    public DeviceAddress doorOpenCommand = new DeviceAddress("도어 오픈 명령 (M2020)"); // 예: M2020
    public DeviceAddress doorCloseCommand = new DeviceAddress("도어 클로즈 명령 (M2026)"); // 예: M2026
    public DeviceAddress magSensorAddress = new DeviceAddress("마그네틱 센서 인식 (AGV 앞)");
    public DeviceAddress laserSensorAddress = new DeviceAddress("레이저 센서 인식 (AGV 안)");

    public DeviceAddress chamberCompleteAddress = new DeviceAddress("챔버 완료 (M1010)");

    // 상태 플래그
    private bool haveToOpenDoor = false;
    private bool haveToCloseDoor = false;

    private void Start()
    {
        // PLC로부터 읽어올 데이터 구독 (문 열림/닫힘 명령)
        if (doorOpenCommand.useDevice)
            MXRequester.Get.AddDeviceAddress(doorOpenCommand.address, OnReceiveDoorOpen);

        if (doorCloseCommand.useDevice)
            MXRequester.Get.AddDeviceAddress(doorCloseCommand.address, OnReceiveDoorClose);
    }

    // ==================================================
    // [READ] PLC -> Unity 콜백 함수 (PLC가 유니티에 명령)
    // ==================================================
    private void OnReceiveDoorOpen(short data)
    {
        // 값이 1(ON)로 들어오면 문을 열어야 함
        if (data != 0) haveToOpenDoor = true;
    }

    private void OnReceiveDoorClose(short data)
    {
        // 값이 1(ON)로 들어오면 문을 닫아야 함
        if (data != 0) haveToCloseDoor = true;
    }

    // ==================================================
    // [WRITE] Unity -> PLC 호출 함수 (유니티가 PLC에 보고)
    // ==================================================

    // 1. AGV가 마그네틱 센서에 닿았을 때 호출
    public void SendMagneticSensorSignal(short value)
    {
        if (magSensorAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(magSensorAddress.address, value);
    }

    // 2. AGV가 레이저 센서에 닿았을 때 호출
    public void SendLaserSensorSignal(short value)
    {
        if (laserSensorAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(laserSensorAddress.address, value);
    }

    // 5. 챔버 작업 완료 시 호출
    public void SendChamberCompleteSignal(short value)
    {
        if (chamberCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(chamberCompleteAddress.address, value);
    }
}