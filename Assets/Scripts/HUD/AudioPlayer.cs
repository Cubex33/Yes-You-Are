using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioPlayer : MonoBehaviour
{
    [Header("Mixer Settings")]
    public AudioMixer audioMixer;
    public string musicParam = "MusicVolume";
    public string sfxParam = "SFXVolume";
    public string masterParam = "MasterVolume";
    [Header("Internal Sources")]
    public AudioSettings audioSettings;
    public AudioSource musicSource;
    public AudioSource sfxSource;
    [Header("Settings")]
    public float fadeDuration = 1f;
    Coroutine _fadeCoroutine;

    void Start()
    {
        if (audioSettings != null)
        {
        }    
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        
        musicSource.clip = clip;
        musicSource.loop = loop;
        StartFade(1f);
        musicSource.Play();
    }

    public void StopMusic(bool fade = true)
    {
        if (fade) StartFade(0f);
        else musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip, Vector3? position = null, float volume = 1f)
    {
        if (clip == null) return;

        if (position == null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, position.Value, volume);
        }
    }
    static float ConverToDecibel(float sliderValue) => sliderValue > 0.0001f ? Mathf.Log10(sliderValue) * 20 : -80f;
    public void SetMusicVolume(float sliderValue) => audioMixer.SetFloat(musicParam, ConverToDecibel(sliderValue));
    public void SetSFXVolume(float sliderValue) => audioMixer.SetFloat(sfxParam, ConverToDecibel(sliderValue));
    public void SetMasterVolume(float sliderValue) => audioMixer.SetFloat(masterParam, ConverToDecibel(sliderValue));
    #region Fade

    void StartFade(float target)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(target));
    }

   IEnumerator FadeRoutine(float target)
    {
        float start = musicSource.volume;
        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(start, target, timer / fadeDuration);
            yield return null;
        }
        musicSource.volume = target;
        if (target <= 0) musicSource.Stop();
    }
    #endregion
}