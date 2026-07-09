using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class OpticalSensor : MonoBehaviour
{
    [Header("감지 조건")]
    public LayerMask detectableLayer;           //감지 가능한 레이어
    public string detectableTag;                //감지 가능한 태그
    public string detectableName;               //감지 가능한 이름
    public float detectableDistance = 0.5f;     //감지 거리

    [Header("선택 사항")]
    public float lineWidth = 0.01f;             //레이저 굵기
    public Material greenLine;                  //감지 안 될 때 사용할 매터리얼
    public Material redLine;                    //감지될 때 사용할 매터리얼



    public UnityEvent<bool> onChangedDetected;  //감지 변화에 대한 콜백함수들을 담는 델리게이트

    private LineRenderer lineRenderer;           //게임뷰에서 레이저가 보이도록 라인 렌더러
    private bool hasDetected;                   //감지 여부
    private Vector3 detectedPoint;              //감지 위치

    //감지 결과에 따른 프로퍼티
    public bool HasDetected
    {
        get => hasDetected;

        private set
        {
            //결과가 동일하면 무시
            if (hasDetected == value)
                return;
            hasDetected = value;
            //등록된 콜백 함수들에게 최신 결과를 알림
            onChangedDetected?.Invoke(value);
        }
    }

    private void Awake()
    {
        
        lineRenderer = GetComponent<LineRenderer>();
        //라인 렌더러가 없으면 밑에 코드 무시
        if (lineRenderer == null)
            return;

        //라인의 위치를 로컬 축에 맞춘다
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPositions(new Vector3[2]
        {
            transform.position, transform.position + transform.forward *detectableDistance
        });
        lineRenderer.material = greenLine;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
    }

    private void Update()
    {
        //레이저 준비(자신의 위치에서, 정면방향으로)
        Ray ray = new Ray(transform.position, transform.forward);
        //준비된 레이저를 발사
        if (Physics.Raycast(ray, out RaycastHit hit, detectableDistance, detectableLayer))
        {
            detectedPoint = hit.point;
            //감지 가능한 태그가 들어있고, 레이저에 감지된 게임오브젝트의 태그가 감지가능한 태그와 다르면 무시한다.
            if (!string.IsNullOrEmpty(detectableTag) && hit.transform.gameObject.tag != detectableTag)
            {
                HasDetected = false;
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, transform.position + transform.forward * detectableDistance);
                lineRenderer.material = greenLine;
                return;
            }
            //감지 가능한 이름이 들어있고, 레이저에 감지된 게임오브젝트의 이름에 감지가능한 이름이 포함되지 않으면 무시한다
            if (!string.IsNullOrEmpty(detectableName) && !hit.transform.gameObject.name.Contains(detectableName))
            {
                HasDetected = false;
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, transform.position + transform.forward * detectableDistance);
                lineRenderer.material = greenLine;
                return;
            }

            HasDetected = true;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, detectedPoint);
            lineRenderer.material = redLine;
        }
        else
        {
            HasDetected = false;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position + transform.forward * detectableDistance);
            lineRenderer.material = greenLine;
        }
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (hasDetected)
        {
            //감지하면 붉은 색 라인 그리기
            Handles.color = Color.red;
            Handles.DrawLine(transform.position, detectedPoint);
        }
        else
        {
            //감지 못하면 녹색 라인으로 최대 감지 범위 그리기
            Handles.color = Color.green;
            Handles.DrawLine(transform.position, transform.position + transform.forward * detectableDistance);
        }
    }

    private void OnValidate()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            return;

        //라인의 위치를 로컬 축에 맞춘다
        lineRenderer.SetPositions(new Vector3[2]
        {
            transform.position, transform.position + transform.forward * detectableDistance
        });
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
    }

#endif
}
