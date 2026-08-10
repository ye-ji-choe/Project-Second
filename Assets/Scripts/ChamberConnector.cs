using UnityEngine;
using System.Collections.Generic;
using System;

public class ChamberConnector : MXObject
{
    public ChamberController controller; // 인스펙터 연결 필수

    public DeviceAddress doorOpenCommand = new DeviceAddress("도어 오픈 명령 (M2020)");
    public DeviceAddress doorCloseCommand = new DeviceAddress("도어 클로즈 명령 (M2026)");

    // 마그네틱 센서는 1개로 단일 할당
    public DeviceAddress magSensorAddress = new DeviceAddress("마그네틱 센서 인식 (AGV 앞)");

    // 레이저 센서는 4개 배열로 할당
    public DeviceAddress[] laserSensorAddresses = new DeviceAddress[4]
    {
        new DeviceAddress("레이저 센서 1 인식"),
        new DeviceAddress("레이저 센서 2 인식"),
        new DeviceAddress("레이저 센서 3 인식"),
        new DeviceAddress("레이저 센서 4 인식")
    };

    public DeviceAddress chamberCompleteAddress = new DeviceAddress("챔버 완료 (M1010)");

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
        HaveToOpenDoor = (data != 0);
    }

    private void OnReceiveDoorClose(short data)
    {
        HaveToCloseDoor = (data != 0);
    }

    /// <summary>
    /// 단일 마그네틱 센서 신호 전송
    /// </summary>
    public void SendMagneticSensorSignal(short value)
    {
        if (magSensorAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(magSensorAddress.address, value);
    }

    /// <summary>
    /// 다중 레이저 센서 신호 전송
    /// </summary>
    public void SendLaserSensorSignal(int index, short value)
    {
        if (index < 0 || index >= laserSensorAddresses.Length)
        {
            Debug.LogWarning($"[ChamberConnector] 유효하지 않은 레이저 센서 인덱스입니다: {index}");
            return;
        }

        if (laserSensorAddresses[index].useDevice)
            MXRequester.Get.AddSetDeviceRequest(laserSensorAddresses[index].address, value);
    }

    public void SendChamberCompleteSignal(short value)
    {
        if (chamberCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(chamberCompleteAddress.address, value);
    }
}