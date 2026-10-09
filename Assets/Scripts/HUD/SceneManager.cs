using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerKit : MonoBehaviour
{
    public string nextScene;
    public void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextScene)) SceneManager.LoadScene(nextScene);
    }
}