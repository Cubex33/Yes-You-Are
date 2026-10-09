using UnityEngine;

public class GravitationEffect : MonoBehaviour
{
    public GameObject gravitationEffectRoot;
    public PlayerController controller;
    [Header("Audio")]
    public AudioSource telepathySource;
    void Start()
    {
        controller.gravitation.OnGrabbed += GrabbedHandler;
        controller.gravitation.OnReleased += ReleasedHandler;
    }

    void OnDestroy()
    {
        controller.gravitation.OnGrabbed -= GrabbedHandler;
        controller.gravitation.OnReleased -= ReleasedHandler;
    }

    void ReleasedHandler(GameObject @object)
    {
        gravitationEffectRoot.SetActive(false);
    }

    void GrabbedHandler(GameObject @object)
    {
        gravitationEffectRoot.SetActive(true);
        telepathySource.Stop();
        telepathySource.Play();
    }
}