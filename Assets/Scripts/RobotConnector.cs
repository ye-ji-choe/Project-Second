using UnityEngine;

public class RobotConnector : MXObject
{
    [Header("로봇 시퀀스 연결")]
    public RobotSequenceTask robotTask; // 유니티 인스펙터에서 SequenceTask를 끌어다 넣는 곳!

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
        if (data != 0)
        {
            haveToExecute = true; // 기동 신호 ON!
        }
    }

    private void OnTaskValueReceived(short data)
    {
        currentTaskValue = data; // D0 값 업데이트
    }

    private void Update()
    {
        // 핵심: 기동 신호가 들어왔고 && D0(currentTaskValue)가 1일 때만 실행
        if (haveToExecute && currentTaskValue == 1)
        {
            robotTask.ResumeSequence(); // SequenceTask로 기동 명령 전달!
            IsBusy = true;
            haveToExecute = false;
        }

        // 사이클 완료 펄스 타이머 처리
        if (completedCycle && remainCompletedTime < Time.time)
        {
            if (cycleCompleteAddress.useDevice)
                MXRequester.Get.AddSetDeviceRequest(cycleCompleteAddress.address, 0);

            completedCycle = false;
        }
    }

    // SequenceTask의 작업이 끝나면 호출될 함수
    public void OnCycleCompleted()
    {
        completedCycle = true;
        remainCompletedTime = Time.time + feedbackTime;

        if (cycleCompleteAddress.useDevice)
            MXRequester.Get.AddSetDeviceRequest(cycleCompleteAddress.address, 1);

        IsBusy = false;
    }
}