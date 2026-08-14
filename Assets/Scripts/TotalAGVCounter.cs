using UnityEngine;
using UnityEngine.UI;

public class TotalAGVCounter : MonoBehaviour
{
    public Text totalCountUI; // 전체 대수를 표시할 UI 글자

    void Start()
    {
        // 씬(Scene)에 있는 모든 오브젝트를 검색 (조금 무겁지만 시작할 때 한 번만 하므로 괜찮습니다)
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int agvCount = 0;

        // 찾은 오브젝트 중에서 이름에 "AGV"가 들어가는 녀석만 골라서 숫자를 셉니다.
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("AGV"))
            {
                agvCount++;
            }
        }

        // 찾아낸 총 숫자를 캔버스 글자에 띄워줍니다.
        if (totalCountUI != null)
        {
            totalCountUI.text = "가동하는 AGV: " + agvCount.ToString() + "대";
        }
    }
}