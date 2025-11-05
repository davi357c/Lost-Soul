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

    [Header("Cooldown ataque (animação)")]
    public float attackCooldown = 1f;

    [Header("Dano por contato (corpo)")]
    [Tooltip("Se marcado, o player toma dano só de encostar no inimigo.")]
    public bool enableTouchDamage = true;
    [Tooltip("Raio ao redor do corpo do inimigo para dano de contato.")]
    public float touchDamageRange = 0.4f;
    [Tooltip("Tempo mínimo entre um dano de contato e outro.")]
    public float touchDamageCooldown = 0.8f;
    [Tooltip("Força do knockback quando o player encosta no inimigo.")]
    public float touchKnockbackForce = 8f;

    private Animator animator;
    private bool canAttack = true;
    private bool isAttacking = false;
    private EnemyHealth enemyHealth;
    private float nextTouchDamageTime = 0f;

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

        // --- ATAQUE NORMAL (animação + event) ---
        if (canAttack && !isAttacking)
        {
            Collider2D playerCollider = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayer);
            if (playerCollider != null)
            {
                StartCoroutine(AttackRoutine());
            }
        }

        // --- DANO POR CONTATO DE CORPO ---
        if (enableTouchDamage && Time.time >= nextTouchDamageTime)
        {
            Collider2D playerColliderTouch = Physics2D.OverlapCircle(transform.position, touchDamageRange, playerLayer);
            if (playerColliderTouch != null)
            {
                PlayerHealth playerHealth = playerColliderTouch.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    // Direção do knockback = do inimigo -> player
                    // (sempre pro lado oposto do inimigo)
                    Vector2 hitDirection = (playerColliderTouch.transform.position - transform.position).normalized;

                    // Knockback direto no Rigidbody2D do player
                    Rigidbody2D playerRb = playerColliderTouch.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        // opcional: zera velocidade X pra deixar o knockback mais consistente
                        playerRb.linearVelocity = new Vector2(0f, playerRb.linearVelocity.y);
                        playerRb.AddForce(hitDirection * touchKnockbackForce, ForceMode2D.Impulse);
                    }

                    // Aplica dano usando o sistema de vida do player
                    playerHealth.TakeDamage(damage, hitDirection);
                }

                // cooldown entre danos de contato
                nextTouchDamageTime = Time.time + touchDamageCooldown;
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        canAttack = false;
        isAttacking = true;

        if (animator != null)
            animator.SetTrigger("Attack");

        // tempo até o frame de hit da animação
        yield return new WaitForSeconds(0.6f);

        isAttacking = false;

        float restante = attackCooldown - 0.6f;
        if (restante > 0f)
            yield return new WaitForSeconds(restante);

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

        // alcance do ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        // alcance do dano de contato
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, touchDamageRange);
    }
}
