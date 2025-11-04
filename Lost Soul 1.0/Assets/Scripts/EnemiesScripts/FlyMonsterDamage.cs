using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class FlyMonsterDamage : MonoBehaviour
{
    [Header("Explosão / Dano em área")]
    public int explosionDamage = 2;
    public float explosionRadius = 1.8f;
    public LayerMask playerLayer;

    [Header("Proximidade")]
    public float proximityDistance = 2f; // se o player chegar tão perto, começa a preparar a explosão

    [Header("Piscando antes de explodir")]
    [Tooltip("Tempo que o inimigo fica piscando antes de explodir.")]
    public float preExplosionBlinkTime = 3f;
    [Tooltip("Intervalo entre cada piscar.")]
    public float blinkInterval = 0.15f;

    [Header("Configurações de morte")]
    public float deathDelayFallback = 1.0f; // usado caso não ache FlyEnemyHealth

    private bool hasExploded = false;
    private bool isPreparingToExplode = false;

    private Animator animator;
    private FlyEnemyHealth enemyHealth;
    private Collider2D mainCollider;
    private Rigidbody2D rb;
    private Transform playerTransform;
    private SpriteRenderer[] spriteRenderers;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<FlyEnemyHealth>();
        mainCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) playerTransform = p.transform;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        // se o collider for trigger para detectar "encostar", ok; se for físico, OnCollision também pode explodir
        if (mainCollider == null)
            Debug.LogWarning($"[{name}] Collider2D não encontrado no inimigo voador.");
    }

    void Update()
    {
        if (hasExploded) return;

        // Atualiza referência do player se perder
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // Explosão por proximidade só se ainda não estiver preparando nem tiver explodido
        if (!isPreparingToExplode && playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= proximityDistance)
            {
                float delay = enemyHealth != null ? enemyHealth.deathDelay : deathDelayFallback;
                StartExplosionSequence(delay);
            }
        }
    }

    // Quando o player encostar (trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded || isPreparingToExplode) return;

        if (other.CompareTag("Player"))
        {
            float delay = enemyHealth != null ? enemyHealth.deathDelay : deathDelayFallback;
            StartExplosionSequence(delay);
        }
    }

    // Caso esteja usando colisão física (não trigger)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasExploded || isPreparingToExplode) return;

        if (collision.collider.CompareTag("Player"))
        {
            float delay = enemyHealth != null ? enemyHealth.deathDelay : deathDelayFallback;
            StartExplosionSequence(delay);
        }
    }

    // Chamado pelo FlyEnemyHealth quando é morto (para que a morte por dano também cause a explosão)
    public void HandleDeathFromHealth(float deathDelay)
    {
        if (hasExploded || isPreparingToExplode) return;
        StartExplosionSequence(deathDelay);
    }

    /// <summary>
    /// Inicia a rotina de piscar por alguns segundos e depois explodir.
    /// </summary>
    private void StartExplosionSequence(float deathDelay)
    {
        if (hasExploded || isPreparingToExplode) return;
        StartCoroutine(BlinkThenExplode(deathDelay));
    }

    private IEnumerator BlinkThenExplode(float deathDelay)
    {
        isPreparingToExplode = true;

        // Para o movimento (se tiver Rigidbody2D)
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        float elapsed = 0f;
        bool visible = true;

        // Loop de piscar
        while (elapsed < preExplosionBlinkTime)
        {
            SetSpritesVisible(visible);
            visible = !visible;

            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        // Garante que fique visível no momento da explosão
        SetSpritesVisible(true);

        // Se tiver sistema de vida, força a vida para 0 na hora da explosão
        if (enemyHealth != null)
        {
            enemyHealth.currentHealth = 0;

            // Marca o inimigo como morto para outros scripts (ex.: movimento) usando reflection no campo privado isDead
            var field = typeof(FlyEnemyHealth).GetField(
                "isDead",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (field != null)
                field.SetValue(enemyHealth, true);
        }

        // Agora executa a explosão de fato (dano em área + animação + destruir)
        yield return StartCoroutine(ExplodeAndDieInternal(deathDelay));
    }

    private void SetSpritesVisible(bool visible)
    {
        if (spriteRenderers == null) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].enabled = visible;
        }
    }

    /// <summary>
    /// Função interna que realmente aplica dano, toca animação e destrói o inimigo.
    /// </summary>
    private IEnumerator ExplodeAndDieInternal(float deathDelay)
    {
        if (hasExploded) yield break;
        hasExploded = true;
        isPreparingToExplode = false;

        // dispara animação de morte/explosão
        if (animator != null)
            animator.SetTrigger("Death");

        // desativa colisores
        if (mainCollider != null)
            mainCollider.enabled = false;

        // para o rigidbody
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // aplica o dano em área imediatamente
        ApplyAreaDamage();

        // espera o tempo da animação antes de destruir
        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }

    public void ApplyAreaDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, playerLayer);
        foreach (Collider2D col in hits)
        {
            if (col == null) continue;

            PlayerHealth ph = col.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                Vector2 dir = (col.transform.position - transform.position).normalized;
                ph.TakeDamage(explosionDamage, dir);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, proximityDistance);
    }
}
