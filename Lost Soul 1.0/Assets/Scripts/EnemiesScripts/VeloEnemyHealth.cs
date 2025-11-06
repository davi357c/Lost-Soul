using UnityEngine;
using System.Collections;

public class VeloEnemyHealth : MonoBehaviour
{
    [Header("Configurações de Vida")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Knockback e Invulnerabilidade")]
    public float knockbackForce = 4f;
    public float invulnerableTime = 0.3f;

    [Header("Morte")]
    public GameObject deathEffect; // opcional (partículas)
    public float destroyDelay = 0.5f;

    private Animator anim;
    private Rigidbody2D rb;
    private bool isDead = false;
    private bool isInvulnerable = false;

    private EnemyAI enemyAI;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        enemyAI = GetComponent<EnemyAI>();
    }

    /// <summary>
    /// Aplica dano ao inimigo, com direção e knockback.
    /// Chamado pelo Player via AttackHitbox.
    /// </summary>
    public void TakeDamage(Vector2 hitDirection, int damage)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (anim != null)
            anim.SetTrigger("Hit");

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(hitDirection * knockbackForce, ForceMode2D.Impulse);
        }

        StartCoroutine(InvulnerabilityRoutine());
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;

        // feedback visual (piscar)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float blinkInterval = 0.1f;
        float timer = 0f;

        while (timer < invulnerableTime)
        {
            if (sr != null)
                sr.enabled = !sr.enabled;

            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        if (sr != null)
            sr.enabled = true;

        isInvulnerable = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (enemyAI != null)
        {
            // para IA e animações
            StopAllCoroutines();
            enemyAI.enabled = false;
        }

        if (anim != null)
            anim.SetTrigger("Die");

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // desativa o collider pra não interagir mais
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // pequena espera antes de destruir (pra animação tocar)
        Destroy(gameObject, destroyDelay);
    }
}
