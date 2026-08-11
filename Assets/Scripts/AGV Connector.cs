using System;
using UnityEngine;
using UnityEngine.Events;

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

    // [핵심 추가] PLC로 현재 위치 번호를 쏴주는 어드레스
    public DeviceAddress currentPositionAddress = new DeviceAddress("현재 위치 번호");

    public UnityEvent<bool> OnChangedBusy;

    private bool haveToExecute;
    private int destinationNum = -1;

    // AGV가 현재 도착해서 머물고 있는 정거장 번호를 기억
    private int currentStationNum = -1;

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
            OnChangedBusy?.Invoke(value);
        }
    }

    private void SetCurrentPositionToPLC(int positionNum)
    {
        if (currentPositionAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(currentPositionAddress.address, (short)positionNum);
    }

    private void Start()
    {
        Debug.Log($"[{gameObject.name} AGVConnector] Start() 실행됨. " +
                  $"목적지 주소: {destinationAddress.address} (체크됨: {destinationAddress.useDevice}), " +
                  $"기동 주소: {startOperationAddress.address} (체크됨: {startOperationAddress.useDevice})");

        if (plcReadyAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(plcReadyAddress.address, PLCReady);

        if (startOperationAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(startOperationAddress.address, StartOperation);

        if (destinationAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(destinationAddress.address, SetDestination);

        SetCurrentPositionToPLC(0);
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
            destinationNum = data;

            if (destinationNum > 0)
            {
                if (destinationNum == currentStationNum)
                {
                    Debug.LogWarning($"[AGVConnector] 이미 {destinationNum}번에 위치해 있습니다. 중복 출발 명령을 무시합니다.");
                    return;
                }

                if (isStartSignalOn && !IsBusy)
                {
                    Debug.Log($"[AGVConnector] 목적지 변경 감지 -> 기동 준비 완료 (최종 목적지: {destinationNum})");
                    haveToExecute = true;
                    currentStationNum = -1;
                }
            }
        }
    }

    private void StartOperation(short data)
    {
        bool previousSignal = isStartSignalOn;
        isStartSignalOn = (data != 0);

        if (!previousSignal && isStartSignalOn)
        {
            if (destinationNum > 0)
            {
                if (destinationNum == currentStationNum)
                {
                    Debug.LogWarning($"[AGVConnector] 이미 {destinationNum}번에 위치해 있습니다. 기동 무시.");
                    return;
                }

                if (!IsBusy)
                {
                    Debug.Log($"[AGVConnector] 기동 신호 ON 감지 -> 기동 준비 완료 (최종 목적지: {destinationNum})");
                    haveToExecute = true;
                    currentStationNum = -1;
                }
            }
        }
    }

    private void Update()
    {
        if (haveToExecute)
        {
            haveToExecute = false;

            // 기동을 시작하므로 현재 위치를 0으로 PLC에 전송
            SetCurrentPositionToPLC(0);

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

        // ==========================================
        // [수정] PLC 데이터 증발 방지 로직
        // ==========================================
        // 이동 중 PLC가 목적지 번호를 0으로 초기화했을 수 있으므로,
        // AGV가 확실하게 기억하고 있는 목적지 번호(currentStationId)를 직접 끌어옵니다.
        currentStationNum = agv.currentStationId;

        // 도착 완료 시 확정된 위치 번호를 PLC로 전송
        SetCurrentPositionToPLC(currentStationNum);

        Debug.Log($"[AGVConnector] {currentStationNum}번 도착 완료. 신호 전송 및 현재위치 갱신");
    }
}