using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 버튼에 붙이면 CameraViewpointManager의 시점 전환에 자동으로 연결된다.
/// OnClick을 인스펙터에서 일일이 연결할 필요가 없어진다.
///
/// [사용법]
///  1. Canvas 아래 Button을 만든다.
///  2. 이 컴포넌트를 Button에 붙인다.
///  3. Manager를 지정하고 Viewpoint Index를 입력한다. (비워두면 자동 탐색)
/// </summary>
[RequireComponent(typeof(Button))]
[AddComponentMenu("Camera/Viewpoint Button")]
public class ViewpointButton : MonoBehaviour
{
    [Tooltip("비워두면 씬에서 자동으로 찾는다.")]
    public CameraViewpointManager manager;

    [Tooltip("전환할 시점의 인덱스 (0부터 시작)")]
    public int viewpointIndex = 0;

    [Tooltip("버튼 라벨을 시점 이름으로 자동 설정한다. (UI.Text 전용)")]
    public bool autoSetLabel = true;

    [Tooltip("현재 선택된 시점일 때 버튼 색을 바꾼다.")]
    public bool highlightWhenActive = true;

    public Color activeColor = new Color(0.35f, 0.7f, 1f);

    private Button button;
    private Color defaultColor;

    private void Awake()
    {
        button = GetComponent<Button>();
        defaultColor = button.image != null ? button.image.color : Color.white;

        if (manager == null)
        {
#if UNITY_2023_1_OR_NEWER
            manager = Object.FindFirstObjectByType<CameraViewpointManager>();
#else
            manager = Object.FindObjectOfType<CameraViewpointManager>();
#endif
        }

        if (manager == null)
        {
            Debug.LogError($"[{name}] CameraViewpointManager를 찾지 못했습니다.", this);
            button.interactable = false;
            return;
        }

        button.onClick.AddListener(OnClicked);
    }

    private void Start()
    {
        if (manager == null) return;

        if (autoSetLabel)
        {
            string label = manager.GetName(viewpointIndex);
            if (!string.IsNullOrEmpty(label))
            {
                Text txt = GetComponentInChildren<Text>();
                if (txt != null) txt.text = label;
                // TextMeshPro를 쓰는 경우 라벨은 직접 입력하세요.
            }
        }

        if (highlightWhenActive)
        {
            manager.OnTransitionComplete += HandleTransitionComplete;
            Refresh(manager.CurrentIndex);
        }
    }

    private void OnDestroy()
    {
        if (manager != null && highlightWhenActive)
            manager.OnTransitionComplete -= HandleTransitionComplete;
    }

    private void OnClicked()
    {
        if (manager != null)
            manager.GoTo(viewpointIndex);
    }

    private void HandleTransitionComplete(int index) => Refresh(index);

    private void Refresh(int activeIndex)
    {
        if (button == null || button.image == null) return;

        button.image.color = (activeIndex == viewpointIndex) ? activeColor : defaultColor;
    }
}