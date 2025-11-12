using UnityEngine;
using System.Collections;

public class BossHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 30;
    public int currentHealth;

    [Header("Knockback")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.25f; // quanto tempo o boss fica "em knockback"
    [HideInInspector]
    public bool isKnockedBack = false; // flag pública pra outros scripts verem

    [Header("Morte")]
    public float deathDelay = 2.3f; // duração da animação de morte
    private bool isDead = false;
    public bool IsDead => isDead;

    private Rigidbody2D rb;
    private Animator animator;
    private Coroutine knockbackCoroutine;

    // Integração com a IA do boss
    private BossAI bossAI;

    void Start()
    {
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        bossAI = GetComponent<BossAI>();

        if (rb == null)
            Debug.LogWarning($"[BossHealth] Rigidbody2D não encontrado em '{name}'. Knockback não funcionará.");

        if (animator == null)
            Debug.LogWarning($"[BossHealth] Animator não encontrado em '{name}' ou em seus filhos.");

        if (bossAI == null)
            Debug.LogWarning($"[BossHealth] BossAI não encontrada em '{name}'. Certifique-se de que o script BossAI está no mesmo objeto.");
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(Vector2.zero, amount);
    }

    public void TakeDamage(Vector2 hitDirection, int amount)
    {
        if (isDead) return;

        // Se o boss estiver imune (fase "boss_immune"), não toma dano
        if (bossAI != null && bossAI.IsImmune)
            return;

        currentHealth -= amount;

        // aplica knockback se tiver direção válida e Rigidbody disponível
        if (rb != null && hitDirection != Vector2.zero)
        {
            // zera a velocidade antes pra aplicar um impulso consistente
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(hitDirection.normalized * knockbackForce, ForceMode2D.Impulse);

            // marca knockback e inicia coroutine para liberar depois
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(KnockbackRoutine(knockbackDuration));
        }

        if (currentHealth <= 0)
        {
            StartCoroutine(Die());
        }
    }

    private IEnumerator KnockbackRoutine(float duration)
    {
        isKnockedBack = true;
        yield return new WaitForSeconds(duration);
        isKnockedBack = false;
        knockbackCoroutine = null;
    }

    private IEnumerator Die()
    {
        if (isDead) yield break;
        isDead = true;

        // Avisa a BossAI que o boss morreu (ela já cuida do trigger "die" no Animator)
        if (bossAI != null)
        {
            bossAI.Die();
        }
        else if (animator != null)
        {
            // fallback se por algum motivo não tiver BossAI
            animator.SetTrigger("die");
        }

        // desativa TODOS os colliders do boss e filhos pra não colidir mais
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (var col in cols)
        {
            col.enabled = false;
        }

        // espera o tempo da animação de morte
        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }
}
