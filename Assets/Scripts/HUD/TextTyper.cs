using System.Collections;
using TMPro;
using UnityEngine;

public class TextTyper : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float charDelay = 0.03f;

    public Coroutine typingCoroutine;
    public bool isTyping = false;
    public string fullText;

    public void StartTyping(string text)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeText(text));
    }

    IEnumerator TypeText(string text)
    {
        fullText = text;

        textMesh.text = text;
        textMesh.maxVisibleCharacters = 0;

        isTyping = true;

        int total = text.Length;

        while (textMesh.maxVisibleCharacters < total)
        {
            textMesh.maxVisibleCharacters++;
            yield return new WaitForSeconds(charDelay);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    public void Skip()
    {
        if (!isTyping) return;

        StopCoroutine(typingCoroutine);

        textMesh.maxVisibleCharacters = fullText.Length;

        isTyping = false;
        typingCoroutine = null;
    }

    public bool IsTyping() => isTyping;
}