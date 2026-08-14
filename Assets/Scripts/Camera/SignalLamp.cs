using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PLC 신호(센서 감지, 컨베이어 구동 등)를 표시하는 UI 램프.
/// Toggle 기반 UI와 Image 기반 UI를 모두 지원한다.
///
/// [Toggle 기반] — 이미 Toggle로 만들어 둔 경우
///   공정_1_컨베이어_구동센서_신호  (Toggle + 이 컴포넌트)
///   ├── Background (Image)   ← 배경색 틴트 대상
///   │   └── Checkmark (Image)← Toggle.isOn으로 표시/숨김
///   └── Label (Text)
///
/// [Image 기반] — 새로 만드는 경우
///   Lamp_X0B4  (이 컴포넌트)
///   ├── Dot (Image)
///   └── Label (Text)
///
/// [신호 연결]
///   MXRequester 디바이스 콜백에서 OnDeviceValueChanged(short)를 호출한다.
///   기존 PreciseStopSensorConnector가 OnForwardChanged(short)를 받는 것과 동일한 방식.
/// </summary>
[AddComponentMenu("Factory UI/Signal Lamp")]
public class SignalLamp : MonoBehaviour
{
    public enum LampState { Off, On, Blink, Fault }

    // ============================================================
    // 인스펙터
    // ============================================================

    [Header("표시 대상 (비워두면 자동 탐색)")]
    [Tooltip("Toggle로 만든 램프인 경우. 같은 오브젝트에 있으면 자동으로 잡힌다.")]
    public Toggle toggleLamp;

    [Tooltip("색을 바꿀 이미지. Toggle이면 보통 Background.")]
    public Graphic tintTarget;

    [Tooltip("설명 라벨. TextMeshPro를 쓰면 여기는 비워두고 직접 입력.")]
    public Text labelText;

    [Header("표시 내용")]
    [Tooltip("예: 컨베이어 구동, 화분 감지")]
    public string displayName = "";

    [Tooltip("PLC 디바이스 이름. 예: X0B4, Y0C0")]
    public string deviceName = "";

    [Tooltip("라벨에 디바이스 이름도 함께 표시. 예: 화분 감지 (X0B4)")]
    public bool showDeviceInLabel = true;

    [Header("Toggle 동작")]
    [Tooltip("Toggle의 체크 표시를 신호에 따라 켜고 끈다.")]
    public bool driveToggleIsOn = true;

    [Tooltip("램프는 표시 전용이므로 사용자가 클릭하지 못하게 막는다. " +
             "해제하면 수동 테스트용으로 클릭할 수 있다.")]
    public bool forceNonInteractable = true;

    [Header("색상")]
    [Tooltip("배경 이미지의 색을 신호에 따라 바꾼다.")]
    public bool tintBackground = true;

    public Color offColor = new Color(0.22f, 0.24f, 0.27f);
    public Color onColor = new Color(0.30f, 0.90f, 0.40f);
    public Color blinkColor = new Color(1.00f, 0.80f, 0.20f);
    public Color faultColor = new Color(0.95f, 0.30f, 0.30f);

    [Header("점멸")]
    [Min(0.05f)] public float blinkInterval = 0.4f;

    [Tooltip("Time.timeScale의 영향을 받지 않게 한다.")]
    public bool useUnscaledTime = true;

    [Header("동작")]
    [Tooltip("신호를 반전시킨다. b접점 센서 등에 사용.")]
    public bool invert = false;

    public LampState initialState = LampState.Off;

    [Header("Debug")]
    [SerializeField] private LampState currentState = LampState.Off;
    [SerializeField] private int lastValue = 0;

    // ============================================================
    // 상태
    // ============================================================

    private float blinkTimer = 0f;
    private bool blinkPhase = false;
    private bool initialized = false;

    public LampState State => currentState;
    public bool IsOn => currentState == LampState.On;

    // ============================================================
    // 라이프사이클
    // ============================================================

    private void Awake() => Initialize();

    private void OnEnable()
    {
        Initialize();
        Redraw();
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        AutoResolveReferences();

        if (toggleLamp != null && forceNonInteractable)
            toggleLamp.interactable = false;

        currentState = initialState;
        ApplyLabel();
    }

