using System;
using UnityEngine;

public class AGVConnector : MXObject
{
    public AGVController agv; // Cartesian 코드의 axis1, axis2 역할
    public float feedbackTime = 0.3f;

    [Header("PLC Addresses")]
    public DeviceAddress busyAddress = new DeviceAddress("BUSY 신호"); // X200
    public DeviceAddress homePositionAddress = new DeviceAddress("Home Position"); // X206
    public DeviceAddress arrivalCompleteAddress = new DeviceAddress("도착 완료"); // X207

    public DeviceAddress startOperationAddress = new DeviceAddress("기동 시작"); // Y225
    public DeviceAddress destinationAddress = new DeviceAddress("목적지 설비 번호"); // D00

    private bool haveToExecute;
    private int destinationNum; // D00 값 저장

    private bool completedArrival;
    private float remainCompletedTime;

    // 직교로봇 코드와 동일한 BUSY 프로퍼티 패턴 적용 (상태가 바뀔 때만 PLC 전송)
    private bool isBusy;
    public bool IsBusy
    {
        get => isBusy;
        set
        {
            if (isBusy == value) return;
            isBusy = value;
            if (busyAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(busyAddress.address, (short)(value ? 1 : 0));
        }
    }

    private void Start()
    {
        // PLC -> Unity (기동 및 목적지 수신)
        if (startOperationAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(startOperationAddress.address, StartOperation);

        if (destinationAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(destinationAddress.address, SetDestination);
    }

    private void SetDestination(short data)
    {
        destinationNum = data; // 0 ~ 15
    }

    private void StartOperation(short data)
    {
        if (data != 0)
        {
            haveToExecute = true;
        }
    }

    private void Update()
    {
        // 1. 기동 신호 처리
        if (haveToExecute)
        {
            // AGV 컨트롤러에 이동 명령 하달
            agv.Positioning(destinationNum);

            IsBusy = true; // 이동 시작하므로 BUSY ON
            haveToExecute = false;
        }

        // 2. 도착 완료 신호 초기화 타이머 (Cartesian 코드 패턴)
        if (completedArrival && remainCompletedTime < Time.time)
        {
            if (arrivalCompleteAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(arrivalCompleteAddress.address, 0);

            completedArrival = false;
        }
    }

    // AGVController가 목적지에 도착하면 이 함수를 호출함
    public void OnArrivalCompleted()
    {
        completedArrival = true;
        remainCompletedTime = Time.time + feedbackTime;

        if (arrivalCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(arrivalCompleteAddress.address, 1);

        IsBusy = false; // 도착했으므로 BUSY OFF
    }
}