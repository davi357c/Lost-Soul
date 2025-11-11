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

    [Header("Portal que aparece ao morrer")]
    [Tooltip("Arraste um GameObject de portal já presente na cena (deixe desativado)")]
    public GameObject portalToEnable;    // opção A: habilitar portal já presente
    [Tooltip("OU arraste um prefab de portal para ser instanciado")]
    public GameObject portalPrefab;      // opção B: instanciar prefab
    [Tooltip("Se instanciar prefab, ponto onde ele aparece (opcional)")]
    public Transform portalSpawnPoint;
    [Tooltip("Aguardar X segundos após a morte antes do portal aparecer")]
    public float portalDelay = 1.0f;

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

        // para IA e movimento
        if (enemyAI != null)
        {
            StopAllCoroutines();
            enemyAI.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 3f; // pra cair até o chão
        }

        if (anim != null)
            anim.SetTrigger("Die");

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // desativa o collider pra não interagir mais
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // Não destruir — ele fica ali morto
        // Inicia rotina que aparecerá o portal
        StartCoroutine(SpawnPortalRoutine());
    }

    private IEnumerator SpawnPortalRoutine()
    {
        yield return new WaitForSeconds(portalDelay);

        // Opção A: habilitar portal já existente
        if (portalToEnable != null)
        {
            portalToEnable.SetActive(true);
            yield break;
        }

        // Opção B: instanciar prefab
        if (portalPrefab != null)
        {
            Vector3 pos = portalSpawnPoint != null ? portalSpawnPoint.position : transform.position;
            Instantiate(portalPrefab, pos, Quaternion.identity);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead && collision.gameObject.CompareTag("Ground"))
        {
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
}
