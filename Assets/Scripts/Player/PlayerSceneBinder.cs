using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.InputSystem;
using Lucy.Core;
public class PlayerSceneBinder : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public Transform spawnPoint;
    public Canvas hud;
    public PlayableDirector deathTimeline;
    public PlayerController playerController;
    bool waitingForRespawnInput;
    bool deathCutscenePlaying;
    void Awake()
    {
        if (player == null) player = FindAnyObjectByType<Player>();
        deathTimeline.stopped += OnDeathTimelineStopped;
        InputProvider.Global.Controls.Cutscene.Any.performed += OnRespawnPressed;
        player.OnDeath += HandlePlayerDeath;
    }
    void OnDestroy()
    {
        if (deathTimeline != null) deathTimeline.stopped -= OnDeathTimelineStopped;
        if (InputProvider.instance != null) InputProvider.Global.Controls.Cutscene.Any.performed -= OnRespawnPressed;
        if (player != null) player.OnDeath -= HandlePlayerDeath;
    }
    void OnDeathTimelineStopped(PlayableDirector director)
    {
        if (director != deathTimeline) return;
        deathCutscenePlaying = false;
        waitingForRespawnInput = true;
    }
    void RespawnPlayer()
    {
        ResetDeathTimeline();
        player.RespawnAt(spawnPoint);
        playerController.enabled = true;
        hud.gameObject.SetActive(true);
        if (InputProvider.instance != null) {
            InputProvider.Global.Controls.Cutscene.Disable();
            InputProvider.Global.Controls.Player.Enable();
        }
    }
    void OnRespawnPressed(InputAction.CallbackContext ctx)
    {
        if (deathCutscenePlaying)
        {
            ResetDeathTimeline();
            deathCutscenePlaying = false;
            waitingForRespawnInput = true;
            return;
        }
        if (waitingForRespawnInput)
        {
            waitingForRespawnInput = false;
            RespawnPlayer();
        }
    }
    void HandlePlayerDeath(object source)
    {
        playerController.enabled = false;
        hud.gameObject.SetActive(false);
        InputProvider.Global.Controls.Player.Disable();
        InputProvider.Global.Controls.Cutscene.Enable();
        waitingForRespawnInput = false;
        deathCutscenePlaying = true;
        ResetDeathTimeline();
        deathTimeline.Play();
    }
    void ResetDeathTimeline()
    {
        deathTimeline.Stop();
        deathTimeline.time = 0;
        deathTimeline.Evaluate();
    }
}