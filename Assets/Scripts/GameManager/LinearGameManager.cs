using UnityEngine;
using UnityEngine.SceneManagement;

public class LinearGameManager : BaseGameManager {

    public string nextScene;
    public string failedScene;

    protected override void Start()
    {
        base.Start();
    }

    protected override void AllMissionsCompletedHandler() {
        if (debugEnabled) Debug.Log("=== All missions completed ===");
        if (!string.IsNullOrEmpty(nextScene))
            SceneManager.LoadScene(nextScene);
    }

    protected override void MissionFailedHandler() {
        if (debugEnabled) Debug.Log("=== Mission failed ===");
        if (!string.IsNullOrEmpty(failedScene))
            SceneManager.LoadScene(failedScene);
    }
}
