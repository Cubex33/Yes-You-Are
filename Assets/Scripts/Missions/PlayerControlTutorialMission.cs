using TMPro;
using UnityEngine;
using Lucy.Core;

/// <summary>Short, event-driven tutorial for the controls used in the first level.</summary>
public class PlayerControlTutorialMission : BaseMission
{
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Canvas tutorialCanvas;
    private bool grabbed;
    private bool adjustedDistance;
    private bool startedCharging;
    private bool usedSuperAttack;
    private PlayerController player;

    private void Awake()
    {
        if (tutorialCanvas != null) tutorialCanvas.gameObject.SetActive(false);
    }

    public override void StartMission()
    {
        if (isActive || isCompleted) return;
        player = FindFirstObjectByType<PlayerController>();
        PlayerController.GravityGrabUsed += OnGrab;
        PlayerController.GrabDistanceAdjusted += OnDistance;
        PlayerController.StaminaChargeStarted += OnCharge;
        PlayerController.SuperAttackUsed += OnSuperAttack;
        if (tutorialCanvas != null) tutorialCanvas.gameObject.SetActive(true);
        base.StartMission();
        UpdateInstructions();
        QuestProgressHUD.Show("Обучение: гравимагия", 0, 4);
    }

    private bool IsPlayer(PlayerController source) => player == null || source == player;
    private void OnGrab(PlayerController source) { if (!isActive || !IsPlayer(source)) return; grabbed = true; UpdateInstructions(); }
    private void OnDistance(PlayerController source) { if (!isActive || !IsPlayer(source) || !grabbed) return; adjustedDistance = true; UpdateInstructions(); }
    private void OnCharge(PlayerController source) { if (!isActive || !IsPlayer(source) || !adjustedDistance) return; startedCharging = true; UpdateInstructions(); }
    private void OnSuperAttack(PlayerController source)
    {
        if (!isActive || !IsPlayer(source) || !grabbed || !adjustedDistance || !startedCharging) return;
        usedSuperAttack = true;
        UpdateInstructions();
        CompleteMission();
    }

    private void UpdateInstructions()
    {
        string message;
        int step;
        if (!grabbed) { message = "Удерживайте ПКМ, чтобы поднять предмет гравимагией."; step = 0; }
        else if (!adjustedDistance) { message = "Прокрутите колесо, чтобы приблизить или отдалить предмет."; step = 1; }
        else if (!startedCharging) { message = "Удерживайте R, чтобы зарядить стамину."; step = 2; }
        else { message = "При полной стамине удерживайте R и нажмите ЛКМ для суперудара."; step = 3; }
        if (instructionText != null) instructionText.text = "Обучение: " + message;
        if (isActive) QuestProgressHUD.Show(message, step, 4);
    }

    private void OnGUI()
    {
        if (!isActive || instructionText != null) return;
        string message = !grabbed ? "Обучение: удерживайте ПКМ, чтобы поднять предмет гравимагией."
            : !adjustedDistance ? "Прокрутите колесо мыши, чтобы приблизить или отдалить предмет."
            : !startedCharging ? "Удерживайте R, чтобы зарядить стамину."
            : "При полной стамине удерживайте R и нажмите ЛКМ для суперудара.";
        GUI.Box(new Rect(24f, 24f, Mathf.Min(Screen.width - 48f, 720f), 54f), message);
    }

    public override void ResetMission()
    {
        Unsubscribe();
        grabbed = adjustedDistance = startedCharging = usedSuperAttack = false;
        if (tutorialCanvas != null) tutorialCanvas.gameObject.SetActive(false);
        QuestProgressHUD.Hide();
        base.ResetMission();
    }

    protected override void CompleteMission()
    {
        Unsubscribe();
        if (instructionText != null) instructionText.text = string.Empty;
        if (tutorialCanvas != null) tutorialCanvas.gameObject.SetActive(false);
        QuestProgressHUD.Hide();
        base.CompleteMission();
    }

    private void OnDisable() => Unsubscribe();

    private void Unsubscribe()
    {
        PlayerController.GravityGrabUsed -= OnGrab;
        PlayerController.GrabDistanceAdjusted -= OnDistance;
        PlayerController.StaminaChargeStarted -= OnCharge;
        PlayerController.SuperAttackUsed -= OnSuperAttack;
    }
}
