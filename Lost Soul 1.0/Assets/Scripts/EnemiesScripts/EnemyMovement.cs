using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 2f;
    public float detectionRange = 5f;
    public float stopDistance = 0.8f;

    [Header("Ataque")]
    public int damage = 1;
    public float attackRange = 0.7f;
    public float attackRate = 1.5f;

    private float nextAttackTime = 0f;
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private EnemyHealth health;

    private bool facingRight = true;
    private bool isAttacking = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Ajusta o valor inicial do flip com base na escala
        facingRight = transform.localScale.x > 0;
    }

    void Update()
    {
        if (health == null || health.isDead) return;
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("isRunning", false);

            if (Time.time >= nextAttackTime && !isAttacking)
            {
                nextAttackTime = Time.time + attackRate;
                StartCoroutine(Attack());
            }
        }
        else if (distanceToPlayer <= detectionRange)
        {
            ChasePlayer();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("isRunning", false);
        }

        FlipTowardsPlayer();
    }

    void ChasePlayer()
    {
        if (isAttacking) return;

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);
        animator.SetBool("isRunning", true);
    }

    System.Collections.IEnumerator Attack()
    {
        isAttacking = true;
        animator.SetBool("isAttacking", true);
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(0.2f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player") && Vector2.Distance(transform.position, hit.transform.position) <= attackRange)
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Vector2 hitDir = (playerHealth.transform.position - transform.position).normalized;
                    playerHealth.TakeDamage(hitDir);
                }
            }
        }

        yield return new WaitForSeconds(0.4f);
        animator.SetBool("isAttacking", false);
        isAttacking = false;
    }

    void FlipTowardsPlayer()
    {
        if (player == null) return;

        bool shouldFaceRight = player.position.x > transform.position.x;
        if (shouldFaceRight != facingRight)
        {
            facingRight = shouldFaceRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
