#region Enemy Clear Mission - spawn/activate enemies, respawn enemies on player death
using System;
using System.Collections;
using System.Collections.Generic;
using Lucy.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mission that takes a list of enemy templates (inactive scene GameObjects). On start it instantiates runtime enemies from those templates
/// and waits until the player kills all of them. If the player dies, the mission will call ResetMission() (player handles their own respawn).
/// NOTE: enemy templates are treated as prototypes — they will remain untouched. Runtime enemies are clones that can be destroyed.
/// </summary>
[DisallowMultipleComponent]
public class EnemyClearMission : ItemSpawnerMission
{
    [Header("UI elements to update")]
    public ClearEmemy_Canvas clearEmemy_Canvas;
    // runtime
    // activeEnemies handled by base.activeDisappearClones
    private int kills = 0;

    // playerEntity handled by base

    public override void StartMission()
    {
        if (isActive) return;
        base.StartMission();

        kills = 0;
        if (clearEmemy_Canvas.canvas != null)
            clearEmemy_Canvas.canvas.gameObject.SetActive(true);
        if (clearEmemy_Canvas.progressBar != null)
            clearEmemy_Canvas.progressBar.maxValue = disappearTemplates.Count;
    }

    protected override void OnItemsSpawned(List<GameObject> nonDisappear, List<GameObject> disappearClones)
    {
        foreach (var inst in disappearClones)
        {
            var entity = inst.GetComponentInChildren<Entity>();
            if (entity != null)
            {
                entity.OnDeath += OnEnemyDeath;
            }
            else
            {
                Debug.LogWarning($"[EnemyClearMission] Enemy clone '{inst.name}' has no Entity component — mission will treat its destruction as kill if it's destroyed.");
            }
        }

        if (disappearClones.Count == 0)
        {
            CompleteMission();
        }
    }



    protected override void CompleteMission()
    {
        base.CompleteMission();
    }

    protected override void FailMission()
    {
        base.FailMission();
    }

    private void OnEnemyDeath(object arg)
    {
        kills++;
        if (clearEmemy_Canvas.progressBar != null)
            clearEmemy_Canvas.progressBar.value = kills;
        Debug.Log($"[EnemyClearMission] Enemy died. Kills: {kills}/{disappearTemplates.Count}"); 

        if (kills >= disappearTemplates.Count)
        {
            CompleteMission();
        }
    }



    public override void ResetMission()
    {
        kills = 0;
        if (clearEmemy_Canvas.canvas != null)
            clearEmemy_Canvas.canvas.gameObject.SetActive(false);
        base.ResetMission();
    }
}
#endregion

[Serializable]
public struct ClearEmemy_Canvas
{
    public Canvas canvas;
    public Slider progressBar;
}