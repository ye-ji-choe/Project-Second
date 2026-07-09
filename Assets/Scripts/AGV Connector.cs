using System;
using UnityEngine;

public class AGVConnector : MXObject
{
    public AGVController agv;
    public float feedbackTime = 0.3f;

    [Header("PLC Addresses")]
    public DeviceAddress plcReadyAddress = new DeviceAddress("PLC Ready"); // 추가된 부분
    public DeviceAddress busyAddress = new DeviceAddress("BUSY 신호");
    public DeviceAddress homePositionAddress = new DeviceAddress("Home Position");
    public DeviceAddress arrivalCompleteAddress = new DeviceAddress("도착 완료");

    public DeviceAddress startOperationAddress = new DeviceAddress("기동 시작");
    public DeviceAddress destinationAddress = new DeviceAddress("목적지 설비 번호");

    private bool haveToExecute;
    private int destinationNum;
    private bool completedArrival;
    private float remainCompletedTime;

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
        // 1. PLC 통신 연결 상태 구독 (로그 출력용)
        if (plcReadyAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(plcReadyAddress.address, PLCReady);

        // 2. 기동 및 목적지 수신
        if (startOperationAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(startOperationAddress.address, StartOperation);

        if (destinationAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(destinationAddress.address, SetDestination);
    }

    // PLC 통신이 정상적으로 붙었는지 확인하는 함수
    private void PLCReady(short data)
    {
        if (data != 0)
            Debug.Log("[AGVConnector] PLC 통신 연결 성공 (READY)!!!");
        else
            Debug.Log("[AGVConnector] PLC 통신 연결 안됨 (Not READY)");
    }

    private void SetDestination(short data)
    {
        destinationNum = data;
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
        if (haveToExecute)
        {
            agv.Positioning(destinationNum);
            IsBusy = true;
            haveToExecute = false;
        }

        if (completedArrival && remainCompletedTime < Time.time)
        {
            if (arrivalCompleteAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(arrivalCompleteAddress.address, 0);

            completedArrival = false;
        }
    }

    public void OnArrivalCompleted()
    {
        completedArrival = true;
        remainCompletedTime = Time.time + feedbackTime;

        if (arrivalCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(arrivalCompleteAddress.address, 1);

        IsBusy = false;
    }
}