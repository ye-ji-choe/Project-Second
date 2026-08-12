using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoverSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public List<Collider> triggerList = new List<Collider>();
    public GameObject[] prefabs;
    public Transform spawnPosition;
    public Transform targetParent;
    public string targetTag = "원하는태그명입력"; // ★ 새로 추가: 강제로 적용할 태그 이름
    public float spawnDelay = 5f;

    private Coroutine spawnCoroutine;

    private void Start()
    {
        if(triggerList.Count == 0)
        StartCoroutine(WaitAndSpawn());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerList.Contains(other)) triggerList.Add(other);

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (triggerList.Contains(other)) triggerList.Remove(other);

        if (triggerList.Count == 0)
        {
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(WaitAndSpawn());
        }
    }

    private IEnumerator WaitAndSpawn()
    {
        yield return new WaitForSeconds(spawnDelay);

        if (triggerList.Count == 0 && prefabs.Length > 0 && spawnPosition != null)
        {
            GameObject prefabToSpawn = prefabs[Random.Range(0, prefabs.Length)];
            Transform parentTransform = targetParent != null ? targetParent : spawnPosition.parent;

            GameObject spawnedCover = Instantiate(
                prefabToSpawn,
                spawnPosition.position,
                spawnPosition.rotation,
                parentTransform
            );

            spawnedCover.SetActive(true);

            // (Clone) 글자 제거
            spawnedCover.name = prefabToSpawn.name;

            // ★ 수정됨: 인스펙터에서 입력한 태그명으로 강제 변경
            if (!string.IsNullOrEmpty(targetTag))
            {
                spawnedCover.tag = targetTag;
            }
        }
    }
}