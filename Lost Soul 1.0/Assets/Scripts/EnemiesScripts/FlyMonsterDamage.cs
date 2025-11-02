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
    public float proximityDistance = 2f; // se o player chegar tão perto, explode

    [Header("Configurações de morte")]
    public float deathDelayFallback = 1.0f; // usado caso não ache FlyEnemyHealth
    private bool hasExploded = false;

    private Animator animator;
    private FlyEnemyHealth enemyHealth;
    private Collider2D mainCollider;
    private Rigidbody2D rb;
    private Transform playerTransform;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<FlyEnemyHealth>();
        mainCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) playerTransform = p.transform;

        // se o collider for trigger para detectar "encostar", ok; se for físico, OnCollision também chamará ExplodeTouch
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

        // Se estiver vivo (ou mesmo morto por health), não queremos que a lógica de proximidade seja ignorada.
        // Só explodimos por proximidade enquanto não explodimos ainda.
        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= proximityDistance)
            {
                StartCoroutine(ExplodeAndDie(enemyHealth != null ? enemyHealth.deathDelay : deathDelayFallback));
            }
        }
    }

    // Quando o player encostar (trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;
        if (other.CompareTag("Player"))
        {
            StartCoroutine(ExplodeAndDie(enemyHealth != null ? enemyHealth.deathDelay : deathDelayFallback));
        }
    }

    // Caso esteja usando colisão física (não trigger)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasExploded) return;
        if (collision.collider.CompareTag("Player"))
        {
            StartCoroutine(ExplodeAndDie(enemyHealth != null ? enemyHealth.deathDelay : deathDelayFallback));
        }
    }

    // Chamado pelo FlyEnemyHealth quando é morto (para que a morte por dano também cause a explosão)
    public void HandleDeathFromHealth(float deathDelay)
    {
        if (hasExploded) return;
        StartCoroutine(ExplodeAndDie(deathDelay));
    }

    private IEnumerator ExplodeAndDie(float deathDelay)
    {
        if (hasExploded) yield break;
        hasExploded = true;

        // Marca como morto se tiver FlyEnemyHealth
        if (enemyHealth != null)
        {
            enemyHealth.currentHealth = 0;
            // usamos reflection-friendly campo privado
            var field = typeof(FlyEnemyHealth).GetField("isDead", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(enemyHealth, true);
        }

        // dispara animação de morte
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
