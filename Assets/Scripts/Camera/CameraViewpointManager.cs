using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카메라 시점을 슬롯에 저장해 두고, UI 버튼으로 전환하는 매니저.
///
/// [사용법]
///  1. 빈 GameObject에 이 컴포넌트를 붙인다.
///  2. Target Camera에 전환시킬 카메라를 넣는다.
///  3. Scene 뷰에서 카메라를 원하는 위치로 옮긴 뒤,
///     Inspector의 [현재 카메라 위치 저장] 버튼을 누른다.
///  4. UI 버튼의 OnClick에 GoTo(int)를 연결하거나, ViewpointButton 컴포넌트를 쓴다.
///
/// [주의] Play 모드에서 저장한 값은 Play 종료 시 사라진다.
///        시점 등록은 Edit 모드에서 할 것.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Camera/Camera Viewpoint Manager")]
public class CameraViewpointManager : MonoBehaviour
{
    // ============================================================
    // 데이터
    // ============================================================

    [Serializable]
    public class Viewpoint
    {
        [Tooltip("UI 버튼 라벨 등으로 쓸 이름")]
        public string name = "시점";

        public Vector3 position;
        public Vector3 eulerAngles;

        [Tooltip("원근 카메라의 화각")]
        public float fieldOfView = 60f;

        public bool orthographic = false;
        public float orthographicSize = 5f;

        [Tooltip("한 번이라도 저장된 적이 있는지")]
        public bool captured = false;
    }

    // ============================================================
    // 인스펙터
    // ============================================================

    [Header("대상 카메라")]
    [Tooltip("비워두면 실행 시 Camera.main을 자동으로 찾는다.")]
    public Camera targetCamera;

    [Header("전환 설정")]
    [Tooltip("체크 해제 시 즉시 순간이동한다.")]
    public bool smoothTransition = true;

    [Min(0.01f)]
    public float transitionDuration = 0.6f;

    [Tooltip("전환 가속/감속 곡선")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("화각(FOV)도 함께 보간할지")]
    public bool blendFieldOfView = true;

    [Tooltip("Time.timeScale의 영향을 받지 않게 한다. (일시정지 중에도 전환 가능)")]
    public bool useUnscaledTime = true;

    [Header("카메라 조작 스크립트 제어")]
    [Tooltip("전환 중 잠시 꺼둘 스크립트들. " +
             "궤도 회전(orbit)이나 자유 이동 스크립트가 카메라를 되돌리는 것을 막는다.")]
    public Behaviour[] cameraControllers;

    [Tooltip("전환이 끝난 뒤 위 스크립트들을 다시 켤지. " +
             "끄고 유지하려면 체크 해제.")]
    public bool reenableControllersAfterMove = true;

    [Header("시점 목록")]
    public List<Viewpoint> viewpoints = new List<Viewpoint>();

    [Header("키보드 단축키 (선택)")]
    [Tooltip("숫자키 1~9로 해당 인덱스의 시점으로 전환한다.")]
    public bool enableNumberKeyShortcuts = false;

    [Header("Debug")]
    public bool verboseLog = false;

    // ============================================================
    // 상태
    // ============================================================

    /// <summary>전환이 시작될 때 (시점 인덱스)</summary>
    public event Action<int> OnTransitionStart;

    /// <summary>전환이 끝났을 때 (시점 인덱스)</summary>
    public event Action<int> OnTransitionComplete;

    private Coroutine transitionRoutine;
    private int currentIndex = -1;

    /// <summary>현재 적용된 시점 인덱스. 없으면 -1.</summary>
    public int CurrentIndex => currentIndex;

    /// <summary>전환 진행 중인지</summary>
    public bool IsTransitioning => transitionRoutine != null;

    public int Count => viewpoints != null ? viewpoints.Count : 0;

    // ============================================================
    // 라이프사이클
    // ============================================================

    private void Awake()
    {
        ResolveCamera();
    }

#if ENABLE_LEGACY_INPUT_MANAGER
    private void Update()
    {
        if (!enableNumberKeyShortcuts) return;

        for (int i = 0; i < 9 && i < Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                GoTo(i);
                break;
            }
        }
    }
