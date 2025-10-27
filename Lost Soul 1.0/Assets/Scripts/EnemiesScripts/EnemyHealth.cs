using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida do Inimigo")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Feedback de Dano")]
    public float knockbackForce = 5f;
    public float hitFlashTime = 0.1f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private EnemyAI ai;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        ai = GetComponent<EnemyAI>();
    }

    public void TakeDamage(Vector2 hitDirection, int damage)
    {
        currentHealth -= damage;

        if (animator != null)
            animator.SetTrigger("Hit");

        StartCoroutine(FlashRoutine());

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(hitDirection.x * knockbackForce, knockbackForce), ForceMode2D.Impulse);
        }

        if (currentHealth <= 0)
            Die();
    }

    IEnumerator FlashRoutine()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(hitFlashTime);
        spriteRenderer.color = Color.white;
    }

    void Die()
    {
        if (ai != null)
            ai.Die();
    }
}
