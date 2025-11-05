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
    [Tooltip("Distância em que o inimigo começa a preparar a explosão ao chegar perto do player.")]
    public float proximityDistance = 2f;

    [Header("Piscando antes de explodir")]
    [Tooltip("Tempo que o inimigo fica piscando antes de explodir.")]
    public float preExplosionBlinkTime = 3f;
    [Tooltip("Intervalo entre cada piscada.")]
    public float blinkInterval = 0.15f;

    [Header("Dano por contato (encostar no inimigo)")]
    [Tooltip("Dano que o player leva apenas por encostar no inimigo.")]
    public int contactDamage = 1;

    [Header("Configurações de morte")]
    [Tooltip("Usado caso não encontre FlyEnemyHealth.")]
    public float deathDelayFallback = 1.0f;

    private bool hasExploded = false;
    private bool isPreparingToExplode = false;

    private Animator animator;
    private FlyEnemyHealth enemyHealth;
    private Transform playerTransform;
    private Collider2D mainCollider;
    private Rigidbody2D rb;
    private FlyingMonsterMovement flyingMovement;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    private Rigidbody2D[] allRigidbodies;
    private Collider2D[] allColliders;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<FlyEnemyHealth>();
        rb = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<Collider2D>();
        flyingMovement = GetComponent<FlyingMonsterMovement>();

        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) playerTransform = p.transform;

        // pega todos os sprites para piscar em vermelho
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

        // pega TODOS rigidbodies e colliders do inimigo (raiz + filhos)
        allRigidbodies = GetComponentsInChildren<Rigidbody2D>();
        allColliders = GetComponentsInChildren<Collider2D>();

        if (mainCollider == null)
        {
            Debug.LogWarning($"[{name}] Collider2D principal não encontrado no inimigo voador.");
        }
    }

    void Update()
    {
        if (hasExploded) return;

        // garante referência do player
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerTransform == null || isPreparingToExplode) return;

        // explosão por proximidade
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= proximityDistance)
        {
            float delay = enemyHealth != null ? enemyHealth.deathDelay : deathDelayFallback;
            StartExplosionSequence(delay);
        }
    }

    // dano e explosão quando QUALQUER collider com PlayerHealth encosta (trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded || isPreparingToExplode) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>() ??
                          other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            DamagePlayerOnContact(ph, other.transform);

            float delay = enemyHealth != null ? enemyHealth.deathDelay : deathDelayFallback;
            StartExplosionSequence(delay);
        }
    }

    // dano e explosão quando encosta com colisão normal (sem trigger)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasExploded || isPreparingToExplode) return;

        PlayerHealth ph = collision.collider.GetComponent<PlayerHealth>() ??
                          collision.collider.GetComponentInParent<PlayerHealth > ();
        if (ph != null)
        {
            DamagePlayerOnContact(ph, collision.collider.transform);

            float delay = enemyHealth != null ? enemyHealth.deathDelay : deathDelayFallback;
            StartExplosionSequence(delay);
        }
    }

    // chamado pelo FlyEnemyHealth quando ele morre por dano normal
    public void HandleDeathFromHealth(float deathDelay)
    {
        if (hasExploded || isPreparingToExplode) return;
        StartExplosionSequence(deathDelay);
    }

    /// <summary>
    /// Começa o preparo da explosão: marca como morto, desliga movimento/física/colisores
    /// e inicia o piscar vermelho.
    /// </summary>
    private void StartExplosionSequence(float deathDelay)
    {
        if (hasExploded || isPreparingToExplode) return;
        isPreparingToExplode = true;

        // Marca o inimigo como morto no sistema de vida (para o FlyingMonsterMovement parar)
        if (enemyHealth != null)
        {
            enemyHealth.currentHealth = 0;

            var field = typeof(FlyEnemyHealth).GetField(
                "isDead",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (field != null)
                field.SetValue(enemyHealth, true);
        }

        // Desliga o script de movimento para ele NÃO seguir/empurrar mais
        if (flyingMovement != null)
            flyingMovement.enabled = false;

        // Desliga TODA física e colisores (raiz + filhos) para não empurrar mais nada
        if (allRigidbodies != null)
        {
            foreach (var body in allRigidbodies)
            {
                if (body == null) continue;
                body.linearVelocity = Vector2.zero;
                body.isKinematic = true;
            }
        }

        if (allColliders != null)
        {
            foreach (var c in allColliders)
            {
                if (c == null) continue;
                c.enabled = false;
            }
        }

        StartCoroutine(BlinkThenExplode(deathDelay));
    }

    private IEnumerator BlinkThenExplode(float deathDelay)
    {
        float elapsed = 0f;
        bool useRed = true;

        // PISCAR VERMELHO (sem sumir/voltar)
        while (elapsed < preExplosionBlinkTime)
        {
            SetSpritesBlinkColor(useRed);
            useRed = !useRed;

            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        // volta pra cor original antes da explosão
        SetSpritesBlinkColor(false);

        // agora faz a explosão de fato
        yield return StartCoroutine(ExplodeAndDieInternal(deathDelay));
    }

    private void SetSpritesBlinkColor(bool red)
    {
        if (spriteRenderers == null || originalColors == null) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;

            spriteRenderers[i].enabled = true; // nunca some
            spriteRenderers[i].color = red ? Color.red : originalColors[i];
        }
    }

    /// <summary>
    /// Explode, aplica dano em área e destrói o inimigo.
    /// </summary>
    private IEnumerator ExplodeAndDieInternal(float deathDelay)
    {
        if (hasExploded) yield break;
        hasExploded = true;
        isPreparingToExplode = false;

        if (animator != null)
            animator.SetTrigger("Death");

        // física já desligada antes, só garante zero
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // dano em área
        ApplyAreaDamage();

        // espera a animação
        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }

    public void ApplyAreaDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, playerLayer);
        foreach (Collider2D col in hits)
        {
            if (col == null) continue;

            PlayerHealth ph = col.GetComponent<PlayerHealth>() ??
                              col.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                Vector2 dir = (col.transform.position - transform.position).normalized;
                ph.TakeDamage(explosionDamage, dir);
            }
        }
    }

    private void DamagePlayerOnContact(PlayerHealth ph, Transform playerTransform)
    {
        if (ph == null || playerTransform == null) return;

        Vector2 dir = (playerTransform.position - transform.position).normalized;
        int dmg = contactDamage > 0 ? contactDamage : explosionDamage;
        ph.TakeDamage(dmg, dir);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, proximityDistance);
    }
}
