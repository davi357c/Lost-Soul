using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition instance;

    [Header("Fade")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.2f; // Tempo do fade

    private bool isFading = false;

    private void Awake()
    {
        // Singleton seguro
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Se esquecer de arrastar, tenta achar automaticamente
        if (fadeCanvasGroup == null)
            fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Start()
    {
        if (fadeCanvasGroup != null)
            StartCoroutine(Fade(1f, 0f));  // fade de preto (1) pra transparente (0)
    }


    public void FadeToScene(string sceneName)
    {
        if (isFading) return;
        if (fadeCanvasGroup == null) return;

        StartCoroutine(FadeAndSwitchScenes(sceneName));
    }

    private IEnumerator FadeAndSwitchScenes(string sceneName)
    {
        isFading = true;

        // Fade out (escurecer)
        yield return StartCoroutine(Fade(0f, 1f));

        // Troca de cena
        SceneManager.LoadScene(sceneName);

        // Espera 1 frame pra garantir que a cena carregou
        yield return null;

        // Garante que ainda temos um CanvasGroup válido
        if (fadeCanvasGroup == null)
            fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();

        // Fade in (clarear)
        if (fadeCanvasGroup != null)
            yield return StartCoroutine(Fade(1f, 0f));

        isFading = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeCanvasGroup == null)
            yield break;

        // Se estiver quase totalmente visível, bloqueia raycasts
        fadeCanvasGroup.blocksRaycasts = (from < to);

        float t = 0f;
        while (t < fadeDuration)
        {
            if (fadeCanvasGroup == null) // se foi destruído no meio, sai
                yield break;

            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / fadeDuration);
            fadeCanvasGroup.alpha = a;
            yield return null;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = to;
            fadeCanvasGroup.blocksRaycasts = (to > 0.99f);
        }
    }

    // Opcional: chamar pra garantir que o fade comece transparente
    public void ResetFade()
    {
        if (fadeCanvasGroup == null) return;

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }
}
