using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public enum UpdateType
    {
        None,
        FixedUpdate,
        Update,
        LateUpdate,
        Max
    }

    public UpdateType type = UpdateType.Update;         //업데이트 방식
    [Header("Movement Settings")]
    public float moveSpeed = 10f;                       //카메라의 이동속도
    public float lookSensitivity = 0.1f;                //마우스 감도
    public float panSpeed = 0.05f;                      //패닝 속도
    public float lerpSpeed = 10f;                       //부드러운 움직임의 정도값

    [Header("Focus Settings")]
    public float focusDistance = 1.5f;                  //포커스할 때 기본 거리
    public float minFocusDistance = 1f;                 //최소 줌 거리
    public float maxFocusDistance = 10f;                //최대 줌 거리
    public float scrollSensitivity = 0.01f;              //휠 감도

    //상태 변수
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float zoomInput;
    private Transform focusingTarget = null;
    private bool isFocusing = false;

    //마우스 버튼 상태
    private bool isRightPressed;
    private bool isLeftPressed;
    private bool isMiddlePressed;

    private void Awake()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    private void FixedUpdate()
    {
        if (type != UpdateType.FixedUpdate)
            return;

        HandleCalculation();
        Vector3 position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
        quaternion rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * lerpSpeed);
        transform.SetPositionAndRotation(position, rotation);
    }

    private void Update()
    {
        if (type != UpdateType.Update)
            return;
        HandleCalculation();
        // 선형 보간(Lerp)을 이용해 부드러운 이동 및 회전
        Vector3 position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
        quaternion rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
        transform.SetPositionAndRotation(position, rotation);
    }

    private void LateUpdate()
    {

        if (type != UpdateType.LateUpdate)
            return;
        HandleCalculation();
        Vector3 position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
        quaternion rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
        transform.SetPositionAndRotation(position, rotation);
    }

    //키보드 WASD 입력 값
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        if (moveInput.sqrMagnitude > 0.01f)
            StopFocus();
    }

    //마우스의 포인터 위치 입력값 콜백함수
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    //Orbit
    public void OnOrbit(InputValue value)
    {
        isLeftPressed = value.isPressed;
    }


    //Freecam
    public void OnFreeCam(InputValue value)
    {
        isRightPressed = value.isPressed;
        if (isRightPressed)
            StopFocus();
    }

    //Pan

    public void OnPan(InputValue value)
    {
        isMiddlePressed = value.isPressed;
    }

    //Zoom
    public void OnZoom(InputValue value)
    {
        zoomInput = value.Get<float>();
    }



    public void OnFocus()
    {
        Debug.Log("Focus!!!!!!");
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            focusingTarget = hit.transform;
            isFocusing = true;
            //포커싱 하는 순간 포커싱된 물체를 정면으로 쳐다보는 기능.
            Vector3 direction = focusingTarget.position - transform.position;
            targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            focusDistance = Mathf.Clamp(direction.magnitude, minFocusDistance, maxFocusDistance);
            targetPosition = focusingTarget.position - (targetRotation * Vector3.forward * focusDistance);
        }
    }

    private void StopFocus()
    {
        if (isFocusing == false)
            return;

        isFocusing = false;
        targetPosition = transform.position;
        focusingTarget = null;
    }

    private void HandleCalculation()
    {
        if (isFocusing && focusingTarget != null)
        {
            //1. 휠로 스크롤하면 줌거리 조절
            if (Mathf.Abs(zoomInput) > 0.01f)
            {
                //휠을 위로 밀면 (양수) 거리가 가까워지고, 아래로 당기면 (음수) 멀어짐(반전해서 계산)
                focusDistance -= zoomInput * scrollSensitivity;
                //거리제한(Clamp)
                focusDistance = Mathf.Clamp(focusDistance, minFocusDistance, maxFocusDistance);
            }

            //2. 궤도 회전(좌클릭 드래그할 때)
            if (isLeftPressed && lookInput.sqrMagnitude > 0.01f)
            {
                float x = lookInput.x * lookSensitivity;
                float y = lookInput.y * lookSensitivity;
                targetRotation = Quaternion.Euler(-y, x, 0);
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = quaternion.Euler(euler.x, euler.y, 0f);
            }

            //3. 최종 위치 계산 => 타겟 중심으로 회전하고 얻어낸 거리 위치에 타겟 위치를 설정함.
            targetPosition = focusingTarget.position - (targetRotation * Vector3.forward * focusDistance);
        }
        else
        {
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 dir = (transform.forward * moveInput.y) + (transform.right * moveInput.x);
                targetPosition += dir * moveSpeed * Time.deltaTime;
            }

            if (isRightPressed && lookInput.sqrMagnitude > 0.01f)
            {
                float x = lookInput.x * lookSensitivity;
                float y = lookInput.y * lookSensitivity;
                targetRotation *= Quaternion.Euler(-y, x, 0);
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, euler.y, 0f);
            }

            if (isMiddlePressed && lookInput.sqrMagnitude > 0.01f)
            {
                Vector3 pan = (transform.up * -lookInput.y + transform.right * -lookInput.x) * panSpeed;
                targetPosition += pan;
            }
        }

    }

    public void MoveToDestination(Transform destination)
    {
        StopFocus();

        targetPosition = destination.position;
        targetRotation = destination.rotation;
    }

}
