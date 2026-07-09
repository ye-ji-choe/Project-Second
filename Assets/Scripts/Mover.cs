using UnityEngine;

public class Mover : MonoBehaviour
{
    public Transform[] positions;
    public float speed = 1.0f;
    public int destination;

    private void Update()
    {
        if (positions == null || positions.Length == 0)
            return;

        transform.position = Vector3.MoveTowards(transform.position, positions[destination].position, speed * Time.deltaTime);
        if(Vector3.Distance(transform.position, positions[destination].position) < 0.01f)
        {
            destination += 1;
            if(destination >= positions.Length)
                positions = null;
        }
    }
    public void SetDestination(Transform[] routes)
    {
        positions = routes;
        destination = 0;
    }
}
