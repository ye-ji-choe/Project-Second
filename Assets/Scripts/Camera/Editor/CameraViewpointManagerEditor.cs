using UnityEditor;
using UnityEngine;

/// <summary>
/// CameraViewpointManager의 Inspector에 저장/이동 버튼을 그려주는 에디터 확장.
///
/// 이 파일은 반드시 "Editor" 라는 이름의 폴더 안에 넣어야 한다. 
///   예: Assets/Scripts/Camera/Editor/CameraViewpointManagerEditor.cs
///   그렇지 않으면 빌드 시 UnityEditor 네임스페이스를 찾지 못해 컴파일 에러가 난다.
/// </summary>
[CustomEditor(typeof(CameraViewpointManager))]
public class CameraViewpointManagerEditor : Editor
{
    private CameraViewpointManager mgr;
    private SerializedProperty viewpointsProp;

    private void OnEnable()
    {
        mgr = (CameraViewpointManager)target;
        viewpointsProp = serializedObject.FindProperty("viewpoints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 시점 목록을 뺀 나머지 필드는 기본 방식으로 그린다.
        DrawPropertiesExcluding(serializedObject, "m_Script", "viewpoints");

        EditorGUILayout.Space(10);
        DrawPlayModeWarning();
        DrawViewpointList();
        DrawBottomButtons();

        serializedObject.ApplyModifiedProperties();
    }

    // ============================================================

    private void DrawPlayModeWarning()
    {
        if (!Application.isPlaying) return;

        EditorGUILayout.HelpBox(
            "Play 모드입니다. 지금 저장한 시점은 Play를 끄면 사라집니다.\n" +
            "시점 등록은 Edit 모드에서 하세요.\n\n" +
            "Play 중에 찾은 좋은 구도를 남기려면: 컴포넌트 우클릭 → Copy Component → " +
            "Play 종료 후 우클릭 → Paste Component Values",
            MessageType.Warning);

        EditorGUILayout.Space(6);
    }

    private void DrawViewpointList()
    {
        EditorGUILayout.LabelField($"시점 목록 ({viewpointsProp.arraySize}개)", EditorStyles.boldLabel);

        if (viewpointsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "등록된 시점이 없습니다. 아래 [＋ 현재 카메라 위치를 새 시점으로 추가]를 누르세요.",
                MessageType.Info);
            return;
        }

        int removeIndex = -1;
        int moveUpIndex = -1;
        int moveDownIndex = -1;

        for (int i = 0; i < viewpointsProp.arraySize; i++)
        {
            SerializedProperty element = viewpointsProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = element.FindPropertyRelative("name");
            SerializedProperty posProp = element.FindPropertyRelative("position");
            SerializedProperty rotProp = element.FindPropertyRelative("eulerAngles");
            SerializedProperty fovProp = element.FindPropertyRelative("fieldOfView");
            SerializedProperty capturedProp = element.FindPropertyRelative("captured");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // ── 헤더: 인덱스 + 이름 + 저장 여부 ──
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(28));
            EditorGUILayout.PropertyField(nameProp, GUIContent.none);

            bool captured = capturedProp.boolValue;
            GUI.color = captured ? Color.green : Color.gray;
            EditorGUILayout.LabelField(captured ? "● 저장됨" : "○ 비어있음", GUILayout.Width(70));
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            // ── 저장된 값 표시 (읽기 전용) ──
            if (captured)
            {
                EditorGUI.indentLevel++;
                GUI.enabled = false;
                EditorGUILayout.Vector3Field("Position", posProp.vector3Value);
                EditorGUILayout.Vector3Field("Rotation", rotProp.vector3Value);
                EditorGUILayout.FloatField("FOV", fovProp.floatValue);
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);

            // ── 저장 버튼 ──
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("현재 카메라 위치 저장", GUILayout.Height(24)))
                SaveFromTargetCamera(i);

            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
            if (GUILayout.Button("씬 뷰 위치 저장", GUILayout.Height(24)))
                SaveFromSceneView(i);

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // ── 이동 / 정렬 / 삭제 버튼 ──
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("이 시점으로 이동", GUILayout.Height(20)))
                MoveCameraTo(i);

            if (GUILayout.Button("씬 뷰를 여기로", GUILayout.Height(20)))
                MoveSceneViewTo(i);

            GUI.enabled = i > 0;
            if (GUILayout.Button("▲", GUILayout.Width(28), GUILayout.Height(20)))
                moveUpIndex = i;

            GUI.enabled = i < viewpointsProp.arraySize - 1;
            if (GUILayout.Button("▼", GUILayout.Width(28), GUILayout.Height(20)))
                moveDownIndex = i;

            GUI.enabled = true;
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("✕", GUILayout.Width(28), GUILayout.Height(20)))
                removeIndex = i;
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        // 루프가 끝난 뒤에 구조 변경을 적용한다 (순회 중 변경 방지)
        if (moveUpIndex > 0)
            viewpointsProp.MoveArrayElement(moveUpIndex, moveUpIndex - 1);

        if (moveDownIndex >= 0 && moveDownIndex < viewpointsProp.arraySize - 1)
            viewpointsProp.MoveArrayElement(moveDownIndex, moveDownIndex + 1);

        if (removeIndex >= 0)
            viewpointsProp.DeleteArrayElementAtIndex(removeIndex);
    }

    private void DrawBottomButtons()
    {
        EditorGUILayout.Space(6);

        GUI.backgroundColor = new Color(0.7f, 0.9f, 0.7f);
        if (GUILayout.Button("＋ 현재 카메라 위치를 새 시점으로 추가", GUILayout.Height(28)))
            AddFromTargetCamera();
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("＋ 씬 뷰 위치를 새 시점으로 추가", GUILayout.Height(22)))
            AddFromSceneView();

        EditorGUILayout.Space(4);

        EditorGUILayout.HelpBox(
            "권장 작업 흐름\n" +
            "1. 씬 뷰에서 원하는 구도를 잡는다\n" +
            "2. [＋ 씬 뷰 위치를 새 시점으로 추가]를 누른다\n" +
            "3. 이름을 알아보기 쉽게 바꾼다 (예: 전체보기, 로봇 클로즈업)\n" +
            "4. UI 버튼의 OnClick에 GoTo(int)를 연결한다",
            MessageType.None);
    }

    // ============================================================
    // 동작
    // ============================================================

    private void SaveFromTargetCamera(int index)
    {
        Camera cam = mgr.ResolveCamera();
        if (cam == null) return;

        Undo.RecordObject(mgr, "시점 저장");
        WriteToElement(index, cam.transform.position, cam.transform.eulerAngles,
                       cam.fieldOfView, cam.orthographic, cam.orthographicSize);

        MarkDirty();
        Debug.Log($"[{mgr.name}] 시점 [{index}] 저장 (대상 카메라 기준)", mgr);
    }

    private void SaveFromSceneView(int index)
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null || sv.camera == null)
        {
            Debug.LogWarning("활성화된 씬 뷰가 없습니다. 씬 뷰를 한 번 클릭한 뒤 다시 시도하세요.");
            return;
        }

        Transform t = sv.camera.transform;

        Undo.RecordObject(mgr, "시점 저장");
        WriteToElement(index, t.position, t.eulerAngles,
                       sv.camera.fieldOfView, sv.camera.orthographic, sv.camera.orthographicSize);

        MarkDirty();
        Debug.Log($"[{mgr.name}] 시점 [{index}] 저장 (씬 뷰 기준)", mgr);
    }

    private void AddFromTargetCamera()
    {
        Camera cam = mgr.ResolveCamera();
        if (cam == null) return;

        int i = AppendElement($"시점 {viewpointsProp.arraySize + 1}");
        WriteToElement(i, cam.transform.position, cam.transform.eulerAngles,
                       cam.fieldOfView, cam.orthographic, cam.orthographicSize);
        MarkDirty();
    }

    private void AddFromSceneView()
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null || sv.camera == null)
        {
            Debug.LogWarning("활성화된 씬 뷰가 없습니다. 씬 뷰를 한 번 클릭한 뒤 다시 시도하세요.");
            return;
        }

        Transform t = sv.camera.transform;

        int i = AppendElement($"시점 {viewpointsProp.arraySize + 1}");
        WriteToElement(i, t.position, t.eulerAngles,
                       sv.camera.fieldOfView, sv.camera.orthographic, sv.camera.orthographicSize);
        MarkDirty();
    }

    private void MoveCameraTo(int index)
    {
        Camera cam = mgr.ResolveCamera();
        if (cam == null) return;

        SerializedProperty e = viewpointsProp.GetArrayElementAtIndex(index);
        if (!e.FindPropertyRelative("captured").boolValue)
        {
            Debug.LogWarning($"시점 [{index}]은(는) 아직 저장된 적이 없습니다.");
            return;
        }

        Undo.RecordObject(cam.transform, "카메라 시점 이동");
        Undo.RecordObject(cam, "카메라 시점 이동");

        cam.transform.position = e.FindPropertyRelative("position").vector3Value;
        cam.transform.eulerAngles = e.FindPropertyRelative("eulerAngles").vector3Value;
        cam.orthographic = e.FindPropertyRelative("orthographic").boolValue;
        cam.fieldOfView = e.FindPropertyRelative("fieldOfView").floatValue;
        cam.orthographicSize = e.FindPropertyRelative("orthographicSize").floatValue;

        EditorUtility.SetDirty(cam);
    }

    private void MoveSceneViewTo(int index)
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return;

        SerializedProperty e = viewpointsProp.GetArrayElementAtIndex(index);
        if (!e.FindPropertyRelative("captured").boolValue) return;

        Vector3 pos = e.FindPropertyRelative("position").vector3Value;
        Quaternion rot = Quaternion.Euler(e.FindPropertyRelative("eulerAngles").vector3Value);

        // 씬 뷰는 pivot 기준이므로, 카메라 위치에서 앞쪽으로 조금 떨어진 지점을 pivot으로 잡는다.
        sv.pivot = pos + rot * Vector3.forward * sv.cameraDistance;
        sv.rotation = rot;
        sv.Repaint();
    }

    // ============================================================
    // 헬퍼
    // ============================================================

    private int AppendElement(string name)
    {
        int i = viewpointsProp.arraySize;
        viewpointsProp.InsertArrayElementAtIndex(i);

        SerializedProperty e = viewpointsProp.GetArrayElementAtIndex(i);
        e.FindPropertyRelative("name").stringValue = name;
        e.FindPropertyRelative("captured").boolValue = false;

        return i;
    }

    private void WriteToElement(int index, Vector3 pos, Vector3 euler,
                                float fov, bool ortho, float orthoSize)
    {
        SerializedProperty e = viewpointsProp.GetArrayElementAtIndex(index);

        e.FindPropertyRelative("position").vector3Value = pos;
        e.FindPropertyRelative("eulerAngles").vector3Value = euler;
        e.FindPropertyRelative("fieldOfView").floatValue = fov;
        e.FindPropertyRelative("orthographic").boolValue = ortho;
        e.FindPropertyRelative("orthographicSize").floatValue = orthoSize;
        e.FindPropertyRelative("captured").boolValue = true;
    }

    private void MarkDirty()
    {
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(mgr);

        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(mgr.gameObject.scene);
        }
    }
}