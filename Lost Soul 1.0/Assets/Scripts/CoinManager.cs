using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;

    [Header("Configurações")]
    public int totalCoins = 0; // total de moedas do jogador
    public TextMeshProUGUI coinText; // texto da UI na cena
    public float displayTime = 1.5f; // tempo que o texto aparece antes de sumir
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // Evita duplicatas e mantém o objeto entre cenas
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Carrega moedas salvas
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);

        UpdateUI();

        // Atualiza UI ao trocar de cena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Procura automaticamente um TextMeshPro na nova cena
        if (coinText == null)
        {
            coinText = FindObjectOfType<TextMeshProUGUI>();
        }
        UpdateUI();
    }

    public void AddCoin(int amount)
    {
        totalCoins += amount;

        // Salva automaticamente
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.Save();

        // Atualiza o texto imediatamente antes do fade
        UpdateUI();

        // Para fade antigo, se estiver rodando
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeText());
    }

    private IEnumerator FadeText()
    {
        if (coinText == null) yield break;

        coinText.gameObject.SetActive(true);

        CanvasGroup canvasGroup = coinText.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = coinText.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;

        float timer = 0f;
        while (timer < displayTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Fade out
        float fadeDuration = 0.5f;
        float fadeTimer = 0f;
        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeDuration);
            yield return null;
        }

        // Não desativa se o jogador pegar outra moeda enquanto o fade está rolando
        if (canvasGroup.alpha <= 0.01f)
            coinText.gameObject.SetActive(false);
    }

    public void ResetCoins()
    {
        totalCoins = 0;
        PlayerPrefs.SetInt("TotalCoins", 0);
        PlayerPrefs.Save();
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (coinText != null)
        {
            coinText.text = totalCoins.ToString();
        }
    }
}