#endif

    /// <summary>대상 카메라를 확정한다. 비어 있으면 Camera.main을 쓴다.</summary>
    public Camera ResolveCamera()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogError(
                $"[{name}] 대상 카메라를 찾지 못했습니다. " +
                $"Target Camera를 지정하거나 카메라에 MainCamera 태그를 붙이세요.", this);
        }

        return targetCamera;
    }

    // ============================================================
    // 저장
    // ============================================================

    /// <summary>현재 카메라 상태를 Viewpoint로 캡처한다.</summary>
    public static Viewpoint Capture(Camera cam, string viewName)
    {
        if (cam == null) return null;

        return new Viewpoint
        {
            name = viewName,
            position = cam.transform.position,
            eulerAngles = cam.transform.eulerAngles,
            fieldOfView = cam.fieldOfView,
            orthographic = cam.orthographic,
            orthographicSize = cam.orthographicSize,
            captured = true
        };
    }

    /// <summary>지정 슬롯에 현재 카메라 상태를 덮어쓴다.</summary>
    public void SaveCurrent(int index)
    {
        if (!IsValidIndex(index)) return;
        if (ResolveCamera() == null) return;

        Viewpoint vp = viewpoints[index];
        Transform t = targetCamera.transform;

        vp.position = t.position;
        vp.eulerAngles = t.eulerAngles;
        vp.fieldOfView = targetCamera.fieldOfView;
        vp.orthographic = targetCamera.orthographic;
        vp.orthographicSize = targetCamera.orthographicSize;
        vp.captured = true;

        Log($"시점 저장 [{index}] {vp.name} → pos {vp.position}, rot {vp.eulerAngles}");
    }

    /// <summary>현재 카메라 상태를 새 슬롯으로 추가한다.</summary>
    public int AddCurrentAsNew(string viewName = null)
    {
        if (ResolveCamera() == null) return -1;

        Viewpoint vp = Capture(targetCamera, viewName ?? $"시점 {Count + 1}");
        viewpoints.Add(vp);

        Log($"시점 추가 [{Count - 1}] {vp.name}");
        return Count - 1;
    }

    // ============================================================
    // 전환
    // ============================================================

    /// <summary>지정 인덱스의 시점으로 전환한다. UI 버튼 OnClick에 연결할 메서드.</summary>
    public void GoTo(int index)
    {
        if (!IsValidIndex(index)) return;

        Viewpoint vp = viewpoints[index];
        if (!vp.captured)
        {
            Debug.LogWarning($"[{name}] 시점 [{index}] '{vp.name}'은(는) 아직 저장된 적이 없습니다.", this);
            return;
        }

        if (ResolveCamera() == null) return;

        StopTransition();
        SetControllersEnabled(false);
        OnTransitionStart?.Invoke(index);

        if (!smoothTransition || !Application.isPlaying)
        {
            ApplyImmediate(vp);
            currentIndex = index;
            if (reenableControllersAfterMove) SetControllersEnabled(true);
            OnTransitionComplete?.Invoke(index);
            Log($"시점 전환(즉시) → [{index}] {vp.name}");
            return;
        }

        transitionRoutine = StartCoroutine(TransitionRoutine(vp, index));
    }

    /// <summary>이름으로 전환한다.</summary>
    public void GoTo(string viewName)
    {
        int idx = IndexOf(viewName);
        if (idx < 0)
        {
            Debug.LogWarning($"[{name}] '{viewName}' 이름의 시점을 찾지 못했습니다.", this);
            return;
        }
        GoTo(idx);
    }

    /// <summary>보간 없이 즉시 이동한다.</summary>
    public void GoToInstant(int index)
    {
        if (!IsValidIndex(index)) return;
        if (ResolveCamera() == null) return;

        StopTransition();
        ApplyImmediate(viewpoints[index]);
        currentIndex = index;

        SetControllersEnabled(reenableControllersAfterMove);
        OnTransitionComplete?.Invoke(index);
    }

    /// <summary>다음 시점으로 순환 전환한다.</summary>
    public void GoToNext()
    {
        if (Count == 0) return;
        GoTo((currentIndex + 1 + Count) % Count);
    }

    /// <summary>이전 시점으로 순환 전환한다.</summary>
    public void GoToPrevious()
    {
        if (Count == 0) return;
        GoTo((currentIndex - 1 + Count) % Count);
    }

    /// <summary>진행 중인 전환을 중단한다.</summary>
    public void StopTransition()
    {
        if (transitionRoutine == null) return;

        StopCoroutine(transitionRoutine);
        transitionRoutine = null;
    }

    private IEnumerator TransitionRoutine(Viewpoint vp, int index)
    {
        Transform t = targetCamera.transform;

        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;
        float startFov = targetCamera.fieldOfView;
        float startOrtho = targetCamera.orthographicSize;

        Vector3 endPos = vp.position;
        Quaternion endRot = Quaternion.Euler(vp.eulerAngles);

        // 투영 방식이 다르면 보간이 무의미하므로 먼저 맞춘다.
        if (targetCamera.orthographic != vp.orthographic)
            targetCamera.orthographic = vp.orthographic;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, transitionDuration);

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float raw = Mathf.Clamp01(elapsed / duration);
            float k = ease != null ? ease.Evaluate(raw) : raw;

            t.position = Vector3.LerpUnclamped(startPos, endPos, k);
            t.rotation = Quaternion.SlerpUnclamped(startRot, endRot, k);

            if (blendFieldOfView)
            {
                if (targetCamera.orthographic)
                    targetCamera.orthographicSize = Mathf.LerpUnclamped(startOrtho, vp.orthographicSize, k);
                else
                    targetCamera.fieldOfView = Mathf.LerpUnclamped(startFov, vp.fieldOfView, k);
            }

            yield return null;
        }

        ApplyImmediate(vp);

        transitionRoutine = null;
        currentIndex = index;

        if (reenableControllersAfterMove) SetControllersEnabled(true);
        OnTransitionComplete?.Invoke(index);

        Log($"시점 전환 완료 → [{index}] {vp.name}");
    }

    private void ApplyImmediate(Viewpoint vp)
    {
        Transform t = targetCamera.transform;

        t.position = vp.position;
        t.eulerAngles = vp.eulerAngles;

        targetCamera.orthographic = vp.orthographic;
        targetCamera.fieldOfView = vp.fieldOfView;
        targetCamera.orthographicSize = vp.orthographicSize;
    }

    // ============================================================
    // 유틸
    // ============================================================

    private void SetControllersEnabled(bool value)
    {
        if (cameraControllers == null) return;

        foreach (Behaviour b in cameraControllers)
        {
            if (b != null) b.enabled = value;
        }
    }

    public bool IsValidIndex(int index)
    {
        if (index >= 0 && index < Count) return true;

        Debug.LogWarning($"[{name}] 시점 인덱스 {index}가 범위를 벗어났습니다. (등록된 시점: {Count}개)", this);
        return false;
    }

    public int IndexOf(string viewName)
    {
        for (int i = 0; i < Count; i++)
        {
            if (viewpoints[i] != null && viewpoints[i].name == viewName)
                return i;
        }
        return -1;
    }

    public string GetName(int index)
        => (index >= 0 && index < Count && viewpoints[index] != null) ? viewpoints[index].name : string.Empty;

    private void Log(string msg)
    {
        if (!verboseLog) return;
        Debug.Log($"[{name}] {msg}", this);
    }

    // ============================================================
    // Gizmo - 저장된 시점을 씬 뷰에 표시
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        if (viewpoints == null) return;

        for (int i = 0; i < viewpoints.Count; i++)
        {
            Viewpoint vp = viewpoints[i];
            if (vp == null || !vp.captured) continue;

            Quaternion rot = Quaternion.Euler(vp.eulerAngles);

            Gizmos.color = (i == currentIndex) ? Color.yellow : new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireSphere(vp.position, 0.15f);

            // 시선 방향
            Gizmos.DrawLine(vp.position, vp.position + rot * Vector3.forward * 0.8f);

            // 간단한 절두체 표시
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(vp.position, rot, Vector3.one);
            Gizmos.DrawFrustum(Vector3.zero, vp.fieldOfView, 1.2f, 0.05f, 1.6f);
            Gizmos.matrix = old;
        }
    }
}