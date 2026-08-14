using UnityEngine;

/// <summary>
/// 램프 하나에 붙이는 단순 커넥터.
/// PreciseStopSensorConnector와 구조가 1:1로 대응되므로 가장 이해하기 쉽다.
///
/// [배치]
///   공정_1_컨베이어_구동센서_신호
///   ├── SignalLamp                    ← 표시
///   └── SingleSignalLampConnector     ← PLC 연결
///
/// 램프가 많아지면 SignalLampConnector(패널 단위)를 쓰는 편이 관리하기 쉽다.
/// </summary>
public class SingleSignalLampConnector : MXObject
{
    [Header("PLC Input")]
    [Tooltip("표시할 디바이스. 비워두면 SignalLamp의 Device Name을 사용한다.")]
    public DeviceAddress signal = new DeviceAddress("Lamp Signal");

    [Header("표시")]
    [Tooltip("비워두면 같은 오브젝트 또는 자식에서 자동으로 찾는다.")]
    public SignalLamp lamp;

    [Tooltip("값이 0이 아닐 때 점등 대신 노란색 점멸. 컨베이어 구동 표시에 적합.")]
    public bool blinkWhenOn = false;

    [Tooltip("신호를 반전시킨다. b접점 센서 등.")]
    public bool invert = false;

    [Header("Debug")]
    public bool verboseLog = false;

    private string resolvedAddress;
    private bool subscribed;

    // ============================================================

    private void Awake()
    {
        if (lamp == null) lamp = GetComponent<SignalLamp>();
        if (lamp == null) lamp = GetComponentInChildren<SignalLamp>(true);
        if (lamp == null) lamp = GetComponentInParent<SignalLamp>();
    }

    private void Start()
    {
        if (lamp == null)
        {
            Debug.LogError($"[{name}] SignalLamp가 없습니다.", this);
            return;
        }

        // 초기 소등
        lamp.SetOn(false);

        resolvedAddress = ResolveAddress();

        if (string.IsNullOrEmpty(resolvedAddress))
        {
            Debug.LogWarning(
                $"[{name}] 디바이스 주소가 비어 있습니다. " +
                $"Signal의 address를 채우거나 SignalLamp의 Device Name을 입력하세요.", this);
            return;
        }

        if (MXRequester.Get == null)
        {
            Debug.LogError($"[{name}] MXRequester를 찾지 못했습니다.", this);
            return;
        }

        // 등록 직후 현재 값으로 콜백이 1회 호출되므로 초기 동기화가 자동으로 된다.
        MXRequester.Get.AddDeviceAddress(resolvedAddress, OnSignalChanged);
        subscribed = true;

        if (verboseLog)
            Debug.Log($"[{name}] 구독 등록: {resolvedAddress}", this);
    }

    private void OnDestroy()
    {
        if (MXRequester.Get == null) return;
        if (!subscribed || string.IsNullOrEmpty(resolvedAddress)) return;

        MXRequester.Get.RemoveDeviceAddress(resolvedAddress, OnSignalChanged);
        subscribed = false;
    }

    // ============================================================

    private string ResolveAddress()
    {
        if (signal != null && signal.useDevice && !string.IsNullOrEmpty(signal.address))
            return signal.address;

        return lamp != null ? lamp.deviceName : null;
    }

    private void OnSignalChanged(short data)
    {
        if (lamp == null) return;

        bool on = (data != 0);
        if (invert) on = !on;

        if (on && blinkWhenOn)
            lamp.SetBlink(true);
        else
            lamp.SetOn(on);

        if (verboseLog)
            Debug.Log($"[{resolvedAddress}] = {data}", this);
    }
}