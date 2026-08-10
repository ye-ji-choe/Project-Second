using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoverSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public List<Collider> triggerList = new List<Collider>(); // 영역 안에 있는 콜라이더 리스트
    public GameObject[] prefabs;        // 생성할 커버 프리팹 배열
    public Transform spawnPosition;     // 커버가 생성될 위치
    public float spawnDelay = 5f;       // 커버가 없어진 후 대기하는 시간 (5초)

    private Coroutine spawnCoroutine;   // 5초 대기를 관리할 코루틴 변수

    // Update 함수는 아예 사용하지 않습니다. (불필요한 시간 계산 방지)

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerList.Contains(other))
        {
            triggerList.Add(other);
        }

        // 영역 안에 무언가 들어왔다면, 진행 중이던 5초 대기(스폰)를 즉시 취소합니다.
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (triggerList.Contains(other))
        {
            triggerList.Remove(other);
        }

        // 커버가 밖으로 나가서 영역이 '완전히 비워진 바로 그 순간' 5초 대기 시작
        if (triggerList.Count == 0)
        {
            // 혹시 기존에 돌고 있던 대기가 있다면 끄고 새로 시작
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }
            spawnCoroutine = StartCoroutine(WaitAndSpawn());
        }
    }

    // 5초 대기 후 생성하는 코루틴
    private IEnumerator WaitAndSpawn()
    {
        // 커버가 없어진 시점부터 5초 동안 가만히 대기합니다.
        yield return new WaitForSeconds(spawnDelay);

        // 5초가 지났는데도 여전히 영역이 비어있다면 커버 생성
        if (triggerList.Count == 0 && prefabs.Length > 0 && spawnPosition != null)
        {
            Instantiate(
                prefabs[Random.Range(0, prefabs.Length)],
                spawnPosition.position,
                spawnPosition.rotation,
                spawnPosition
            );
        }
    }
}