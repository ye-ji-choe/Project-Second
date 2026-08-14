using UnityEngine;
using UnityEngine.UI;

public class SensorCounter : MonoBehaviour
{
    [Header("카운트 설정")]
    public string prefix = "정상품: ";
    public int currentCount = 0;

    [Header("중복 방지 (쿨타임)")]
    public float cooldownTime = 1.0f; // 1초 대기
    private float lastCountTime = -100f;

    [Header("UI 연결")]
    public Text countUI;

    private void OnTriggerEnter(Collider other)
    {
        // Box Collider에서 이미 BATTERY 레이어만 들어오게 설정하셨으므로,
        // 복잡한 이름 검사 없이 '쿨타임(1초)'이 지났는지만 확인합니다!
        if (Time.time >= lastCountTime + cooldownTime)
        {
            currentCount++; // 카운트 1 증가
            lastCountTime = Time.time; // 시간 기록

            // 캔버스 UI 업데이트
            if (countUI != null)
            {
                countUI.text = prefix + currentCount.ToString();
            }
        }
    }
}