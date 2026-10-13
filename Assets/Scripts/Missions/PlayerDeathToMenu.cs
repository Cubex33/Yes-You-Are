
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Lucy.Core;

public class PlayerDeathToMenu : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Entity playerEntity;

    [Header("Main Menu")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Death Screen")]
    [SerializeField] private string deathMessage = "Are you stupid?";
    [SerializeField, Min(1)] private int fontSize = 48;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float blackFadeDuration = 1f;
    [SerializeField, Min(0f)] private float textFadeDuration = 1.2f;
    [SerializeField, Min(0f)] private float messageDuration = 2.5f;

    private bool isLoadingMenu;
    private Image backgroundImage;
    private TextMeshProUGUI messageText;

    private void Start()
    {
        if (playerEntity == null)
        {
            PlayerController player =
                FindAnyObjectByType<PlayerController>();

            if (player != null)
                playerEntity = player.GetComponentInChildren<Entity>();
        }

        if (playerEntity == null)
        {
            Debug.LogError(
                "[PlayerDeathToMenu] Player Entity не найден!"
            );
            return;
        }

        playerEntity.OnDeath += HandlePlayerDeath;
    }

    private void HandlePlayerDeath(object source)
    {
        if (isLoadingMenu)
            return;

        isLoadingMenu = true;

        // Больше не принимаем урон и ввод во время перехода.
        StartCoroutine(ShowDeathScreenAndLoadMenu());
    }

    private IEnumerator ShowDeathScreenAndLoadMenu()
    {
        CreateDeathScreen();

        // Останавливаем игровое время, но сохраняем работу анимации.
        Time.timeScale = 0f;

        // Затемняем экран.
        yield return FadeImage(
            backgroundImage, 0f, 1f, blackFadeDuration
        );

        // Показываем надпись.
        yield return FadeText(
            messageText, 0f, 1f, textFadeDuration
        );

        // Удерживаем надпись на экране.
        yield return new WaitForSecondsRealtime(messageDuration);

        // Восстанавливаем время перед загрузкой меню.
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogError(
                $"[PlayerDeathToMenu] Сцена '{menuSceneName}' " +
                "не найдена. Проверь имя сцены и Build Settings."
            );
        }
    }

    private void CreateDeathScreen()
    {
        GameObject canvasObject = new GameObject("DeathScreenCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        // Чёрный фон.
        GameObject background = new GameObject(
            "BlackBackground",
            typeof(RectTransform),
            typeof(Image)
        );

        background.transform.SetParent(canvasObject.transform, false);

        RectTransform backgroundRect =
            background.GetComponent<RectTransform>();

        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0f);
        backgroundImage.raycastTarget = false;

        // Текст по центру.
        GameObject textObject = new GameObject(
            "DeathMessage",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(30f, 30f);
        textRect.offsetMax = new Vector2(-30f, -30f);

        messageText = textObject.GetComponent<TextMeshProUGUI>();
        messageText.text = deathMessage;
        messageText.fontSize = fontSize;
        messageText.color = new Color(1f, 1f, 1f, 0f);
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.textWrappingMode = TextWrappingModes.NoWrap;

        if (TMP_Settings.defaultFontAsset != null)
        {
            messageText.font = TMP_Settings.defaultFontAsset;
        }
        else
        {
            Debug.LogWarning(
                "[PlayerDeathToMenu] TMP Font Asset не найден. " +
                "Проверь TMP Essential Resources."
            );
        }
    }

    private IEnumerator FadeImage(
        Image image, float startAlpha, float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetImageAlpha(image, targetAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(elapsed / duration));

            SetImageAlpha(image, Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetImageAlpha(image, targetAlpha);
    }

    private IEnumerator FadeText(
        TextMeshProUGUI text, float startAlpha, float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetTextAlpha(text, targetAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(elapsed / duration));

            SetTextAlpha(text, Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetTextAlpha(text, targetAlpha);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private void OnDestroy()
    {
        if (playerEntity != null)
            playerEntity.OnDeath -= HandlePlayerDeath;
    }
}
