using UnityEngine;

public class VcumRobotConnector : MXObject
{
    [Header("로봇 시퀀스 연결")]
    // 💡 방금 만드신 'VcumRobotSequenceTask'를 여기에 끌어다 넣으시면 됩니다!
    public VcumRobotSequenceTask robotTask;

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
                Debug.Log("[VcumRobotConnector] 기동 신호 상승 에지 확인. 진공 흡착(Vcum) 로봇 기동 준비!");
                haveToExecute = true;
            }
            else
            {
                Debug.LogWarning("[VcumRobotConnector] 기동 신호가 들어왔으나 로봇이 이미 Busy 상태입니다.");
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
            Debug.Log("[VcumRobotConnector] 시퀀스 Task 시작 및 Busy ON");

            // 연결된 시퀀스 Task 실행 (null 체크 추가하여 안전성 확보)
            if (robotTask != null)
            {
                robotTask.Play();
            }
            else
            {
                Debug.LogError("[VcumRobotConnector] 에러: 인스펙터의 'Robot Task' 빈칸에 시퀀스 스크립트가 연결되지 않았습니다!");
            }

            IsBusy = true;
            haveToExecute = false;
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
        Debug.Log("[VcumRobotConnector] 로봇 사이클 완료. Busy OFF 및 완료 펄스 전송.");
    }
}