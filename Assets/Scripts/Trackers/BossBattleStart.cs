using Lucy.Core;
using UnityEngine;
using UnityEngine.Playables;

public class BossBattleStart : MonoBehaviour
{
    public Boss boss;
    public GameObject cutScenesCamera;
    public GameObject playerCamera;
    public Movement movement;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "startPos")
        {
            boss.enabled = true;
            playerCamera.SetActive(false);
            cutScenesCamera.SetActive(true);
            PlayableDirector playable = cutScenesCamera.GetComponent<PlayableDirector>();
            movement.enabled = false;
            playable.Play();
            Destroy(other.gameObject);
        }
    }
}
