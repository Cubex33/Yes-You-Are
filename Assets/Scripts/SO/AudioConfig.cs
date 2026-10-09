using UnityEngine;

[CreateAssetMenu(menuName = "Lucy/Audio/Config", fileName = "AudioConfig")]
public class AudioConfig : ScriptableObject
{
    [Header("UI SFX")]
    public AudioClip hoverClip;
    public AudioClip pressClip;
    public AudioClip enterClip;
    public AudioClip exitClip;
    public AudioClip releaseClip;
}
