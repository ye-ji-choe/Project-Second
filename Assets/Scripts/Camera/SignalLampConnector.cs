using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공정 패널 하나에 붙여서, 그 안의 램프 여러 개를 한 번에 PLC에 연결한다.
/// PreciseStopSensorConnector와 동일한 MXObject 패턴을 따른다.
///
/// [배치]
///   Panel_공정1  ← 이 컴포넌트를 여기에
///   ├── 공정_1_컨베이어_구동센서_신호 (SignalLamp, deviceName = Y0C0)
///   └── 공정_1_화분_감지센서_신호     (SignalLamp, deviceName = X0B4)
///
/// [주소 결정 순서]
///   1. Device의 address가 비어 있지 않으면 그것을 쓴다.
///   2. 비어 있으면 SignalLamp의 deviceName을 쓴다.
///   → 램프에 deviceName만 적어두면 여기는 건드릴 필요가 없다.
/// </summary>
public class SignalLampConnector : MXObject
{
    [Serializable]
    public class LampBinding
    {
        [Tooltip("구분용 이름. 동작에는 영향 없음.")]
        public string label = "램프";

        [Tooltip("PLC 디바이스. 비워두면 SignalLamp의 Device Name을 사용한다.")]
        public DeviceAddress device = new DeviceAddress("Lamp Device");

        public SignalLamp lamp;

        [Tooltip("값이 0이 아닐 때 점등 대신 노란색 점멸. 컨베이어 구동 표시에 적합.")]
        public bool blinkWhenOn = false;

        [Tooltip("신호를 반전시킨다. b접점 센서 등.")]
        public bool invert = false;

        // 런타임 전용
        [NonSerialized] public Action<short> handler;
        [NonSerialized] public string resolvedAddress;
        [NonSerialized] public bool subscribed;
        [NonSerialized] public short lastValue;
    }

    [Header("램프 바인딩")]
    public List<LampBinding> lamps = new List<LampBinding>();

    [Header("자동 수집")]
    [Tooltip("시작 시 자식의 SignalLamp 중 목록에 없는 것을 자동으로 추가한다. " +
             "각 램프의 Device Name이 주소로 사용된다.")]
    public bool autoCollectFromChildren = true;

    [Header("연결 대기")]
    [Tooltip("MXRequester가 아직 준비되지 않았을 때 최대 몇 초까지 기다릴지.")]
    [Min(0f)] public float waitForRequesterSeconds = 3f;

    [Header("Debug")]
    public bool verboseLog = false;

    // ============================================================
    // 라이프사이클
    // ============================================================

    private void Awake()
    {
        if (autoCollectFromChildren)
            CollectFromChildren();
    }

    private void Start()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private void OnDestroy()
    {
        UnsubscribeAll();
    }

    private void CollectFromChildren()
    {
        foreach (SignalLamp lamp in GetComponentsInChildren<SignalLamp>(true))
        {
            bool exists = false;
            foreach (LampBinding b in lamps)
            {
                if (b != null && b.lamp == lamp) { exists = true; break; }
            }
            if (exists) continue;

            lamps.Add(new LampBinding
            {
                label = lamp.displayName,
                lamp = lamp
            });
        }
    }

    // ============================================================
    // 구독
    // ============================================================

    private IEnumerator SubscribeWhenReady()
    {
        float waited = 0f;

        // MXRequester.Awake가 아직 안 돌았을 수 있으므로 잠시 기다린다.
        while (MXRequester.Get == null && waited < waitForRequesterSeconds)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (MXRequester.Get == null)
        {
            Debug.LogError(
                $"[{name}] MXRequester를 찾지 못했습니다. " +
                $"씬에 MXRequester가 있는지, Script Execution Order를 확인하세요.", this);
            yield break;
        }

        SubscribeAll();
    }

    private void SubscribeAll()
    {
        int ok = 0;

        foreach (LampBinding b in lamps)
        {
            if (b == null || b.subscribed) continue;

            if (b.lamp == null)
            {
                Debug.LogWarning($"[{name}] '{b.label}'에 SignalLamp가 지정되지 않았습니다.", this);
                continue;
            }

            string addr = ResolveAddress(b);
            if (string.IsNullOrEmpty(addr))
            {
                Debug.LogWarning(
                    $"[{name}] '{b.label}'의 디바이스 주소가 비어 있습니다. " +
                    $"Device의 address를 채우거나 SignalLamp의 Device Name을 입력하세요.", this);
                continue;
            }

            // ★ 클로저 주의 ★
            // 반복 변수를 그대로 캡처하면 모든 핸들러가 마지막 요소를 가리키게 된다.
            // 반드시 지역 변수에 복사한 뒤 캡처한다.
            LampBinding captured = b;

            captured.resolvedAddress = addr;
            captured.handler = value => OnDeviceValueChanged(captured, value);

            // AddDeviceAddress는 등록 직후 현재 값으로 콜백을 즉시 1회 호출한다.
            // 덕분에 초기 상태 동기화가 별도로 필요 없다.
            MXRequester.Get.AddDeviceAddress(addr, captured.handler);
            captured.subscribed = true;
            ok++;

            if (verboseLog)
                Debug.Log($"[{name}] 구독 등록: {addr} → {captured.lamp.name}", this);
        }

        if (verboseLog)
            Debug.Log($"[{name}] 램프 {ok}개 구독 완료", this);
    }

