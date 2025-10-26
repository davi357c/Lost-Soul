using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vidas")]
    public int maxLives = 5;
    public int currentLives;

    [Header("UI de Corações")]
    public Animator[] hearts;

    [Header("Configurações de Dano")]
    public float invulnerableTime = 1f;
    public float knockbackForce = 5f;

    private bool isInvulnerable = false;
    private bool isDead = false;

    private Animator animator;
    private Rigidbody2D rb;
    private PlayerMovement movement;

    void Awake()
    {
        // Garante que só exista um PlayerHealth na cena
        if (FindObjectsOfType<PlayerHealth>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        currentLives = maxLives;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();

        UpdateHeartsUI();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reatribui os corações da nova cena
        GameObject container = GameObject.Find("HeartsContainer");
        if (container != null)
        {
            hearts = container.GetComponentsInChildren<Animator>();
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

        animator.SetTrigger("Hit");
        StartCoroutine(InvulnerabilityRoutine());

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(hitDirection.x * knockbackForce, knockbackForce), ForceMode2D.Impulse);

        movement.Respawn(0.3f);
    }

    void UpdateHeartsUI()
    {
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

        animator.SetTrigger("Die");
        movement.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
    }

    IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float blinkInterval = 0.1f;
        float timer = 0f;
        while (timer < invulnerableTime)
        {
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }
        sr.enabled = true;
        isInvulnerable = false;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
