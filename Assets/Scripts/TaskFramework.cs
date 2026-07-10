using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Preliy.Flange;

[System.Serializable]
public class Target
{
    [Tooltip("목표 위치 (mm 기준 입력)")]
    public Vector3 position;

    [Tooltip("목표 회전 각도 (Rx, Ry, Rz)")]
    public Vector3 rotation;
}

public abstract class MotionInstruction
{
    public Vector3 position;
    public Quaternion rotation;
    public float speed = 1.0f;
    public bool isRelativeTool = false;

    public MotionInstruction Speed(float speedValue)
    {
        this.speed = speedValue;
        return this;
    }
    public MotionInstruction RelTool()
    {
        this.isRelativeTool = true;
        return this;
    }
}

public class LIN : MotionInstruction
{
    public LIN(Vector3 position, Quaternion rotation = default)
    {
        this.position = position;
        this.rotation = rotation;
    }
}

public class PTP : MotionInstruction
{
    public PTP(Vector3 position, Quaternion rotation = default)
    {
        this.position = position;
        this.rotation = rotation;
    }
}

public abstract class Task : MonoBehaviour
{
    [Header("로봇 컨트롤러 연결")]
    public Controller robotController;

    private Queue<IEnumerator> _commandQueue = new Queue<IEnumerator>();
    private bool _isRunning = false;

    private void Awake()
    {
        if (robotController == null)
            robotController = GetComponent<Controller>();
    }

    private void Start()
    {
        Program();
        StartCoroutine(ExecuteQueue());
    }

    protected abstract void Program();

    #region 모션 큐 적재 API

    protected void PTP(Target target)
    {
        if (target != null)
            _commandQueue.Enqueue(MotionRoutine(target.position, Quaternion.Euler(target.rotation), "PTP"));
    }

    protected void LIN(Target target)
    {
        if (target != null)
            _commandQueue.Enqueue(MotionRoutine(target.position, Quaternion.Euler(target.rotation), "LIN"));
    }

    protected void Move(MotionInstruction instruction)
    {
        _commandQueue.Enqueue(MotionRoutine(instruction.position, instruction.rotation, instruction.GetType().Name, instruction.speed));
    }

    protected void Wait(int milliseconds)
    {
        _commandQueue.Enqueue(WaitRoutine(milliseconds));
    }

    protected void Message(LogType logType, string text)
    {
        _commandQueue.Enqueue(MessageRoutine(logType, text));
    }

    #endregion

    #region 핵심 보간 및 컨트롤러 제어부

    private IEnumerator ExecuteQueue()
    {
        _isRunning = true;
        while (_commandQueue.Count > 0)
        {
            IEnumerator currentCommand = _commandQueue.Dequeue();
            yield return StartCoroutine(currentCommand);
        }
        _isRunning = false;
        Debug.Log("[Task] 시퀀스 종료");
    }

    private IEnumerator MotionRoutine(Vector3 targetPos, Quaternion targetRot, string motionType, float speed = 1.0f)
    {
        if (robotController == null || !robotController.IsValid.Value) yield break;

        // 1. [목표 지점 설정] 사용자가 입력한 mm 단위를 m로 변환하고 Local 행렬 생성
        Vector3 pos_m = targetPos * 0.001f;
        Matrix4x4 localTargetMatrix = Matrix4x4.TRS(pos_m, targetRot, Vector3.one);

        // Controller의 FrameToWorld를 사용해 오프셋이 적용된 정확한 절대 좌표(World) 획득
        Matrix4x4 endWorldMatrix = robotController.FrameToWorld(localTargetMatrix, robotController.Frame.Value);
        Vector3 endPos = endWorldMatrix.GetColumn(3);
        Quaternion endRot = endWorldMatrix.rotation;

        // 2. [시작 지점 설정] Solver의 정기구학을 돌려 현재 TCP의 정확한 절대 좌표 획득
        Matrix4x4 startWorldMatrix = robotController.Solver.ComputeForward(
            robotController.MechanicalGroup.JointState,              // <--- .JointTarget 삭제
            robotController.Tool.Value
        );
        Vector3 startPos = startWorldMatrix.GetColumn(3);
        Quaternion startRot = startWorldMatrix.rotation;

        // 3. 이동 궤적 보간 연산
        float distance = Vector3.Distance(startPos, endPos);
        if (distance <= 0.0001f) yield break;

        float duration = distance / (speed * 0.5f);
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // 매 프레임마다 목표를 향한 절대 좌표 행렬 갱신
            Vector3 stepPos = Vector3.Lerp(startPos, endPos, smoothT);
            Quaternion stepRot = Quaternion.Slerp(startRot, endRot, smoothT);
            Matrix4x4 stepMatrix = Matrix4x4.TRS(stepPos, stepRot, Vector3.one);

            // 4. 역기구학 연산 후 로봇에 적용
            var solution = robotController.Solver.ComputeInverse(
                stepMatrix,
                robotController.Tool.Value,
                robotController.Configuration.Value,
                robotController.MechanicalGroup.JointState.ExtJoint
            );

            robotController.Solver.TryApplySolution(solution, true);
            yield return null;
        }
    }

    private IEnumerator WaitRoutine(int milliseconds)
    {
        yield return new WaitForSeconds(milliseconds / 1000f);
    }

    private IEnumerator MessageRoutine(LogType logType, string text)
    {
        Debug.unityLogger.Log(logType, text);
        yield return null;
    }

    #endregion

    // TaskFramework.cs에 아래 메서드를 추가하세요
    protected void Log(string message)
    {
        _commandQueue.Enqueue(MessageRoutine(LogType.Log, message));
    }
}