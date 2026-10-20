
using IDosGames.WebBridge;
using Lucy.Core;
using UnityEngine;
using UnityEngine.Playables;

public class SkeletDie : MonoBehaviour
{
    public Entity entity;
    public PlayableDirector PlayableDirector;
    public IDosWebsiteBridge bridge;

    private bool completionHandled;

    private void Start()
    {
        if (entity == null)
        {
            Debug.LogError(
                "[SkeletDie] Entity не назначен в Inspector.",
                this
            );
            enabled = false;
            return;
        }

        entity.OnDamageTaken += OnDamageTaken;
    }

    private void OnDestroy()
    {
        if (entity != null)
        {
            entity.OnDamageTaken -= OnDamageTaken;
        }
    }

    private void OnDamageTaken(float damage, object source)
    {
        if (completionHandled || entity == null)
            return;

        if (entity.Health > 0 && !entity.IsDead)
            return;

        completionHandled = true;

        if (PlayableDirector != null)
        {
            PlayableDirector.Play();
        }
        else
        {
            Debug.LogWarning(
                "[SkeletDie] PlayableDirector не назначен. " +
                "‘инальна€ последовательность не будет запущена.",
                this
            );
        }

        if (bridge != null)
        {
            bridge.ReportGameCompleted();
        }
        else
        {
            Debug.LogWarning(
                "[SkeletDie] Bridge не назначен или недоступен. " +
                "”ведомление о завершении игры не отправлено.",
                this
            );
        }
    }
}
