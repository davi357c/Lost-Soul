using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GroundEnemyWave : MonoBehaviour
{
    [Header("Movimento / Perseguição")]
    public Transform playerTransform;
    public float moveSpeed = 2f;
    public float chaseDistance = 5f;
    [Tooltip("Distância mínima até o player para parar e não empurrar.")]
    public float stopDistanceFromPlayer = 0.7f;

    private Animator animator;
    private EnemyHealth enemyHealth;
    private Rigidbody2D rb;

    private bool isChasing = false;
    private float targetVelocityX = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    void Start()
    {
        // procura o player automaticamente se não tiver setado no inspector
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void Update()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            targetVelocityX = 0f;
            if (animator != null)
                animator.SetFloat("Speed", 0f);
            return;
        }

        // se estiver em knockback, não controla o movimento
        if (enemyHealth != null && enemyHealth.isKnockedBack)
        {
            if (animator != null)
                animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            return;
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerTransform == null) return;

        // calcula distância até o player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // se estiver dentro da distância de perseguição, ativa chase
        if (distanceToPlayer <= chaseDistance)
            isChasing = true;

        if (isChasing)
        {
            FollowPlayer();
        }
        else
        {
            // se estiver longe demais, para de andar
            targetVelocityX = 0f;
            if (animator != null)
                animator.SetFloat("Speed", 0f);
        }
    }

    void FollowPlayer()
    {
        float dx = playerTransform.position.x - transform.position.x;
        float horizontalDistance = Mathf.Abs(dx);

        // vira pro lado do player
        if (dx < 0f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (dx > 0f)
            transform.localScale = new Vector3(-1, 1, 1);

        // se está longe o suficiente, anda até ele
        if (horizontalDistance > stopDistanceFromPlayer)
        {
            float direction = Mathf.Sign(dx);
            targetVelocityX = direction * moveSpeed;
        }
        else
        {
            // se chegou perto demais, para
            targetVelocityX = 0f;
        }

        if (animator != null)
            animator.SetFloat("Speed", Mathf.Abs(targetVelocityX));
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (enemyHealth != null && enemyHealth.IsDead)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (enemyHealth != null && enemyHealth.isKnockedBack)
            return;

        // aplica movimento
        rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
    }
}
