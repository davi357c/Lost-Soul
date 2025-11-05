using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class FlyMonsterDamage : MonoBehaviour
{
    [Header("Explosão / Dano em área")]
    public int explosionDamage = 2;
    public float explosionRadius = 1.8f;
    public LayerMask playerLayer;

    [Header("Proximidade para explodir")]
    [Tooltip("Se o player chegar nessa distância, o inimigo começa a preparar a explosão.")]
    public float proximityDistance = 2f;

    [Header("Piscando antes de explodir")]
    [Tooltip("Tempo que o inimigo fica piscando antes de explodir.")]
    public float preExplosionBlinkTime = 3f;
    [Tooltip("Intervalo entre cada piscada.")]
    public float blinkInterval = 0.15f;

    [Header("Dano por contato (encostar no inimigo)")]
    [Tooltip("Dano que o player toma imediatamente ao encostar no inimigo.")]
    public int contactDamage = 1;

    [Header("Configurações de morte")]
    [Tooltip("Usado caso não exista FlyEnemyHealth ou o campo deathDelay não esteja configurado.")]
    public float deathDelayFallback = 1.0f;

    private bool hasExploded = false;
    private bool isPreparingToExplode = false;

    private Animator animator;
    private FlyEnemyHealth enemyHealth;
    private Collider2D mainCollider;
    private Rigidbody2D rb;
    private FlyingMonsterMovement flyingMovement;
    private Transform playerTransform;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<FlyEnemyHealth>();
        mainCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        flyingMovement = GetComponent<FlyingMonsterMovement>();

        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) playerTransform = p.transform;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                    originalColors[i] = spriteRenderers[i].color;
            }
        }

        if (mainCollider == null)
            Debug.LogWarning($"[{name}] Collider2D não encontrado no inimigo voador.");
    }

    void Update()
    {
        if (hasExploded || isPreparingToExplode) return;

        // Atualiza referência do player se perder
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerTransform == null) return;

        // Explosão por proximidade
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= proximityDistance)
        {
            float delay = (enemyHealth != null) ? enemyHealth.deathDelay : deathDelayFallback;
            StartExplosionSequence(delay);
        }
    }

    // Quando o player encostar (trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded || isPreparingToExplode) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            DamagePlayerOnContact(ph, other.transform);

            float delay = (enemyHealth != null) ? enemyHealth.deathDelay : deathDelayFallback;
            StartExplosionSequence(delay);
        }
    }

    // Caso esteja usando colisão física (não trigger)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasExploded || isPreparingToExplode) return;

        PlayerHealth ph = collision.collider.GetComponent<PlayerHealth>() ??
                          collision.collider.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            DamagePlayerOnContact(ph, collision.collider.transform);

            float delay = (enemyHealth != null) ? enemyHealth.deathDelay : deathDelayFallback;
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
    /// Aqui também travamos o movimento e as colisões para parar de empurrar o player.
    /// </summary>
    private void StartExplosionSequence(float deathDelay)
    {
        if (hasExploded || isPreparingToExplode) return;
        isPreparingToExplode = true;

        // Marca o inimigo como morto no sistema de vida, se existir
        if (enemyHealth != null)
        {
            enemyHealth.currentHealth = 0;
            enemyHealth.ForceDeadState(); // IsDead = true
        }

        // Desativa totalmente o movimento do inimigo (inclui colliders + RB.simulated = false)
        if (flyingMovement != null)
            flyingMovement.DisableMovement();
        else
        {
            // fallback: se por algum motivo não houver script de movimento, ainda assim desabilita física e colliders (objeto + filhos + pai)
            DisableAllCollidersAndPhysics();
        }

        // desativa a colisão principal (por garantia)
        if (mainCollider != null)
            mainCollider.enabled = false;

        StartCoroutine(BlinkThenExplode(deathDelay));
    }

    private IEnumerator BlinkThenExplode(float deathDelay)
    {
        float elapsed = 0f;
        bool useRed = true;

        // Loop de piscar: alterna entre cor original e vermelho
        while (elapsed < preExplosionBlinkTime)
        {
            SetSpritesBlinkColor(useRed);
            useRed = !useRed;

            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        // Garante que volte para a cor original no momento da explosão
        SetSpritesBlinkColor(false);

        // Agora executa a explosão de fato (dano em área + animação + destruir)
        yield return StartCoroutine(ExplodeAndDieInternal(deathDelay));
    }

    private void SetSpritesBlinkColor(bool red)
    {
        if (spriteRenderers == null || originalColors == null) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;

            // Mantém o sprite sempre visível, só muda a cor
            spriteRenderers[i].enabled = true;
            spriteRenderers[i].color = red ? Color.red : originalColors[i];
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

        // por garantia, desativa colisão e zera velocidade
        if (mainCollider != null)
            mainCollider.enabled = false;

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

            PlayerHealth ph = col.GetComponent<PlayerHealth>() ?? col.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                Vector2 dir = (col.transform.position - transform.position).normalized;
                ph.TakeDamage(explosionDamage, dir);
            }
        }
    }

    private void DamagePlayerOnContact(PlayerHealth ph, Transform player)
    {
        if (ph == null || player == null) return;

        Vector2 dir = (player.position - transform.position).normalized;
        int dmg = (contactDamage > 0) ? contactDamage : explosionDamage;
        ph.TakeDamage(dmg, dir);
    }

    private void DisableAllCollidersAndPhysics()
    {
        // colliders do próprio objeto e dos filhos
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (var c in cols)
        {
            if (c != null) c.enabled = false;
        }

        // colliders do pai
        if (transform.parent != null)
        {
            Collider2D[] parentCols = transform.parent.GetComponents<Collider2D>();
            foreach (var c in parentCols)
            {
                if (c != null) c.enabled = false;
            }
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
            rb.simulated = false;
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
