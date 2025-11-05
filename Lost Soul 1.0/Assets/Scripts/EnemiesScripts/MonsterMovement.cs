using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MonsterMovement : MonoBehaviour
{
    [Header("Patrulha")]
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;
    public int patrolDestination;

    [Header("Perseguição")]
    public Transform playerTransform;
    public bool isChasing;
    public float chaseDistance = 5f;

    [Header("Parada perto do Player")]
    [Tooltip("Distância mínima em X até o player para PARAR de andar e não empurrar o player.")]
    public float stopDistanceFromPlayer = 0.7f;

    private Animator animator;
    private EnemyHealth enemyHealth;
    private Rigidbody2D rb;

    // velocidade alvo no eixo X (o que vamos aplicar no Rigidbody)
    private float targetVelocityX = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            Debug.LogWarning("[MonsterMovement] Rigidbody2D não encontrado, adicione um no mesmo objeto do script.");

        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void Update()
    {
        // Se o inimigo morreu, não anda mais
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            targetVelocityX = 0f;
            if (animator != null)
                animator.SetFloat("Speed", 0f);
            return;
        }

        // Se está em knockback, não mexe no movimento (deixa o Rigidbody "voar" sozinho)
        if (enemyHealth != null && enemyHealth.isKnockedBack)
        {
            if (animator != null && rb != null)
                animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            return;
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        Vector2 moveDir = Vector2.zero;
        targetVelocityX = 0f; // por padrão não anda

        if (isChasing)
        {
            if (playerTransform == null) return;

            // Se o player morreu, para de perseguir
            if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead)
            {
                isChasing = false;
            }
            else
            {
                // Se ficou longe demais, volta pra patrulha
                if (Vector2.Distance(transform.position, playerTransform.position) > chaseDistance + 2f)
                {
                    isChasing = false;
                }
                else
                {
                    float stopDist = (stopDistanceFromPlayer <= 0f) ? 0.7f : stopDistanceFromPlayer;
                    float dx = playerTransform.position.x - transform.position.x;
                    float horizontalDistance = Mathf.Abs(dx);

                    // Vira pro lado do player
                    if (dx < 0f)
                        transform.localScale = new Vector3(1, 1, 1);
                    else if (dx > 0f)
                        transform.localScale = new Vector3(-1, 1, 1);

                    if (horizontalDistance > stopDist)
                    {
                        // Só anda se ainda estiver LONGE o suficiente
                        moveDir = (dx < 0f) ? Vector2.left : Vector2.right;
                        targetVelocityX = moveDir.x * moveSpeed;
                    }
                    else
                    {
                        // Já está em alcance de ataque → PARA COMPLETAMENTE
                        targetVelocityX = 0f;
                        moveDir = Vector2.zero;
                    }
                }
            }
        }
        else
        {
            // Começa a perseguir se o player aproximar
            if (playerTransform != null &&
                Vector2.Distance(transform.position, playerTransform.position) < chaseDistance)
            {
                isChasing = true;
            }

            // Patrulha entre os pontos
            if (patrolPoints != null && patrolPoints.Length >= 2)
            {
                Transform targetPoint = patrolPoints[patrolDestination];
                float dxPatrol = targetPoint.position.x - transform.position.x;

                if (Mathf.Abs(dxPatrol) > 0.05f)
                {
                    float dir = Mathf.Sign(dxPatrol);
                    moveDir = new Vector2(dir, 0f);
                    targetVelocityX = dir * moveSpeed;
                }
                else
                {
                    // Considera que chegou no ponto
                    transform.position = new Vector3(targetPoint.position.x, transform.position.y, transform.position.z);
                    targetVelocityX = 0f;

                    if (patrolDestination == 0)
                    {
                        transform.localScale = new Vector3(1, 1, 1);
                        patrolDestination = 1;
                    }
                    else
                    {
                        transform.localScale = new Vector3(-1, 1, 1);
                        patrolDestination = 0;
                    }
                }
            }
        }

        // Atualiza animação de andar/parado
        if (animator != null)
            animator.SetFloat("Speed", Mathf.Abs(moveDir.x));
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // Se morreu, zera movimento
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Se estiver em knockback, não sobrescreve a velocidade
        if (enemyHealth != null && enemyHealth.isKnockedBack)
            return;

        // Aplica a velocidade calculada em Update
        rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
    }
}
