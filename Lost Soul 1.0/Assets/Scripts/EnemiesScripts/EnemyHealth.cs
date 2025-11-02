using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 3;
    public int currentHealth;

    [Header("Knockback")]
    public float knockbackForce = 5f;

    [Header("Morte")]
    public float deathDelay = 1.0f; // tempo para animação de morte
    private bool isDead = false;

    Rigidbody2D rb;
    Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb == null)
            Debug.LogWarning($"[EnemyHealth] Rigidbody2D não encontrado em '{name}'. Knockback não funcionará.");
        if (animator == null)
            Debug.LogWarning($"[EnemyHealth] Animator não encontrado em '{name}'. Animação de morte não funcionará.");
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(Vector2.zero, amount);
    }

    public void TakeDamage(Vector2 hitDirection, int amount)
    {
        if (isDead) return; // evita chamar TakeDamage depois da morte

        currentHealth -= amount;

        // Aplica knockback se houver Rigidbody2D e houver direção válida
        if (rb != null && hitDirection != Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(hitDirection.normalized * knockbackForce, ForceMode2D.Impulse);
        }

        if (currentHealth <= 0)
        {
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        isDead = true;

        // Toca animação de morte
        if (animator != null)
            animator.SetTrigger("Death");

        // Espera a animação terminar (ou tempo definido)
        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }
}
