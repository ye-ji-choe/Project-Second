using System;
using UnityEngine;

public class AGVConnector : MXObject
{
    public AGVController agv;
    public float feedbackTime = 0.3f;

    [Header("PLC Addresses")]
    private DeviceAddress plcReadyAddress = new DeviceAddress("PLC Ready");
    public DeviceAddress busyAddress = new DeviceAddress("BUSY 신호");
    public DeviceAddress homePositionAddress = new DeviceAddress("Home Position");
    public DeviceAddress arrivalCompleteAddress = new DeviceAddress("도착 완료");

    public DeviceAddress startOperationAddress = new DeviceAddress("기동 시작");
    public DeviceAddress destinationAddress = new DeviceAddress("목적지 설비 번호");

    private bool haveToExecute;
    private int destinationNum = -1;

    private bool completedArrival;
    private float remainCompletedTime;

    private bool isStartSignalOn = false;
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
        if (plcReadyAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(plcReadyAddress.address, PLCReady);

        if (startOperationAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(startOperationAddress.address, StartOperation);

        if (destinationAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(destinationAddress.address, SetDestination);
    }

    private void PLCReady(short data)
    {
        if (data != 0)
            Debug.Log("[AGVConnector] PLC 통신 연결 성공 (READY)!!!");
        else
            Debug.Log("[AGVConnector] PLC 통신 연결 안됨 (Not READY)");
    }

    private void SetDestination(short data)
    {
        if (destinationNum != data)
        {
            Debug.Log($"[AGVConnector] 목적지 변경 감지: {destinationNum} -> {data}");
            destinationNum = data;

            // [추가된 로직] 목적지가 변경되었을 때, 기동 신호가 이미 켜져있고 대기 상태라면 기동
            if (isStartSignalOn && !IsBusy)
            {
                Debug.Log($"[AGVConnector] 기동 신호(M120)가 이미 ON 상태입니다. 즉시 기동 준비 (최종 목적지: {destinationNum})");
                haveToExecute = true;
            }
        }
    }

    private void StartOperation(short data)
    {
        bool previousSignal = isStartSignalOn;
        isStartSignalOn = (data != 0);

        // 상승 에지(OFF -> ON) 순간 기동
        if (!previousSignal && isStartSignalOn)
        {
            if (destinationNum < 0)
            {
                Debug.LogWarning("[AGVConnector] 기동 신호가 들어왔으나 유효한 목적지 데이터가 없습니다. (오작동 방지)");
                return;
            }

            if (!IsBusy)
            {
                Debug.Log($"[AGVConnector] 기동 신호 ON 감지 -> 기동 준비 완료 (최종 목적지: {destinationNum})");
                haveToExecute = true;
            }
            else
            {
                Debug.LogWarning("[AGVConnector] 기동 신호가 들어왔으나 AGV가 이미 동작 중(Busy)입니다.");
            }
        }
    }

    private void Update()
    {
        if (haveToExecute)
        {
            haveToExecute = false;

            Debug.Log($"[AGVConnector] AGV Positioning 명령 하달 (최종 목적지: {destinationNum})");
            agv.Positioning(destinationNum);
            IsBusy = true;
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

        Debug.Log($"[AGVConnector] {destinationNum}번 도착 완료. 도착 신호 전송 및 Busy 해제");
    }
}