using UnityEngine;

public class Restarter : MonoBehaviour
{
    public void Restert()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
