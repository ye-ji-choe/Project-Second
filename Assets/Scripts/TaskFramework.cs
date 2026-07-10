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


    /// <summary>
    /// 지정된 조건이 true가 될 때까지 시퀀스를 일시 정지합니다.
    /// </summary>
    protected void WaitUntil(System.Func<bool> condition)
    {
        _commandQueue.Enqueue(WaitUntilRoutine(condition));
    }

    /// <summary>
    /// 로봇 이동 중 특정 타이밍에 일반 C# 코드(PLC 신호 쓰기 등)를 실행합니다.
    /// </summary>
    protected void DoAction(System.Action action)
    {
        _commandQueue.Enqueue(ActionRoutine(action));
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

        // 1. 목표 절대 좌표(World) 획득
        Vector3 pos_m = targetPos * 0.001f;
        Matrix4x4 localTargetMatrix = Matrix4x4.TRS(pos_m, targetRot, Vector3.one);
        Matrix4x4 endWorldMatrix = robotController.FrameToWorld(localTargetMatrix, robotController.Frame.Value);
        Vector3 endPos = endWorldMatrix.GetColumn(3);
        Quaternion endRot = endWorldMatrix.rotation;

        // 2. 시작 상태(관절 및 절대 좌표) 획득
        var startJointState = robotController.MechanicalGroup.JointState;
        Matrix4x4 startWorldMatrix = robotController.Solver.ComputeForward(startJointState, robotController.Tool.Value);
        Vector3 startPos = startWorldMatrix.GetColumn(3);
        Quaternion startRot = startWorldMatrix.rotation;

        float distance = Vector3.Distance(startPos, endPos);
        if (distance <= 0.0001f) yield break;

        float duration = distance / (speed * 0.5f);
        float timeElapsed = 0f;

        // ==========================================
        // [ 분기점: LIN vs PTP 동작 로직 분리 ]
        // ==========================================
        if (motionType == "LIN")
        {
            // --- LIN: 직교 공간 직선 보간 (Cartesian Interpolation) ---
            while (timeElapsed < duration)
            {
                timeElapsed += Time.deltaTime;
                float smoothT = Mathf.SmoothStep(0f, 1f, timeElapsed / duration);

                // 매 프레임 X, Y, Z 좌표를 직선으로 보간
                Vector3 stepPos = Vector3.Lerp(startPos, endPos, smoothT);
                Quaternion stepRot = Quaternion.Slerp(startRot, endRot, smoothT);
                Matrix4x4 stepMatrix = Matrix4x4.TRS(stepPos, stepRot, Vector3.one);

                // 좌표가 변할 때마다 매번 역기구학(IK) 계산
                var solution = robotController.Solver.ComputeInverse(
                    stepMatrix,
                    robotController.Tool.Value,
                    robotController.Configuration.Value,
                    startJointState.ExtJoint
                );

                robotController.Solver.TryApplySolution(solution, true);
                yield return null;
            }
        }
        else if (motionType == "PTP")
        {
            // --- PTP: 관절 공간 최적 보간 (Joint Interpolation) ---
            // 1. 목표 지점의 관절 각도를 이동 시작 전에 단 '한 번'만 계산
            Matrix4x4 targetMatrix = Matrix4x4.TRS(endPos, endRot, Vector3.one);
            var endSolution = robotController.Solver.ComputeInverse(
                targetMatrix,
                robotController.Tool.Value,
                robotController.Configuration.Value,
                startJointState.ExtJoint
            );

            // 보내주신 MechanicalGroup 스크립트 구조에 맞춘 배열 추출 (.Value 및 .JointTarget 사용)
            float[] startJoints = startJointState.Value;
            float[] endJoints = endSolution.JointTarget.Value;

            // 보간된 값을 담을 임시 타겟 객체 복사 (C# 9.0 record struct 복사 기능 활용)
            var currentJointTarget = startJointState with { };

            while (timeElapsed < duration)
            {
                timeElapsed += Time.deltaTime;
                float smoothT = Mathf.SmoothStep(0f, 1f, timeElapsed / duration);

                // 매 프레임 각 관절(모터)의 각도를 부드럽게 보간
                for (int i = 0; i < startJoints.Length; i++)
                {
                    // MechanicalGroup의 인덱서 기능(currentJointTarget[i])을 활용하여 각도 주입
                    currentJointTarget[i] = Mathf.Lerp(startJoints[i], endJoints[i], smoothT);
                }

                // IK(역기구학) 계산 없이 변경된 관절 각도를 로봇 컨트롤러에 직접 적용하여 곡선 궤적 형성
                robotController.MechanicalGroup.SetJoints(currentJointTarget, true);
                yield return null;
            }
        }
    }

    protected void Offset(Target target, Vector3 offset, float speed = 1.0f)
    {
        if (target != null)
        {
            Vector3 targetPos = target.position;
            Quaternion targetRot = Quaternion.Euler(target.rotation);

            // 타겟의 자세(방향)를 기준으로 오프셋 위치 계산
            Vector3 offsetPos = targetPos + (targetRot * offset);

            // MotionRoutine에 계산된 위치, 기존 회전값, 동작 타입(LIN), 속도를 넘겨 큐에 적재
            _commandQueue.Enqueue(MotionRoutine(offsetPos, targetRot, "LIN", speed));
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

    private IEnumerator WaitUntilRoutine(System.Func<bool> condition)
    {
        // 유니티 내장 클래스인 WaitUntil을 사용하여 조건이 참이 될 때까지 대기
        yield return new UnityEngine.WaitUntil(condition);
    }

    private System.Collections.IEnumerator ActionRoutine(System.Action action)
    {
        // 전달받은 Action(함수)을 실행
        action?.Invoke();
        yield return null;
    }

}