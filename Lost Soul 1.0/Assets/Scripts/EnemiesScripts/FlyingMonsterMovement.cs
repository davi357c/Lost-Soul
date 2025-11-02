using UnityEngine;

public class FlyingMonsterMovement : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float moveSpeed;
    public float chaseDistance;

    private int currentPatrolIndex = 0;
    public Transform playerTransform;
    public bool isChasing;

    private Animator animator;
    private FlyEnemyHealth enemyHealth;
    private Rigidbody2D rb;
    private Collider2D col;

    private bool isMovementDisabled = false; // evita que tente "reviver" o movimento

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<FlyEnemyHealth>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void Update()
    {
        // Se o inimigo morreu e o movimento ainda não foi desativado, faz isso uma vez
        if (enemyHealth != null && enemyHealth.IsDead && !isMovementDisabled)
        {
            DisableMovement();
            return;
        }

        // Se o movimento já foi desativado, sai
        if (isMovementDisabled) return;

        // Atualiza referência do player caso tenha sido perdida
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        Vector2 moveDir = Vector2.zero;

        if (isChasing)
        {
            if (playerTransform == null) return;

            if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead)
            {
                isChasing = false;
                return;
            }

            Vector3 targetPos = playerTransform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            moveDir = (targetPos - transform.position).normalized;

            if (moveDir.x > 0.01f)
                transform.localScale = new Vector3(-1, 1, 1);
            else if (moveDir.x < -0.01f)
                transform.localScale = new Vector3(1, 1, 1);

            if (Vector2.Distance(transform.position, playerTransform.position) > chaseDistance + 2f)
                isChasing = false;
        }
        else
        {
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < chaseDistance)
            {
                isChasing = true;
            }

            if (patrolPoints.Length == 0) return;

            Transform targetPoint = patrolPoints[currentPatrolIndex];
            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime);
            moveDir = (targetPoint.position - transform.position).normalized;

            if (moveDir.x > 0.01f)
                transform.localScale = new Vector3(-1, 1, 1);
            else if (moveDir.x < -0.01f)
                transform.localScale = new Vector3(1, 1, 1);

            if (Vector2.Distance(transform.position, targetPoint.position) < 0.2f)
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        if (animator != null)
            animator.SetFloat("Speed", moveDir.magnitude);
    }

    private void DisableMovement()
    {
        isMovementDisabled = true;

        if (animator != null)
            animator.SetFloat("Speed", 0f);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true; // desliga a física
        }

        if (col != null)
            col.enabled = false; // desativa colisões

        // opcional: também pode desabilitar este script
        // enabled = false;
    }
}
