using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Knockback")]
    public float knockbackForce = 5f;

    private Animator animator;
    private Rigidbody2D rb;
    private EnemyMovement movement;

    [HideInInspector] public bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<EnemyMovement>();
    }

    public void TakeDamage(Vector2 hitDir, int damage = 1)
    {
        if (isDead) return;

        currentHealth -= damage;

        // animação de hit
        animator.SetTrigger("Hit");

        // knockback
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(hitDir.x * knockbackForce, knockbackForce), ForceMode2D.Impulse);

        // se morreu
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        animator.SetBool("isDead", true);
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (movement != null)
            movement.enabled = false;

        // destrói após animação de morte
        Destroy(gameObject, 1.5f);
    }
}
