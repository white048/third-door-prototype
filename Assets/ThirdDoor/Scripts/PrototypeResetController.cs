using UnityEngine;
using UnityEngine.SceneManagement;

public class PrototypeResetController : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex >= 0 ? activeScene.buildIndex : 0);
        }
    }
}
