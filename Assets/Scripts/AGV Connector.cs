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

    public UnityEvent<bool> OnChangedBusy;

    private bool haveToExecute;
    private int destinationNum = -1;

    // [핵심 추가] AGV가 현재 도착해서 머물고 있는 정거장 번호를 기억
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
            destinationNum = data;

            // [수정] PLC가 목적지를 0으로 초기화하는 순간은 철저히 무시
            if (destinationNum > 0)
            {
                // [인터락] 이미 해당 위치에 도착해 있다면 2번 중복 이동하는 현상 원천 차단
                if (destinationNum == currentStationNum)
                {
                    Debug.LogWarning($"[AGVConnector] 이미 {destinationNum}번에 위치해 있습니다. 중복 출발 명령을 무시합니다.");
                    return;
                }

                // [복구] 기동 신호가 이미 켜져 있다면, 목적지가 하달되자마자 즉시 출발 허용 (1번 AGV 멈춤 해결)
                if (isStartSignalOn && !IsBusy)
                {
                    Debug.Log($"[AGVConnector] 목적지 변경 감지 -> 기동 준비 완료 (최종 목적지: {destinationNum})");
                    haveToExecute = true;
                    currentStationNum = -1; // 출발하므로 현재 위치 기억 지움
                }
            }
        }
    }

    private void StartOperation(short data)
    {
        bool previousSignal = isStartSignalOn;
        isStartSignalOn = (data != 0);

        if (!previousSignal && isStartSignalOn) // 상승 에지
        {
            if (destinationNum > 0)
            {
                // 여기에도 동일한 인터락 적용
                if (destinationNum == currentStationNum)
                {
                    Debug.LogWarning($"[AGVConnector] 이미 {destinationNum}번에 위치해 있습니다. 기동 무시.");
                    return;
                }

                if (!IsBusy)
                {
                    Debug.Log($"[AGVConnector] 기동 신호 ON 감지 -> 기동 준비 완료 (최종 목적지: {destinationNum})");
                    haveToExecute = true;
                    currentStationNum = -1; // 출발 리셋
                }
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

        // [추가] 무사히 도착하면 현재 도착한 번호를 메모리에 기록해둠 (중복 기동 방지용)
        currentStationNum = destinationNum;

        Debug.Log($"[AGVConnector] {destinationNum}번 도착 완료. 도착 신호 전송 및 Busy 해제");
    }
}