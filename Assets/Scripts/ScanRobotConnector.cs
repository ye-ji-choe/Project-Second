using UnityEngine;

public class ScanRobotConnector : MXObject
{
    [Header("로봇 시퀀스 연결")]
    public ScanRobotSequenceTask robotTask;

    public float feedbackTime = 0.5f;

    [Range(0f, 100f)]
    [Tooltip("PASS 판정 확률 (%)")]
    public float passProbability = 80f; // 기본값 80% 확률로 PASS 발생

    [Header("PLC Addresses (RX: PLC -> 로봇)")]
    public DeviceAddress startSignalAddress = new DeviceAddress("기동 신호 (M2110)");
    public DeviceAddress taskValueAddress = new DeviceAddress("목적지 번호 (D0)");

    [Header("PLC Addresses (TX: 로봇 -> PLC)")]
    public DeviceAddress busyAddress = new DeviceAddress("BUSY 신호 (M81)");
    public DeviceAddress cycleCompleteAddress = new DeviceAddress("사이클 완료 (M1094)");

    // [추가] PASS / NG 신호 주소 
    public DeviceAddress passAddress = new DeviceAddress("PASS 신호 (M1095)"); // 실제 PLC 할당 주소로 변경 필요
    public DeviceAddress ngAddress = new DeviceAddress("NG 신호 (M1096)");     // 실제 PLC 할당 주소로 변경 필요

    private bool haveToExecute;
    private int currentTaskValue;
    private bool completedCycle;
    private float remainCompletedTime;

    // 기동 신호의 이전 상태를 기억할 변수
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
        // 상승 에지 판단 로직
        bool previousSignal = isStartSignalOn;
        isStartSignalOn = (data != 0);

        // 이전에는 OFF였고, 지금 ON으로 들어온 순간에만
        if (!previousSignal && isStartSignalOn)
        {
            if (!IsBusy) // 로봇이 대기 상태일 때만 기동 허용
            {
                Debug.Log("[RobotConnector] 기동 신호 상승 에지 확인. 기동 준비!");
                haveToExecute = true;
            }
            else
            {
                Debug.LogWarning("[RobotConnector] 기동 신호가 들어왔으나 로봇이 이미 Busy 상태입니다.");
            }
        }
    }

    private void OnTaskValueReceived(short data)
    {
        currentTaskValue = data;
    }

    private void Update()
    {
        // 핵심: 상승 에지로 기동 플래그가 섰고 && D0가 1일 때
        if (haveToExecute && currentTaskValue == 1)
        {
            Debug.Log("[RobotConnector] 시퀀스 Task 시작 및 Busy ON");
            robotTask.ResumeSequence();
            IsBusy = true;
            haveToExecute = false; // 한 번 기동하면 즉시 플래그 리셋
        }

        // feedbackTime 경과 후 펄스 신호 초기화(OFF) 처리
        if (completedCycle && remainCompletedTime < Time.time)
        {
            if (cycleCompleteAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(cycleCompleteAddress.address, 0);

            // [추가] PASS/NG 신호 펄스 OFF
            if (passAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(passAddress.address, 0);

            if (ngAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(ngAddress.address, 0);

            completedCycle = false;
        }
    }

    public void OnCycleCompleted()
    {
        completedCycle = true;
        remainCompletedTime = Time.time + feedbackTime;

        // 사이클 완료 신호 ON
        if (cycleCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(cycleCompleteAddress.address, 1);

        // [추가] 랜덤 PASS / NG 판정 로직
        // Random.Range(0f, 100f)를 통해 지정한 확률에 따라 결과 분기
        bool isPass = Random.Range(0f, 100f) <= passProbability;

        if (isPass)
        {
            if (passAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(passAddress.address, 1);
            Debug.Log("[RobotConnector] 판정 결과: PASS 전송 완료");
        }
        else
        {
            if (ngAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(ngAddress.address, 1);
            Debug.Log("[RobotConnector] 판정 결과: NG 전송 완료");
        }

        IsBusy = false;
        Debug.Log("[RobotConnector] 로봇 사이클 완료. Busy OFF 및 완료/판정 펄스 시작.");
    }
}