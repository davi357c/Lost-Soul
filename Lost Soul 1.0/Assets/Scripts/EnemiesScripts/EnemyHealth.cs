using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 3;
    public int currentHealth;

    [Header("Knockback")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.25f; // quanto tempo o inimigo fica "em knockback"
    [HideInInspector]
    public bool isKnockedBack = false; // flag pública para outros scripts verificarem

    [Header("Morte")]
    public float deathDelay = 2.3f; // duração da animação de morte
    private bool isDead = false;
    public bool IsDead => isDead; 


    private Rigidbody2D rb;
    private Animator animator;
    private Coroutine knockbackCoroutine;

    void Start()
    {
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        // procura o Animator no próprio objeto ou em um filho (sprite)
        animator = GetComponentInChildren<Animator>();

        if (rb == null)
            Debug.LogWarning($"[EnemyHealth] Rigidbody2D não encontrado em '{name}'. Knockback não funcionará.");
        if (animator == null)
            Debug.LogWarning($"[EnemyHealth] Animator não encontrado em '{name}' ou em seus filhos.");
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(Vector2.zero, amount);
    }

    public void TakeDamage(Vector2 hitDirection, int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        // aplica knockback se houver direção válida e Rigidbody disponível
        if (rb != null && hitDirection != Vector2.zero)
        {
            // zera a velocidade antes para aplicar um impulso consistente
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

        // aciona animação de morte, se houver
        if (animator != null)
            animator.SetTrigger("Death");

        // desativa o collider pra não colidir mais
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // espera o tempo da animação
        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }
}
