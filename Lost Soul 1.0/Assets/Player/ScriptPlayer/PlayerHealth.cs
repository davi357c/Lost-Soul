using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vidas")]
    public int maxLives = 5;
    public int currentLives = -1; // inicia com -1 pra sabermos se já foi inicializado

    [Header("UI de Corações")]
    public Animator[] hearts;

    [Header("Opcional: Prefab do HeartsContainer (UI)")]
    public GameObject heartsPrefab; // arrasta aqui o prefab com o Canvas + ObjectAleatorio

    [Header("Configurações de Dano")]
    public float invulnerableTime = 1f;
    public float knockbackForce = 5f;

    private bool isInvulnerable = false;
    private bool isDead = false;

    private Animator animator;
    private Rigidbody2D rb;
    private PlayerMovement movement;

    private static PlayerHealth instance;

    void Awake()
    {
        // Singleton robusto (só 1 PlayerHealth existe)
        if (instance != null && instance != this)
        {
            Debug.Log("[PlayerHealth] Outro PlayerHealth encontrado - destruindo este.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // Só define a vida se ainda não foi inicializada
        if (currentLives <= 0)
            currentLives = maxLives;

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();

        Debug.Log($"[PlayerHealth] Start - currentLives={currentLives}, heartsArrayLength={hearts?.Length ?? 0}");
        UpdateHeartsUI();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[PlayerHealth] Cena carregada: " + scene.name);

        // Tenta encontrar o container da cena atual (ajustado para "ObjectAleatorio")
        GameObject container = GameObject.Find("ObjectAleatorio");
        if (container != null)
        {
            hearts = container.GetComponentsInChildren<Animator>();
            Debug.Log("[PlayerHealth] ObjectAleatorio encontrado. Hearts count = " + hearts.Length);
            UpdateHeartsUI();
            return;
        }

        Debug.LogWarning("[PlayerHealth] ObjectAleatorio não encontrado na cena.");

        // Se não encontrar e tiver prefab, instancia (apenas uma vez)
        if (heartsPrefab != null && GameObject.Find(heartsPrefab.name) == null)
        {
            GameObject heartsObj = Instantiate(heartsPrefab);
            heartsObj.name = heartsPrefab.name;
            DontDestroyOnLoad(heartsObj);

            Transform containerTransform = heartsObj.transform.Find("ObjectAleatorio");
            if (containerTransform != null)
                hearts = containerTransform.GetComponentsInChildren<Animator>();
            else
                hearts = heartsObj.GetComponentsInChildren<Animator>();

            Debug.Log("[PlayerHealth] Hearts prefab instanciado via código. Hearts count = " + hearts.Length);
            UpdateHeartsUI();
        }
    }

    public void TakeDamage(Vector2 hitDirection)
    {
        if (isInvulnerable || isDead) return;

        currentLives--;
        UpdateHeartsUI();

        if (currentLives <= 0)
        {
            Die();
            return;
        }

        if (animator != null) animator.SetTrigger("Hit");
        StartCoroutine(InvulnerabilityRoutine());

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(hitDirection.x * knockbackForce, knockbackForce), ForceMode2D.Impulse);
        }

        if (movement != null) movement.Respawn(0.3f);
    }

    void UpdateHeartsUI()
    {
        if (hearts == null || hearts.Length == 0)
        {
            Debug.LogWarning("[PlayerHealth] UpdateHeartsUI: hearts array vazio.");
            return;
        }

        for (int i = 0; i < hearts.Length; i++)
        {
            bool isAlive = i < currentLives;
            hearts[i].gameObject.SetActive(isAlive);
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null) animator.SetTrigger("Die");
        if (movement != null) movement.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
    }

    IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float blinkInterval = 0.1f;
        float timer = 0f;
        while (timer < invulnerableTime)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }
        if (sr != null) sr.enabled = true;
        isInvulnerable = false;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ChangeHealth(int amount)
    {
        // Garante que o player não está morto
        if (isDead) return;

        // Altera a vida (positiva = cura, negativa = dano)
        currentLives += amount;

        // Limita a vida entre 0 e o máximo
        currentLives = Mathf.Clamp(currentLives, 0, maxLives);

        // Atualiza o UI
        UpdateHeartsUI();

        // Se a vida chegou a 0, morre
        if (currentLives <= 0)
        {
            Die();
        }
    }

}
