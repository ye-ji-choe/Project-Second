using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// 숫자키로 카메라 시점을 전환한다.
/// Legacy Input Manager와 New Input System 양쪽 모두에서 동작한다.
///
/// [배치] CameraViewpointManager와 같은 오브젝트에 붙이면 된다.
///
/// [주의] CameraViewpointManager의 Enable Number Key Shortcuts는 꺼두세요.
///        둘 다 켜져 있으면 같은 키에 두 번 반응합니다.
///
/// [연동] ProcessPanelController는 매니저의 전환 이벤트를 구독하므로,
///        키보드로 시점을 바꿔도 램프 패널이 자동으로 따라 바뀝니다.
/// </summary>
[AddComponentMenu("Camera/Viewpoint Keyboard Shortcuts")]
public class ViewpointKeyboardShortcuts : MonoBehaviour
{
    [Serializable]
    public class KeyMapping
    {
        [Tooltip("누를 숫자키. 1~9 또는 0")]
        [Range(0, 9)] public int number = 1;

        [Tooltip("이동할 시점 인덱스 (0부터)")]
        public int viewpointIndex = 0;
    }

    // ============================================================
    // 인스펙터
    // ============================================================

    [Header("연결")]
    [Tooltip("비워두면 같은 오브젝트 → 씬 순으로 자동 탐색한다.")]
    public CameraViewpointManager manager;

    [Header("매핑 방식")]
    [Tooltip("켜면 숫자키 1 → 시점 0, 2 → 시점 1 … 순서로 자동 매핑한다.\n" +
             "끄면 아래 Custom Mappings만 사용한다.")]
    public bool autoMapSequential = true;

    [Tooltip("자동 매핑을 쓰지 않거나, 특정 키만 다르게 지정하고 싶을 때. " +
             "여기에 등록된 숫자는 자동 매핑보다 우선한다.")]
    public List<KeyMapping> customMappings = new List<KeyMapping>();

    [Header("입력 옵션")]
    [Tooltip("상단 숫자열(1~0)을 사용한다.")]
    public bool useNumberRow = true;

    [Tooltip("키패드 숫자(Numpad 1~0)도 사용한다.")]
    public bool useNumpad = true;

    [Tooltip("Ctrl / Alt / Shift가 눌린 상태에서는 무시한다. " +
             "Ctrl+1 같은 다른 단축키와 겹치지 않게 해준다.")]
    public bool ignoreWhenModifierHeld = true;

    [Tooltip("입력 필드에 포커스가 있으면 무시한다. " +
             "UI에 텍스트 입력란이 있을 때 숫자를 치면 시점이 바뀌는 것을 막는다.")]
    public bool ignoreWhileTypingInInputField = true;

    [Header("추가 단축키")]
    [Tooltip("다음 시점으로 넘어가는 키. None이면 사용 안 함.")]
    public bool useNextPrevKeys = false;

    [Header("Debug")]
    public bool verboseLog = false;

    // ============================================================
    // 상태
    // ============================================================

    private readonly Dictionary<int, int> resolvedMap = new Dictionary<int, int>();

    private void Awake()
    {
        if (manager == null)
            manager = GetComponent<CameraViewpointManager>();

        if (manager == null)
        {
#if UNITY_2023_1_OR_NEWER
            manager = UnityEngine.Object.FindFirstObjectByType<CameraViewpointManager>();
#else
            manager = UnityEngine.Object.FindObjectOfType<CameraViewpointManager>();
#endif
        }

        if (manager == null)
        {
            Debug.LogError($"[{name}] CameraViewpointManager를 찾지 못했습니다.", this);
            enabled = false;
            return;
        }

        // 매니저의 내장 단축키와 중복되지 않도록 꺼준다.
        if (manager.enableNumberKeyShortcuts)
        {
            manager.enableNumberKeyShortcuts = false;
            Debug.Log(
                $"[{name}] CameraViewpointManager의 내장 숫자키 기능을 껐습니다. " +
                $"(중복 입력 방지)", this);
        }

        BuildMap();
    }

    /// <summary>숫자키 → 시점 인덱스 매핑표를 만든다.</summary>
    private void BuildMap()
    {
        resolvedMap.Clear();

        if (autoMapSequential)
        {
            // 1키 → 시점 0, 2키 → 시점 1 … 9키 → 시점 8, 0키 → 시점 9
            for (int n = 1; n <= 9; n++)
                resolvedMap[n] = n - 1;

            resolvedMap[0] = 9;
        }

        // 커스텀 매핑이 자동 매핑을 덮어쓴다.
        foreach (KeyMapping m in customMappings)
        {
            if (m == null) continue;
            resolvedMap[m.number] = m.viewpointIndex;
        }
    }

    // ============================================================
    // 입력 처리
    // ============================================================

