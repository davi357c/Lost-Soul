using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Referências")]
    public string playerTag = "Player";
    private Transform player;
    private Animator anim;
    private PlayerHealth playerHealth;

    [Header("Movimentação")]
    public float moveSpeed = 3f;
    public float chaseRange = 5f;
    public float attackRange = 0.8f;
    public float stopDistance = 0.3f;

    [Header("Ataque")]
    public float timeBetweenAttacks = 2f;
    public GameObject attackHitbox1;
    public GameObject attackHitbox2;

    private bool isAttacking = false;
    private bool facingRight = true;

    void Start()
    {
        anim = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();

            // ⚡ Desativa colisão física entre inimigo e player (se quiser que o player não leve dano por contato)
            Collider2D myCol = GetComponent<Collider2D>();
            Collider2D playerCol = playerObj.GetComponent<Collider2D>();
            if (myCol != null && playerCol != null)
            {
                Physics2D.IgnoreCollision(myCol, playerCol, true);
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ Nenhum objeto com tag '{playerTag}' encontrado!");
        }

        if (attackHitbox1 != null) attackHitbox1.SetActive(false);
        if (attackHitbox2 != null) attackHitbox2.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        // 🧠 Se o player morreu, reseta o inimigo
        if (playerHealth != null && playerHealth.IsDead)
        {
            StopAllCoroutines();
            isAttacking = false;
            anim.SetBool("isChasing", false);
            anim.ResetTrigger("Attack1");
            anim.ResetTrigger("Attack2");

            if (attackHitbox1 != null) attackHitbox1.SetActive(false);
            if (attackHitbox2 != null) attackHitbox2.SetActive(false);
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        FacePlayer();

        // ✅ Sempre perseguir o player se estiver dentro do chaseRange
        if (distance <= chaseRange && distance > attackRange)
        {
            anim.SetBool("isChasing", true);
            ChasePlayer();
        }
        else
        {
            anim.SetBool("isChasing", false);
        }

        // Inicia ataque somente se estiver dentro do attackRange e não estiver atacando
        if (distance <= attackRange && !isAttacking)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    void ChasePlayer()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > stopDistance)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
        }
    }

    void FacePlayer()
    {
        if (player == null) return;

        if (player.position.x > transform.position.x && !facingRight)
            Flip();
        else if (player.position.x < transform.position.x && facingRight)
            Flip();
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    IEnumerator AttackRoutine()
    {
        if (isAttacking) yield break;
        isAttacking = true;
        anim.SetBool("isChasing", false);

        int attackType = Random.Range(0, 2);
        string triggerName = attackType == 0 ? "Attack1" : "Attack2";
        anim.SetTrigger(triggerName);

        yield return new WaitForSeconds(timeBetweenAttacks);
        isAttacking = false;
    }

    // ===== Eventos de animação =====
    public void ActivateAttack1Hitbox() => attackHitbox1?.SetActive(true);
    public void DeactivateAttack1Hitbox() => attackHitbox1?.SetActive(false);
    public void ActivateAttack2Hitbox() => attackHitbox2?.SetActive(true);
    public void DeactivateAttack2Hitbox() => attackHitbox2?.SetActive(false);

    // ===== Gizmos =====
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
