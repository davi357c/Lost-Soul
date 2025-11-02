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
    public bool isKnockedBack = false; // flag p�blica para outros scripts verificarem

    [Header("Morte")]
    public float deathDelay = 1.03f; // dura��o da anima��o de morte
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
            Debug.LogWarning("Rigidbody2D n�o encontrado!");
        if (animator == null)
            Debug.LogWarning("Animator n�o encontrado!");
        else
            Debug.Log("Animator encontrado: " + animator.name);
    }


    public void TakeDamage(int amount)
    {
        TakeDamage(Vector2.zero, amount);
    }

    public void TakeDamage(Vector2 hitDirection, int amount)
    {
        Debug.Log($"{name} recebeu {amount} de dano! Vida atual: {currentHealth}");

        if (isDead) return;

        currentHealth -= amount;

        // aciona anima��o de hit
        if (animator != null && !isDead)
        {
            animator.SetTrigger("Hit");
        }

        // aplica knockback se houver dire��o v�lida e Rigidbody dispon�vel
        if (rb != null && hitDirection != Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(hitDirection.normalized * knockbackForce, ForceMode2D.Impulse);

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

        // Se existir FlyMonsterDamage, delega a sequ�ncia de morte para ele (explode + anima��o + destroy)
        FlyMonsterDamage damageComp = GetComponent<FlyMonsterDamage>();
        if (damageComp != null)
        {
            damageComp.HandleDeathFromHealth(deathDelay);
            yield break; // o FlyMonsterDamage ir� destruir o GameObject ap�s o delay
        }

        // fallback � comporta-se como antes se n�o houver FlyMonsterDamage
        if (animator != null)
            animator.SetTrigger("Death");

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }
}
