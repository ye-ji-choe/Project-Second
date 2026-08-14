using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// INPUT_OUTPUT 하위의 패널들(공정_1~4) 중 "현재 활성화된 패널"만 토글로 숨기고 보여준다.
///
/// 숨기기 버튼을 누르면: 지금 켜져 있는 패널을 기억해두고 끈다. (라벨 → "보여짐")
/// 다시 누르면: 기억해둔 패널만 다시 켠다. (라벨 → "숨기기")
///
/// 공정_1~4 버튼(processButtons)을 누르면: 숨김 상태를 자동으로 해제한다.
/// (그 버튼이 어떤 패널을 켜는지는 이 스크립트가 몰라도 된다.
///  ViewpointButton 등 기존 로직이 알아서 해당 패널을 켜주므로,
///  여기서는 "숨김 상태였다면 그 표시만 리셋"하는 역할만 한다.)
///
/// [배치]
///   숨기기 버튼 오브젝트 (Button)
///   └── PanelVisibilityToggle (이 컴포넌트)
///
/// [설정]
///   ProcessPanelsRoot 에 "INPUT_OUTPUT" 오브젝트를 드래그.
///   ProcessButtons 에 공정_1, 공정_2, 공정_3, 공정_4 버튼들을 드래그.
/// </summary>
[RequireComponent(typeof(Button))]
[AddComponentMenu("UI/Panel Visibility Toggle")]
public class PanelVisibilityToggle : MonoBehaviour
{
    [Tooltip("이 아래에 있는 패널들의 활성 상태를 토글 대상으로 삼는다. INPUT_OUTPUT을 드래그. 비워두면 이름으로 자동 검색.")]
    public Transform processPanelsRoot;

    [Tooltip("processPanelsRoot을 비워뒀을 때 자동으로 찾을 오브젝트 이름.")]
    public string processPanelsRootName = "INPUT_OUTPUT";

    [Tooltip("직계 자식만 볼지, 손자까지 다 뒤질지. 보통 직계 자식(공정_1 등)만 보면 된다.")]
    public bool directChildrenOnly = true;

    [Header("공정 전환 버튼 (숨김 자동 해제용)")]
    [Tooltip("공정_1~4 같은 '패널 전환' 버튼들. 이 버튼들을 누르면 숨김 상태가 자동으로 해제된다. " +
             "이 버튼들이 실제로 패널을 켜고 끄는 로직(ViewpointButton 등)은 건드리지 않는다.")]
    public List<Button> processButtons = new List<Button>();

    [Header("버튼 라벨 (선택)")]
    [Tooltip("버튼 안의 Text. 있으면 상태에 따라 문구를 바꿔준다.")]
    public Text buttonLabel;
    public string showLabel = "숨기기";
    public string hideLabel = "표시하기";

    [Header("Debug")]
    public bool verboseLog = false;

    private Button button;
    private bool isHidden = false;

    // 숨기기 직전에 켜져 있던 패널들만 기록.
    private readonly List<GameObject> hiddenPanels = new List<GameObject>();

    private void Awake()
    {
        button = GetComponent<Button>();

        if (processPanelsRoot == null && !string.IsNullOrEmpty(processPanelsRootName))
        {
            GameObject found = GameObject.Find(processPanelsRootName);
            if (found != null) processPanelsRoot = found.transform;
        }

        if (processPanelsRoot == null)
        {
            Debug.LogError($"[{name}] {processPanelsRootName} 루트를 찾지 못했습니다. processPanelsRoot을 직접 연결하세요.", this);
            button.interactable = false;
            return;
        }

        button.onClick.AddListener(OnHideButtonClicked);

        // 공정 버튼들이 눌리면 숨김 상태를 해제하도록 구독한다.
        // 이 버튼들의 원래 onClick(패널 전환 로직)은 그대로 유지되고,
        // 여기서는 리스너를 "추가"만 하므로 기존 동작에 영향 없다.
        foreach (Button pb in processButtons)
        {
            if (pb == null) continue;
            pb.onClick.AddListener(OnProcessButtonClicked);
        }

        RefreshLabel();
    }

    private void OnHideButtonClicked()
    {
        if (processPanelsRoot == null) return;

        if (verboseLog)
            Debug.Log($"[{name}] 숨기기 버튼 클릭됨. 현재 isHidden={isHidden}", this);

        if (!isHidden)
            HideActivePanels();
        else
            RestoreHiddenPanels();

        isHidden = !isHidden;
        RefreshLabel();
    }

    /// <summary>
    /// 공정_1~4 버튼 중 하나가 눌렸을 때 호출된다.
    /// 그 버튼이 어떤 패널을 켜는지는 몰라도 되고,
    /// "숨김 상태였다면 이제는 아니다"라는 사실만 반영한다.
    /// </summary>
    private void OnProcessButtonClicked()
    {
        if (!isHidden)
            return; // 숨긴 상태가 아니었다면 할 일 없음

        if (verboseLog)
            Debug.Log($"[{name}] 공정 버튼 클릭됨 → 숨김 상태 자동 해제", this);

        // 주의: 여기서 패널을 직접 켜지 않는다.
        // 방금 눌린 공정 버튼(ViewpointButton 등)이 이미 알아서
        // "자신의 패널만 켜고 나머지는 끄는" 로직을 실행했을 것이므로,
        // 이 스크립트는 그저 내부 상태와 라벨만 리셋한다.
        hiddenPanels.Clear();
        isHidden = false;
        RefreshLabel();
    }

    /// <summary>지금 켜져 있는 패널들을 스냅샷으로 저장하고 전부 끈다.</summary>
    private void HideActivePanels()
    {
        hiddenPanels.Clear();

        foreach (Transform child in GetPanelTransforms())
        {
            if (child.gameObject.activeSelf)
            {
                hiddenPanels.Add(child.gameObject);
                child.gameObject.SetActive(false);

                if (verboseLog)
                    Debug.Log($"[{name}] 숨김: {child.name}", this);
            }
        }

        if (hiddenPanels.Count == 0 && verboseLog)
        {
            Debug.LogWarning(
                $"[{name}] 숨기기를 눌렀지만 활성화된 패널이 하나도 없었습니다. " +
                $"processPanelsRoot='{processPanelsRoot.name}' 설정을 확인하세요.", this);
        }
    }

    /// <summary>숨기기 시점에 켜져 있던 패널들만 다시 켠다.</summary>
    private void RestoreHiddenPanels()
    {
        foreach (GameObject panel in hiddenPanels)
        {
            if (panel != null)
            {
                panel.SetActive(true);

                if (verboseLog)
                    Debug.Log($"[{name}] 복원: {panel.name}", this);
            }
        }
        hiddenPanels.Clear();
    }

    private IEnumerable<Transform> GetPanelTransforms()
    {
        if (directChildrenOnly)
        {
            foreach (Transform child in processPanelsRoot)
                yield return child;
        }
        else
        {
            foreach (Transform child in processPanelsRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child != processPanelsRoot)
                    yield return child;
            }
        }
    }

    private void RefreshLabel()
    {
        if (buttonLabel == null) return;
        buttonLabel.text = isHidden ? hideLabel : showLabel;
    }
}