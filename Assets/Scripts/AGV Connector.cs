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

    // [수정 1] 목적지 초기값을 -1(무효값)로 설정하여, PLC가 값을 주기 전에는 절대 움직이지 않도록 방어
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

            // [수정 2] 위험 로직 제거: 
            // 기동 신호가 유지된 상태에서 목적지가 바뀌었다고 무조건 출발하는 로직을 삭제했습니다.
            // 이제 출발(haveToExecute = true)은 오직 StartOperation(상승 에지)에서만 결정됩니다.
        }
    }

    private void StartOperation(short data)
    {
        bool previousSignal = isStartSignalOn;
        isStartSignalOn = (data != 0);

        // 상승 에지(OFF -> ON) 순간에만 명확하게 기동 플래그를 세움
        if (!previousSignal && isStartSignalOn)
        {
            // [수정 3] 목적지 유효성 검사
            // 내부 데이터가 -1(초기화 상태)라면 기동 신호가 들어와도 무시합니다.
            if (destinationNum < 0)
            {
                Debug.LogWarning("[AGVConnector] 기동 신호가 들어왔으나 유효한 목적지 데이터가 없습니다. (오작동 방지)");
                return;
            }

            if (!IsBusy)
            {
                Debug.Log($"[AGVConnector] 기동 신호 ON -> 기동 준비 완료 (최종 목적지: {destinationNum})");
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

        // [수정] 아래의 내부 목적지 데이터 강제 초기화(-1) 로직을 삭제합니다.
        // AGV는 '도착 완료' 상태만 상위에 보고하며, 목적지 데이터는 PLC가 덮어씌울 때까지 유지합니다.
        // StartOperation에 이미 상승 에지(Rising Edge) 인터락이 있으므로 오작동(재출발)하지 않습니다.

        Debug.Log($"[AGVConnector] {destinationNum}번 도착 완료. 도착 신호 전송 및 Busy 해제");
    }
}