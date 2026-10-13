using IDosGames.WebBridge;
using Lucy.Core;
using System;
using UnityEngine;
using UnityEngine.Playables;

public class SkeletDie : MonoBehaviour
{
    public Entity entity;
    public PlayableDirector PlayableDirector;
    public IDosWebsiteBridge bridge;

    private void Start()
    {
        entity.OnDamageTaken += OnDamageTaken;
    }

    private void OnDamageTaken(float arg1, object arg2)
    {
        if (entity.Health <= 0 || entity.IsDead)
        {
            PlayableDirector.Play();
            bridge.ReportGameCompleted();
        }
    }
}