    private void UnsubscribeAll()
    {
        if (MXRequester.Get == null) return;

        foreach (LampBinding b in lamps)
        {
            if (b == null || !b.subscribed) continue;
            if (string.IsNullOrEmpty(b.resolvedAddress) || b.handler == null) continue;

            MXRequester.Get.RemoveDeviceAddress(b.resolvedAddress, b.handler);

            b.subscribed = false;
            b.handler = null;
        }
    }

    private string ResolveAddress(LampBinding b)
    {
        if (b.device != null && b.device.useDevice && !string.IsNullOrEmpty(b.device.address))
            return b.device.address;

        return b.lamp != null ? b.lamp.deviceName : null;
    }

    // ============================================================
    // 신호 처리
    // ============================================================

    private void OnDeviceValueChanged(LampBinding b, short value)
    {
        if (b == null || b.lamp == null) return;

        b.lastValue = value;

        bool on = (value != 0);
        if (b.invert) on = !on;

        if (on && b.blinkWhenOn)
            b.lamp.SetBlink(true);
        else
            b.lamp.SetOn(on);

        if (verboseLog)
            Debug.Log($"[{name}] {b.resolvedAddress} = {value} → {b.lamp.name}", this);
    }

    // ============================================================
    // 유틸
    // ============================================================

    /// <summary>코드에서 직접 값을 밀어넣고 싶을 때 (테스트용)</summary>
    public void SetSignal(string address, short value)
    {
        foreach (LampBinding b in lamps)
        {
            if (b == null) continue;

            string addr = b.resolvedAddress ?? ResolveAddress(b);
            if (!string.Equals(addr, address, StringComparison.OrdinalIgnoreCase)) continue;

            OnDeviceValueChanged(b, value);
            return;
        }
    }

    public SignalLamp GetLamp(string address)
    {
        foreach (LampBinding b in lamps)
        {
            if (b == null) continue;

            string addr = b.resolvedAddress ?? ResolveAddress(b);
            if (string.Equals(addr, address, StringComparison.OrdinalIgnoreCase))
                return b.lamp;
        }
        return null;
    }

    // ============================================================
    // 에디터
    // ============================================================

#if UNITY_EDITOR
    [ContextMenu("자식에서 램프 다시 수집")]
    private void EditorCollect()
    {
        lamps.Clear();
        CollectFromChildren();
        UnityEditor.EditorUtility.SetDirty(this);

        Debug.Log($"[{name}] 램프 {lamps.Count}개 수집", this);
    }

    [ContextMenu("검증 / 주소 확인")]
    private void ValidateAddresses()
    {
        bool ok = true;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (LampBinding b in lamps)
        {
            if (b == null) continue;

            if (b.lamp == null)
            {
                Debug.LogWarning($"'{b.label}': SignalLamp 미지정", this);
                ok = false;
                continue;
            }

            string addr = ResolveAddress(b);
            if (string.IsNullOrEmpty(addr))
            {
                Debug.LogWarning($"'{b.label}': 주소 비어 있음", this);
                ok = false;
                continue;
            }

            if (!seen.Add(addr))
                Debug.Log($"'{b.label}': 주소 {addr} 중복 (문제는 아님. 같은 신호를 여러 램프에 표시 가능)", this);
        }

        if (ok) Debug.Log($"[{name}] 검증 통과. 램프 {lamps.Count}개", this);
    }

    [ContextMenu("TEST / 전부 점등")]
    private void TestAllOn()
    {
        foreach (LampBinding b in lamps)
            if (b?.lamp != null) b.lamp.SetOn(true);
    }

    [ContextMenu("TEST / 전부 소등")]
    private void TestAllOff()
    {
        foreach (LampBinding b in lamps)
            if (b?.lamp != null) b.lamp.SetOn(false);
    }
#endif
}