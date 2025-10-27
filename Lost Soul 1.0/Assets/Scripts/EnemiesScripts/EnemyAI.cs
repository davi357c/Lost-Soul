using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    [Header("Movimento e Perseguição")]
    public float moveSpeed = 2f;
    public float chaseRange = 6f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1f;

    [Header("Ataque")]
    public Transform attackPoint;
    public float attackRadius = 0.6f;
    public int damageToPlayer = 1;

    [Header("Detecção")]
    public string playerTag = "Player";
    public LayerMask obstacleMask; // paredes, chão, etc.

    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private EnemyHealth health;

    private bool isFacingRight = true;
    private bool isAttacking = false;
    private bool isDead = false;
    private float lastAttackTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("[EnemyAI] Player não encontrado com a tag " + playerTag);

        // Se AttackPoint não tiver sido definido, cria automaticamente
        if (attackPoint == null)
        {
            GameObject ap = new GameObject("AttackPoint");
            ap.transform.parent = transform;
            ap.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            attackPoint = ap.transform;
        }
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        bool playerInFront = (isFacingRight && player.position.x >= transform.position.x) ||
                             (!isFacingRight && player.position.x <= transform.position.x);

        bool canSeePlayer = HasLineOfSight();

        Debug.Log($"[EnemyAI] Distance: {distance}, InFront: {playerInFront}, CanSee: {canSeePlayer}");

        if (distance <= chaseRange && playerInFront && canSeePlayer)
            ChasePlayer(distance);
        else
        {
            animator.SetBool("isRunning", false);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void ChasePlayer(float distance)
    {
        if (distance <= attackRange)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetBool("isRunning", false);

            if (Time.time > lastAttackTime + attackCooldown)
                StartCoroutine(AttackRoutine());
        }
        else if (!isAttacking)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
            animator.SetBool("isRunning", true);

            if (dir.x > 0 && !isFacingRight) Flip();
            else if (dir.x < 0 && isFacingRight) Flip();
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        animator.SetBool("isAttacking", true);
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(0.3f);
        Debug.Log("[EnemyAI] Tentando atacar o player");
        AttackHit();

        yield return new WaitForSeconds(0.4f);
        animator.SetBool("isAttacking", false);
        lastAttackTime = Time.time;
        isAttacking = false;
    }

    void AttackHit()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(playerTag))
            {
                Debug.Log("[EnemyAI] Player atingido pelo AttackPoint!");
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    Vector2 dir = (hit.transform.position - transform.position).normalized;
                    ph.TakeDamage(dir);
                    Debug.Log("[EnemyAI] Dano aplicado ao player!");
                }
            }
        }
    }

    bool HasLineOfSight()
    {
        if (player == null) return false;
        Vector2 dir = (player.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, player.position);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, distance, obstacleMask);
        return hit.collider == null;
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;

        // Move o AttackPoint para frente automaticamente
        if (attackPoint != null)
        {
            Vector3 pos = attackPoint.localPosition;
            pos.x = isFacingRight ? Mathf.Abs(pos.x) : -Mathf.Abs(pos.x);
            attackPoint.localPosition = pos;
        }
    }

    public void Die()
    {
        isDead = true;
        animator.SetBool("isDead", true);
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
