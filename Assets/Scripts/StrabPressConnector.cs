using UnityEngine;
using System.Collections.Generic;
using System;

public class StrabPressConnector : MXObject
{
    public StrabPressController controller; // 챔버를 제어할 애니메이션/센서 컨트롤러
    public DeviceAddress PressReady = new DeviceAddress("프레스 READY"); //프레스 레디
    public DeviceAddress AreaSensor = new DeviceAddress("구역 센서"); //센서
    public DeviceAddress HomePosition = new DeviceAddress("원점 상태"); // 다운 명령
    public DeviceAddress UpCommand = new DeviceAddress("기동 시작"); // 예: 업 명령

    public DeviceAddress laserSensorAddress = new DeviceAddress("레이저 센서 인식");
    public DeviceAddress pressCompleteAddress = new DeviceAddress("프레스 완료");

    // 상태 플래그
    private bool haveToDown = false;
    private bool haveToUp = false;

    private void Start()
    {
        // PLC로부터 읽어올 데이터 구독 (프레스 내림/프레스 올림 명령)
        if (PressReady.useDevice)
            MXRequester.Get.AddDeviceAddress(PressReady.address, OnReceiveDown);

        if (AreaSensor.useDevice)
            MXRequester.Get.AddDeviceAddress(AreaSensor.address, OnReceiveUp);

        if (pressCompleteAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(pressCompleteAddress.address, OnReceiveDown);

        if (HomePosition.useDevice)
            MXRequester.Get.AddDeviceAddress(HomePosition.address, OnReceiveUp);
    }

    // ==================================================
    // [READ] PLC -> Unity 콜백 함수 (PLC가 유니티에 명령)
    // ==================================================
    private void OnReceiveDown(short data)
    {
        // 값이 1(ON)로 들어오면 문을 열어야 함
        if (data != 0) haveToDown = true;
    }

    private void OnReceiveUp(short data)
    {
        // 값이 1(ON)로 들어오면 문을 닫아야 함
        if (data != 0) haveToUp = true;
    }


    // 1. AGV가 레이저 센서에 닿았을 때 호출
    public void SendLaserSensorSignal(short value)
    {
        if (laserSensorAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(laserSensorAddress.address, value);
    }

    // 2. 프레스 작업 완료 시 호출
    public void SendChamberCompleteSignal(short value)
    {
        if (pressCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(pressCompleteAddress.address, value);
    }
}