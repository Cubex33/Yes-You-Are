using TMPro;
using UnityEngine;
using Lucy.Core;
using IDosGames.WebBridge;

/// <summary>Completes the quest only when the configured boss dies.</summary>
public class DefeatBossMission : BaseMission
{
    [SerializeField] private Entity boss;
    [SerializeField] private TMP_Text objectiveText;
    public IDosWebsiteBridge bridge;

    public override void StartMission()
    {
        if (isActive || isCompleted) return;
        base.StartMission();
        if (boss == null)
        {
            Debug.LogError($"{nameof(DefeatBossMission)}: assign the boss Entity in the Inspector.", this);
            return;
        }
        boss.OnDeath += OnBossDeath;
        if (objectiveText != null) objectiveText.text = "Победите босса.";
        QuestProgressHUD.Show("Победите босса");
    }

    private void OnGUI()
    {
        if (isActive && objectiveText == null) GUI.Box(new Rect(24f, 24f, 320f, 42f), "Победите босса.");
    }

    private void OnBossDeath(object source)
    {
        if (boss != null) boss.OnDeath -= OnBossDeath;
        if (objectiveText != null) objectiveText.text = "Босс побеждён!";
        QuestProgressHUD.Show("Босс побеждён!", 1, 1);
        bridge.ReportGameCompleted();
        CompleteMission();
    }

    public override void ResetMission()
    {
        if (boss != null) boss.OnDeath -= OnBossDeath;
        base.ResetMission();
    }

    protected override void CompleteMission()
    {
        if (boss != null) boss.OnDeath -= OnBossDeath;
        base.CompleteMission();
    }
}
