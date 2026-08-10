using UnityEngine;

public class ScanRobotConnector : MXObject
{
    [Header("로봇 시퀀스 연결")]
    // 💡 인스펙터 창에서 ScanRobotSequenceTask를 여기에 연결하세요.
    public Task robotTask;

    public float feedbackTime = 0.5f;

    [Header("PLC Addresses (RX: PLC -> 로봇)")]
    public DeviceAddress startSignalAddress = new DeviceAddress("기동 신호 (M2110)");
    public DeviceAddress taskValueAddress = new DeviceAddress("목적지 번호 (D0)");

    [Header("PLC Addresses (TX: 로봇 -> PLC)")]
    public DeviceAddress busyAddress = new DeviceAddress("BUSY 신호 (M81)");
    public DeviceAddress cycleCompleteAddress = new DeviceAddress("사이클 완료 (M1094)");

    // ==========================================
    // ▼ 판정 결과 전송용 어드레스 추가 ▼
    // ==========================================
    public DeviceAddress passAddress = new DeviceAddress("스캔 패스 (M1166)");
    public DeviceAddress ngAddress = new DeviceAddress("스캔 NG (M1167)");

    private bool haveToExecute;
    private int currentTaskValue;
    private bool completedCycle;
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
        if (startSignalAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(startSignalAddress.address, OnStartSignalReceived);

        if (taskValueAddress.useDevice)
            MXRequester.Get.AddDeviceAddress(taskValueAddress.address, OnTaskValueReceived);
    }

    private void OnStartSignalReceived(short data)
    {
        bool previousSignal = isStartSignalOn;
        isStartSignalOn = (data != 0);

        if (!previousSignal && isStartSignalOn)
        {
            if (!IsBusy)
            {
                Debug.Log("[ScanRobotConnector] 기동 신호 상승 에지 확인. 스캔 로봇 기동 준비!");
                haveToExecute = true;
            }
            else
            {
                Debug.LogWarning("[ScanRobotConnector] 기동 신호가 들어왔으나 로봇이 이미 Busy 상태입니다.");
            }
        }
    }

    private void OnTaskValueReceived(short data)
    {
        currentTaskValue = data;
    }

    private void Update()
    {
        if (haveToExecute && currentTaskValue == 1)
        {
            Debug.Log("[ScanRobotConnector] 시퀀스 Task 시작 및 Busy ON");

            // 💡 새 사이클 기동 시 이전 판정 결과(PASS/NG) 초기화 (Clear)
            SendPassSignal(0);
            SendNGSignal(0);

            if (robotTask != null)
            {
                robotTask.Play();
            }
            else
            {
                Debug.LogError("[ScanRobotConnector] 에러: 인스펙터의 'Robot Task' 빈칸에 시퀀스 스크립트가 연결되지 않았습니다!");
            }

            IsBusy = true;
            haveToExecute = false; // 한 번 기동하면 즉시 플래그 리셋
        }

        // 사이클 완료 펄스 신호 OFF 처리
        if (completedCycle && remainCompletedTime < Time.time)
        {
            if (cycleCompleteAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(cycleCompleteAddress.address, 0);

            completedCycle = false;
        }
    }

    public void OnCycleCompleted()
    {
        completedCycle = true;
        remainCompletedTime = Time.time + feedbackTime;

        // 사이클 완료 펄스 신호 ON 처리
        if (cycleCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(cycleCompleteAddress.address, 1);

        IsBusy = false;
        Debug.Log("[ScanRobotConnector] 로봇 사이클 완료. Busy OFF 및 완료 펄스 전송.");
    }

    // ==========================================
    // ▼ 판정 신호 송신용 메서드 추가 ▼
    // ==========================================

    /// <summary>
    /// 스캔 PASS 신호 전송 (Task에서 호출)
    /// </summary>
    public void SendPassSignal(short value)
    {
        if (passAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(passAddress.address, value);
    }

    /// <summary>
    /// 스캔 NG 신호 전송 (Task에서 호출)
    /// </summary>
    public void SendNGSignal(short value)
    {
        if (ngAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(ngAddress.address, value);
    }
}