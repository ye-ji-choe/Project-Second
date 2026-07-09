using System.Collections.Generic;
using UnityEngine;

public class AGVSpawner : MonoBehaviour
{
    public List<Collider> triggerList = new List<Collider>();   //영역 안에 존재하는 콜라이더 리스트
    public GameObject[] prefabs;               //주기적으로 생성할 게임오브젝트
    public Transform spawnPosition;         //생성 위치
    public float startSpawnTime = 3f;       //활성화시 처음 생성되기까지 걸리는 시간.
    public float spawnInterval = 2.5f;      //생성 인터벌
    public int maxCount = 0;              //최대 생성 카운트, 0이면 무한
    private float nextSpawnTime;            //다음 생성 타임
    private int currentCount;

    private void Start()
    {
        nextSpawnTime = Time.time + startSpawnTime;
    }


    private void Update()
    {
        //최대 카운트가 0보다 큰데, 현재 생성갯수가 최대생성갯수보다 같거나 클때
        if (maxCount > 0 && currentCount >= maxCount)
            return;

        if (triggerList.Count == 0 && nextSpawnTime < Time.time)
        {
            nextSpawnTime = Time.time + spawnInterval;
            //Instantiate함수 => 동적으로 지정된 게임오브젝트를 씬에 생성하는 함수.
            GameObject nextCube =
                Instantiate<GameObject>(
                    prefabs[Random.Range(0, prefabs.Length)], spawnPosition.position, spawnPosition.rotation, spawnPosition);

            currentCount++;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerList.Contains(other))
            return;

        triggerList.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!triggerList.Contains(other))
            return;

        triggerList.Remove(other);
    }
}
