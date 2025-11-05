using UnityEngine;
using System.Collections;

public class FlyEnemyHealth : MonoBehaviour
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
    [Tooltip("Tempo da animação de morte/explosão antes de destruir o inimigo.")]
    public float deathDelay = 1.0f;

    private bool isDead = false;
    public bool IsDead => isDead;

    private Rigidbody2D rb;
    private Animator animator;
    private Coroutine knockbackCoroutine;

    void Start()
    {
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        if (rb == null)
            Debug.LogWarning("Rigidbody2D não encontrado em " + name);
        if (animator == null)
            Debug.LogWarning("Animator não encontrado em " + name);
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(Vector2.zero, amount);
    }

    public void TakeDamage(Vector2 hitDirection, int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        // animação de hit
        if (animator != null && !isDead)
        {
            animator.SetTrigger("Hit");
        }

        // aplica knockback se houver direção válida e Rigidbody disponível
        if (rb != null && hitDirection != Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(hitDirection.normalized * knockbackForce, ForceMode2D.Impulse);

            if (knockbackCoroutine != null)
                StopCoroutine(knockbackCoroutine);

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

    /// <summary>
    /// Força o estado de morto (usado, por exemplo, quando a explosão é iniciada
    /// pelo FlyMonsterDamage).
    /// </summary>
    public void ForceDeadState()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0; // garante que a vida zera em qualquer caminho de morte
    }

    private IEnumerator Die()
    {
        if (isDead) yield break;

        // Marca como morto logo no início (garante integração com o spawner)
        isDead = true;
        currentHealth = 0;

        // Se existir FlyMonsterDamage, delega a sequência de morte/explosão pra ele
        FlyMonsterDamage damageComp = GetComponent<FlyMonsterDamage>();
        if (damageComp != null)
        {
            damageComp.HandleDeathFromHealth(deathDelay);
            yield break; // FlyMonsterDamage vai destruir o GameObject
        }

        // fallback – morte simples se não houver FlyMonsterDamage
        if (animator != null)
            animator.SetTrigger("Death");

        // DESATIVA TODOS colliders do inimigo (objeto + filhos + pai)
        DisableAllCollidersAndPhysics();

        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }

    private void DisableAllCollidersAndPhysics()
    {
        // colliders do próprio objeto e dos filhos
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (var c in cols)
        {
            if (c != null) c.enabled = false;
        }

        // colliders do objeto pai (onde geralmente está o BoxCollider principal)
        if (transform.parent != null)
        {
            Collider2D[] parentCols = transform.parent.GetComponents<Collider2D>();
            foreach (var c in parentCols)
            {
                if (c != null) c.enabled = false;
            }
        }

        // trava física também
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
            rb.simulated = false;
        }
    }
}
