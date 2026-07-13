using UnityEngine;
using System.Collections.Generic;
using System;

public class StrabPressConnector : MXObject
{
    public StrabPressController controller; // 컨트롤러 할당용

    public DeviceAddress PressReady = new DeviceAddress("프레스 READY");
    public DeviceAddress AreaSensor = new DeviceAddress("구역 센서");
    public DeviceAddress HomePosition = new DeviceAddress("원점 상태");
    public DeviceAddress StartCommand = new DeviceAddress("기동 시작"); // (수정) M2190 신호

    public DeviceAddress laserSensorAddress = new DeviceAddress("레이저 센서 인식");
    public DeviceAddress pressCompleteAddress = new DeviceAddress("프레스 완료"); // M1174 신호

    // Controller에서 읽어갈 수 있도록 상태 플래그를 public 프로퍼티로 선언
    public bool HaveToStart { get; private set; } = false;

    private void Start()
    {
        // PLC로부터 "기동 시작(M2190)" 신호가 들어오는지 구독 (가장 중요)
        if (StartCommand.useDevice)
            MXRequester.Get.AddDeviceAddress(StartCommand.address, OnReceiveStart);

        // (필요 시 프레스 레디나 원점 상태도 동일한 방식으로 구독 추가 가능)
    }

    // ==================================================
    // [READ] PLC -> Unity 콜백 함수
    // ==================================================
    private void OnReceiveStart(short data)
    {
        // M2190 값이 1(ON)로 들어오면 기동 시작 플래그를 true로 변경
        HaveToStart = (data != 0);
    }

    // ==================================================
    // [WRITE] Unity -> PLC 호출 함수
    // ==================================================
    // 1. 레이저 센서 감지 신호 전송
    public void SendLaserSensorSignal(short value)
    {
        if (laserSensorAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(laserSensorAddress.address, value);
    }

    // 2. 프레스 작업 완료 신호 전송 (함수명 직관적으로 변경)
    public void SendPressCompleteSignal(short value)
    {
        if (pressCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(pressCompleteAddress.address, value);
    }
}