    private void AutoResolveReferences()
    {
        if (toggleLamp == null)
            toggleLamp = GetComponent<Toggle>();

        if (tintTarget == null)
        {
            // Toggle이면 targetGraphic(보통 Background)을 쓴다.
            if (toggleLamp != null && toggleLamp.targetGraphic != null)
                tintTarget = toggleLamp.targetGraphic;
            else
                tintTarget = GetComponentInChildren<Image>(true);
        }

        if (labelText == null)
            labelText = GetComponentInChildren<Text>(true);
    }

    private void Update()
    {
        if (currentState != LampState.Blink && currentState != LampState.Fault)
            return;

        blinkTimer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (blinkTimer < blinkInterval) return;

        blinkTimer = 0f;
        blinkPhase = !blinkPhase;
        Redraw();
    }

    // ============================================================
    // 공개 API — 여기에 PLC 신호를 연결한다
    // ============================================================

    /// <summary>MXRequester 디바이스 콜백에 연결. 0이면 소등, 그 외는 점등.</summary>
    public void OnDeviceValueChanged(short value) => SetValue(value);

    public void SetValue(int value)
    {
        lastValue = value;
        SetOn(value != 0);
    }

    public void SetOn(bool on)
    {
        if (invert) on = !on;
        SetState(on ? LampState.On : LampState.Off);
    }

    /// <summary>노란색 점멸. 컨베이어 "구동 중" 표시에 적합.</summary>
    public void SetBlink(bool blink) => SetState(blink ? LampState.Blink : LampState.Off);

    /// <summary>적색 점멸. 이상 상태.</summary>
    public void SetFault(bool fault) => SetState(fault ? LampState.Fault : LampState.Off);

    public void SetState(LampState state)
    {
        Initialize();

        if (currentState == state) return;

        currentState = state;
        blinkTimer = 0f;
        blinkPhase = true;   // 점멸은 켜진 상태부터 시작

        Redraw();
    }

    public void SetLabel(string text)
    {
        displayName = text;
        ApplyLabel();
    }

    // ============================================================
    // 그리기
    // ============================================================

    private void Redraw()
    {
        bool lit = IsLit();

        // ── Toggle 체크 표시 ──
        if (toggleLamp != null && driveToggleIsOn)
        {
            // ★ SetIsOnWithoutNotify를 써야 한다 ★
            // 그냥 isOn을 대입하면 OnValueChanged가 발동해서
            // Inspector에 연결해 둔 다른 동작(SetActive 등)까지 같이 실행된다.
            toggleLamp.SetIsOnWithoutNotify(lit);
        }

        // ── 배경 색상 ──
        if (tintBackground && tintTarget != null)
            tintTarget.color = ResolveColor();
    }

    private bool IsLit()
    {
        switch (currentState)
        {
            case LampState.On: return true;
            case LampState.Blink:
            case LampState.Fault: return blinkPhase;
            default: return false;
        }
    }

    private Color ResolveColor()
    {
        switch (currentState)
        {
            case LampState.On: return onColor;
            case LampState.Blink: return blinkPhase ? blinkColor : offColor;
            case LampState.Fault: return blinkPhase ? faultColor : offColor;
            default: return offColor;
        }
    }

    private void ApplyLabel()
    {
        if (labelText == null || string.IsNullOrEmpty(displayName)) return;

        labelText.text = (showDeviceInLabel && !string.IsNullOrEmpty(deviceName))
            ? $"{displayName}  ({deviceName})"
            : displayName;
    }

    // ============================================================
    // 에디터
    // ============================================================

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoResolveReferences();
        ApplyLabel();

        if (Application.isPlaying) return;

        // 에디터에서 미리보기
        if (toggleLamp != null && forceNonInteractable)
            toggleLamp.interactable = false;

        bool preview = (initialState == LampState.On);

        if (toggleLamp != null && driveToggleIsOn)
            toggleLamp.SetIsOnWithoutNotify(preview);

        if (tintBackground && tintTarget != null)
            tintTarget.color = preview ? onColor : offColor;
    }

    [ContextMenu("TEST / 점등")]
    private void TestOn() => SetState(LampState.On);

    [ContextMenu("TEST / 소등")]
    private void TestOff() => SetState(LampState.Off);

    [ContextMenu("TEST / 점멸 (구동 중)")]
    private void TestBlink() => SetState(LampState.Blink);

    [ContextMenu("TEST / 이상 (적색 점멸)")]
    private void TestFault() => SetState(LampState.Fault);
#endif
}