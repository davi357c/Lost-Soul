using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition instance;

    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.2f; // Tempo do fade

    private bool isFading = false;

    void Awake()
    {
        // Singleton: só 1 instance em todas as cenas
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    public void FadeToScene(string sceneName)
    {
        if (!isFading)
        {
            StartCoroutine(FadeAndSwitchScenes(sceneName));
        }
    }

    private IEnumerator FadeAndSwitchScenes(string sceneName)
    {
        isFading = true;

        // Começa o fade out (escurecer)
        yield return StartCoroutine(FadeOut());

        // Troca de cena
        SceneManager.LoadScene(sceneName);

        // Espera 1 frame pra garantir que a cena carregou
        yield return null;

        // Começa o fade in (clarear)
        yield return StartCoroutine(FadeIn());

        isFading = false;
    }

    private IEnumerator FadeOut()
    {
        fadeCanvasGroup.blocksRaycasts = true; // Bloqueia interações

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeIn()
    {
        fadeCanvasGroup.blocksRaycasts = false; // Libera interações

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
    }
}
