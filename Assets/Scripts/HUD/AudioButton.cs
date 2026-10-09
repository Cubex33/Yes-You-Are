using UnityEngine;
using UnityEngine.EventSystems;

namespace Lucy.Game.Audio
{
    public class AudioButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public AudioConfig audioConfig;
        public AudioPlayer AudioService;
        public void OnPointerClick(PointerEventData _) => Play(audioConfig.pressClip);
        public void OnPointerEnter(PointerEventData _) => Play(audioConfig.hoverClip);
        public void OnPointerExit(PointerEventData _)  => Play(audioConfig.exitClip);
        public void OnPointerDown(PointerEventData _)  => Play(audioConfig.pressClip);
        public void OnPointerUp(PointerEventData _)    => Play(audioConfig.releaseClip);
        private void Play(AudioClip clip) { if (clip != null && AudioService != null) AudioService.PlaySFX(clip); }
    }
}