using UnityEngine;
using Lucy.Core;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] protected GameObject pauseMenuUI;
    [SerializeField] protected GameObject[] HUD;
    [SerializeField] protected bool pauseOnLoad = false;
    [SerializeField] protected bool debug = false;
    public Volume volume;

    public UnityEvent OnOpen;
    public UnityEvent OnClose;
    protected bool isPaused = false;
    protected struct HUDShot
    {
        public GameObject obj;
        public bool active;
    }
    protected bool[] hudStates;

    void Awake()
    {
        InputProvider.Global.Controls.UI.Pause.performed += ctx => TogglePause();
        hudStates = new bool[HUD.Length];
        if (pauseOnLoad) Pause(); else Resume();
    }

    void OnEnable() => InputProvider.Global.Controls.UI.Enable();

    void OnDisable() { if (InputProvider.instance != null) InputProvider.Global.Controls.UI.Disable(); }

    protected void TogglePause()
    {
        if (debug) Debug.Log("[PauseMenu] Toggled");
        if (isPaused) Resume();
        else Pause();
    }

    public void SetBloom(float intensity)
    {
        if (volume.profile.TryGet(out Bloom bloomComponent))
        {
            Bloom bloom = bloomComponent;
            if (bloom != null) bloom.intensity.value = intensity;
            else Debug.LogWarning("[PauseMenu] No Bloom found in the scene.");
        }
        else Debug.LogWarning("[PauseMenu] No Bloom found in the scene.");
        
    }

    public virtual void Pause()
    {
        pauseMenuUI.SetActive(true);
        for (int i = 0; i < HUD.Length; i++) { if (HUD[i] != null) { hudStates[i] = HUD[i].activeSelf; HUD[i].SetActive(false); } }
        Time.timeScale = 0f;
        isPaused = true;
        if (InputProvider.instance != null) InputProvider.Global.Controls.Player.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        OnOpen?.Invoke();
    }

    public virtual void Resume()
    {
        pauseMenuUI.SetActive(false);
        for (int i = 0; i < HUD.Length; i++) { if (HUD[i] != null) HUD[i].SetActive(hudStates[i]); }
        Time.timeScale = 1f;
        isPaused = false;
        if (InputProvider.instance != null) InputProvider.Global.Controls.Player.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        OnClose?.Invoke();
    }


    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void ChangeScene(string sceneName)
    {
        Pause();
        SceneManager.LoadScene(sceneName);
    }
}