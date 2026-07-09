using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class MagneticSensor : MonoBehaviour
{

    public LayerMask detectableLayer;       //감지 가능한 레이어
    public string detectableTag;            //감지 가능한 태그
    public string detectableName;           //감지 가능한 이름

    public List<Collider> triggerList = new List<Collider>();   //감지한 콜라이더 리스트

    public UnityEvent<bool> onChangedDetected;                  //감지 결과가 변경될 때 호출하는 콜백함수를 담고 있는 델리게이트

    //감지 결과에 대한 프로퍼티
    private bool hasDetected;

    public bool HasDetected
    {
        //외부에서 값을 확인 가능
        get => hasDetected;
        //private를 써주면 외부에서 값을 수정할 수 없게 만들 수 있음
        private set
        {
            if (hasDetected == value)
                return;
            hasDetected = value;
            onChangedDetected?.Invoke(value);
        }
    }

    private void Awake()
    {
        Collider c = GetComponent<Collider>();
        //게임 오브젝트 안에 감지할 콜라이더가 없으면 콜라이더를 추가하고
        if (c == null)
            c = gameObject.AddComponent<BoxCollider>();
        //IsTrigger 체크해라
        c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        //감지 가능한 레이어들 중에서 트리거된 게임 오브젝트가 레이어에 포함되지 않으면 리턴
        if ((detectableLayer.value & 1 << other.gameObject.layer) == 0)
            return;
        //감지 가능한 태그가 입력되어 있고, 트리거된 게임오브젝트의 태그가 감지 가능한 태그와 다를 경우 리턴
        if (!string.IsNullOrEmpty(detectableTag) && other.gameObject.tag != detectableTag)
            return;
        //감지 가능한 이름이 입력되어 있고, 트리거된 게임오브젝트의 이름 안에 감지 가능한 이름이 포함되지 않을 경우 리턴
        if (!string.IsNullOrEmpty(detectableName) && !other.gameObject.name.Contains(detectableName))
            return;

        if (triggerList.Contains(other))
            return;
        triggerList.Add(other);
        HasDetected = triggerList.Count > 0;
    }


    private void OnTriggerExit(Collider other)
    {
        //감지 가능한 레이어들 중에서 트리거된 게임 오브젝트가 레이어에 포함되지 않으면 리턴
        if ((detectableLayer.value & 1 << other.gameObject.layer) == 0)
            return;
        //감지 가능한 태그가 입력되어 있고, 트리거된 게임오브젝트의 태그가 감지 가능한 태그와 다를 경우 리턴
        if (!string.IsNullOrEmpty(detectableTag) && other.gameObject.tag != detectableTag)
            return;
        //감지 가능한 이름이 입력되어 있고, 트리거된 게임오브젝트의 이름 안에 감지 가능한 이름이 포함되지 않을 경우 리턴
        if (!string.IsNullOrEmpty(detectableName) && !other.gameObject.name.Contains(detectableName))
            return;

        if (!triggerList.Contains(other))
            return;
        triggerList.Remove(other);
        HasDetected = triggerList.Count > 0;
    }
}
