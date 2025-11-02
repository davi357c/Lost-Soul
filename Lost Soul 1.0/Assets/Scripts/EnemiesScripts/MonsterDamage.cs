using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class MonsterDamage : MonoBehaviour
{
    [Header("Dano / Ataque")]
    public int damage = 1;
    public Transform attackPoint;
    public float attackRange = 1f;
    public LayerMask playerLayer;

    [Header("Cooldown")]
    public float attackCooldown = 1f;

    private Animator animator;
    private bool canAttack = true;
    private bool isAttacking = false; 
    private EnemyHealth enemyHealth;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        enemyHealth = GetComponentInParent<EnemyHealth>();

        if (attackPoint == null)
            attackPoint = transform;
    }


    void Update()
    {
        if (enemyHealth != null && enemyHealth.IsDead) return;

        if (canAttack && !isAttacking)
        {
            Collider2D playerCollider = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayer);
            if (playerCollider != null)
            {
                StartCoroutine(AttackRoutine());
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        canAttack = false;
        isAttacking = true;

        if (animator != null)
            animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.6f);

        isAttacking = false;

        yield return new WaitForSeconds(attackCooldown - 0.6f);

        canAttack = true;
    }


    // chamada pelo Animation Event
    public void OnAttackHit()
    {
        if (enemyHealth != null && enemyHealth.IsDead) return;

        Collider2D playerCollider = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayer);
        if (playerCollider != null)
        {
            PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector2 hitDirection = (playerCollider.transform.position - transform.position).normalized;
                playerHealth.TakeDamage(damage, hitDirection);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) attackPoint = transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