    private void Update()
    {
        if (manager == null) return;
        if (ignoreWhenModifierHeld && IsModifierHeld()) return;
        if (ignoreWhileTypingInInputField && IsTypingInInputField()) return;

        // 0~9 확인
        for (int n = 0; n <= 9; n++)
        {
            if (!WasNumberPressed(n)) continue;

            if (!resolvedMap.TryGetValue(n, out int index))
            {
                if (verboseLog)
                    Debug.Log($"[{name}] 숫자키 {n}에 매핑된 시점이 없습니다.", this);
                continue;
            }

            if (index < 0 || index >= manager.Count)
            {
                if (verboseLog)
                    Debug.Log(
                        $"[{name}] 숫자키 {n} → 시점 [{index}]는 등록 범위를 벗어납니다. " +
                        $"(등록된 시점 {manager.Count}개)", this);
                continue;
            }

            if (verboseLog)
                Debug.Log($"[{name}] 숫자키 {n} → 시점 [{index}] {manager.GetName(index)}", this);

            manager.GoTo(index);
            return;   // 한 프레임에 하나만 처리
        }

        if (useNextPrevKeys)
        {
            if (WasNextPressed()) manager.GoToNext();
            else if (WasPrevPressed()) manager.GoToPrevious();
        }
    }

    // ============================================================
    // 입력 백엔드 추상화
    // ============================================================
    // New Input System과 Legacy Input Manager 중 사용 가능한 쪽을 쓴다.
    // Player Settings의 Active Input Handling이 "Both"면 두 심볼이 모두
    // 정의되므로, New Input System을 우선하고 Legacy는 else로 처리한다.

    private bool WasNumberPressed(int n)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return false;

        if (useNumberRow)
        {
            // Key 열거형 순서: Digit1 … Digit9, Digit0
            Key k = (n == 0) ? Key.Digit0 : (Key)((int)Key.Digit1 + (n - 1));

            KeyControl ctrl = kb[k];
            if (ctrl != null && ctrl.wasPressedThisFrame) return true;
        }

        if (useNumpad)
        {
            Key k = (n == 0) ? Key.Numpad0 : (Key)((int)Key.Numpad1 + (n - 1));

            KeyControl ctrl = kb[k];
            if (ctrl != null && ctrl.wasPressedThisFrame) return true;
        }

        return false;

#elif ENABLE_LEGACY_INPUT_MANAGER
        if (useNumberRow && Input.GetKeyDown(KeyCode.Alpha0 + n)) return true;
        if (useNumpad && Input.GetKeyDown(KeyCode.Keypad0 + n)) return true;
        return false;
#else
        return false;
#endif
    }

    private bool IsModifierHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return false;

        return kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed
            || kb.leftAltKey.isPressed || kb.rightAltKey.isPressed
            || kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
            || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)
            || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
        return false;
#endif
    }

    private bool WasNextPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.pageDownKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.PageDown);
#else
        return false;
#endif
    }

    private bool WasPrevPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.pageUpKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.PageUp);
#else
        return false;
#endif
    }

    // ============================================================
    // 입력 필드 포커스 확인
    // ============================================================

    /// <summary>
    /// 현재 선택된 UI가 입력 필드인지 확인한다.
    /// UI.InputField와 TMP_InputField를 모두 잡기 위해 타입 이름으로 판별한다.
    /// (TextMeshPro에 대한 컴파일 의존성을 만들지 않기 위함)
    /// </summary>
    private bool IsTypingInInputField()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;

        GameObject sel = es.currentSelectedGameObject;
        if (sel == null) return false;

        foreach (Component c in sel.GetComponents<Component>())
        {
            if (c == null) continue;
            if (c.GetType().Name.Contains("InputField")) return true;
        }

        return false;
    }

    // ============================================================
    // 에디터
    // ============================================================

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) BuildMap();
    }

    [ContextMenu("현재 매핑 출력")]
    private void DumpMapping()
    {
        BuildMap();

        var sb = new System.Text.StringBuilder("=== 숫자키 매핑 ===\n");

        for (int n = 1; n <= 9; n++) AppendLine(sb, n);
        AppendLine(sb, 0);

        Debug.Log(sb.ToString(), this);
    }

    private void AppendLine(System.Text.StringBuilder sb, int n)
    {
        if (!resolvedMap.TryGetValue(n, out int idx))
        {
            sb.AppendLine($"  [{n}] → (매핑 없음)");
            return;
        }

        string state;
        if (manager == null) state = "매니저 없음";
        else if (idx < 0 || idx >= manager.Count) state = "범위 초과";
        else state = manager.GetName(idx);

        sb.AppendLine($"  [{n}] → 시점 {idx} : {state}");
    }
#endif
}