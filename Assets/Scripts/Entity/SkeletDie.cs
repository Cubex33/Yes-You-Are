using IDosGames;
using Lucy.Core;
using System;
using UnityEngine;
using UnityEngine.Playables;

public class SkeletDie : MonoBehaviour
{
    public Entity entity;
    public PlayableDirector PlayableDirector;

    private void Start()
    {
        entity.OnDamageTaken += OnDamageTaken;
    }

    private void OnDamageTaken(float arg1, object arg2)
    {
        if (entity.Health <= 0 || entity.IsDead)
        {
            PlayableDirector.Play();
            ClaimRewardSystem.ClaimCoinReward(100, 0);
            IGSClientAPI.ClaimTokenReward(10, 0, result => Debug.Log("Награда обработана"),error => Debug.LogError(error)); }
    }
}
