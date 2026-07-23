using UnityEngine;

public class RobotConnector : MXObject
{
    [Header("로봇 시퀀스 연결")]
    public RobotSequenceTask robotTask;

    public float feedbackTime = 0.5f;

    [Header("PLC Addresses (RX: PLC -> 로봇)")]
    public DeviceAddress startSignalAddress = new DeviceAddress("기동 신호 (M2110)");
    public DeviceAddress taskValueAddress = new DeviceAddress("목적지 번호 (D0)");

    [Header("PLC Addresses (TX: 로봇 -> PLC)")]
    public DeviceAddress busyAddress = new DeviceAddress("BUSY 신호 (M81)");
    public DeviceAddress cycleCompleteAddress = new DeviceAddress("사이클 완료 (M1094)");

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
        if (haveToExecute && currentTaskValue == 1)
        {
            Debug.Log("[RobotConnector] 시퀀스 Task 시작 및 Busy ON");

            // [수정 핵심] 변수만 켜주는 것이 아니라 Task 자체를 처음부터 재실행(빌드) 시킵니다.
            // ⚠️ 주의: 사용하시는 프레임워크에 따라 Play(), Restart(), Execute(), StartTask() 중 하나일 수 있습니다.
            // 빨간줄이 뜬다면 해당 Task 클래스에서 '실행'을 담당하는 함수 이름으로 바꿔주세요.
            robotTask.Play();

            IsBusy = true;
            haveToExecute = false;
        }

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

        if (cycleCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(cycleCompleteAddress.address, 1);

        IsBusy = false;
        Debug.Log("[RobotConnector] 로봇 사이클 완료. Busy OFF 및 완료 펄스 시작.");
    }
}