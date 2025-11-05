using UnityEngine;

public class FlyingMonsterMovement : MonoBehaviour
{
    [Header("Patrulha")]
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;

    [Header("Perseguição")]
    public float chaseDistance = 5f;
    public Transform playerTransform;
    public bool isChasing;

    private int currentPatrolIndex = 0;

    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D col;
    private FlyEnemyHealth enemyHealth;

    private bool isMovementDisabled = false; // evita que tente "reviver" o movimento

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        enemyHealth = GetComponent<FlyEnemyHealth>();

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void Update()
    {
        if (isMovementDisabled) return;

        // Se o inimigo morreu, desativa movimento e colisões
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            DisableMovement();
            return;
        }

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

            Vector3 targetPos = playerTransform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            moveDir = (targetPos - transform.position).normalized;

            if (moveDir.x > 0.01f)
                transform.localScale = new Vector3(-1, 1, 1);
            else if (moveDir.x < -0.01f)
                transform.localScale = new Vector3(1, 1, 1);

            // Se afastar muito além da distância de perseguição, volta pra patrulha
            if (Vector2.Distance(transform.position, playerTransform.position) > chaseDistance + 2f)
                isChasing = false;
        }
        else
        {
            // Se o player estiver perto o suficiente, começa a perseguir
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < chaseDistance)
            {
                isChasing = true;
            }

            // Patrulha simples entre pontos
            if (patrolPoints == null || patrolPoints.Length == 0) return;

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

    /// <summary>
    /// Desativa completamente o movimento e as colisões do inimigo.
    /// Pode ser chamado de outros scripts (ex.: FlyMonsterDamage ou FlyEnemyHealth).
    /// </summary>
    public void DisableMovement()
    {
        if (isMovementDisabled) return;
        isMovementDisabled = true;

        // para animação de movimento
        if (animator != null)
            animator.SetFloat("Speed", 0f);

        // trava física COMPLETAMENTE
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
            rb.simulated = false; // não participa mais de nenhuma simulação 2D
        }

        // desativa TODOS colliders do inimigo (objeto + filhos)
        Collider2D[] allCols = GetComponentsInChildren<Collider2D>();
        foreach (var c in allCols)
        {
            if (c != null) c.enabled = false;
        }

        // 🔴 IMPORTANTE: desativa também os colliders do OBJETO PAI
        if (transform.parent != null)
        {
            Collider2D[] parentCols = transform.parent.GetComponents<Collider2D>();
            foreach (var c in parentCols)
            {
                if (c != null) c.enabled = false;
            }
        }

        // por garantia, também desativa o collider principal, se havia referência
        if (col != null)
            col.enabled = false;

        // desabilita este script para garantir que não mova mais o inimigo
        enabled = false;
    }
}
