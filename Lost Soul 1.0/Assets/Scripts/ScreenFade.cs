using UnityEngine;
using System.Collections;

public class ScreenFade : MonoBehaviour
{
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.5f;

    private void Awake()
    {
        if (fadeCanvasGroup == null)
            fadeCanvasGroup = GetComponent<CanvasGroup>();
    }

    public IEnumerator FadeOutIn(System.Action onMidFade = null)
    {
        // Fade para preto
        yield return StartCoroutine(Fade(1));

        // Executa a ação no meio do fade (respawn)
        onMidFade?.Invoke();

        // Pequena pausa no preto
        yield return new WaitForSeconds(0.1f);

        // Fade de volta
        yield return StartCoroutine(Fade(0));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}
