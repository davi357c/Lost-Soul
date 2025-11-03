using UnityEngine;

public class MonsterMovement : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float moveSpeed;
    public int patrolDestination;

    [Header("Perseguição")]
    public Transform playerTransform;
    public bool isChasing;
    public float chaseDistance;

    [Header("Parada perto do Player")]
    [Tooltip("Distância mínima em X até o player para PARAR de andar e não empurrar o player.")]
    public float stopDistanceFromPlayer = 0.7f;

    private Animator animator;
    private EnemyHealth enemyHealth;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void Update()
    {
        // se o inimigo morreu, não faz nada
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            if (animator != null)
                animator.SetFloat("Speed", 0f);
            return;
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        Vector2 moveDir = Vector2.zero;

        if (isChasing)
        {
            if (playerTransform == null) return;

            // se o player morreu, para de perseguir
            if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead)
            {
                isChasing = false;
                return;
            }

            // se ficou longe demais, volta pra patrulha
            if (Vector2.Distance(transform.position, playerTransform.position) > chaseDistance + 2f)
            {
                isChasing = false;
                return;
            }

            // ==== PARAR PERTO DO PLAYER ====
            float stopDist = (stopDistanceFromPlayer <= 0f) ? 0.7f : stopDistanceFromPlayer;
            float dx = playerTransform.position.x - transform.position.x;
            float horizontalDistance = Mathf.Abs(dx);

            // vira pro lado do player mesmo parado
            if (dx < 0f)
                transform.localScale = new Vector3(1, 1, 1);
            else if (dx > 0f)
                transform.localScale = new Vector3(-1, 1, 1);

            if (horizontalDistance > stopDist)
            {
                // mesma lógica de antes, só anda se estiver longe o suficiente
                if (dx < 0f)
                {
                    transform.position += Vector3.left * moveSpeed * Time.deltaTime;
                    moveDir = Vector2.left;
                }
                else if (dx > 0f)
                {
                    transform.position += Vector3.right * moveSpeed * Time.deltaTime;
                    moveDir = Vector2.right;
                }
            }
            else
            {
                // já em alcance de ataque → não anda, não empurra o player
                moveDir = Vector2.zero;
            }
        }
        else
        {
            // começa a perseguir se o player aproximar
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < chaseDistance)
            {
                isChasing = true;
            }

            // patrulha entre os pontos
            if (patrolPoints != null && patrolPoints.Length >= 2)
            {
                if (patrolDestination == 0)
                {
                    transform.position = Vector2.MoveTowards(transform.position, patrolPoints[0].position, moveSpeed * Time.deltaTime);
                    moveDir = (patrolPoints[0].position - transform.position).normalized;

                    if (Vector2.Distance(transform.position, patrolPoints[0].position) < .2f)
                    {
                        transform.localScale = new Vector3(1, 1, 1);
                        patrolDestination = 1;
                    }
                }
                else if (patrolDestination == 1)
                {
                    transform.position = Vector2.MoveTowards(transform.position, patrolPoints[1].position, moveSpeed * Time.deltaTime);
                    moveDir = (patrolPoints[1].position - transform.position).normalized;

                    if (Vector2.Distance(transform.position, patrolPoints[1].position) < .2f)
                    {
                        transform.localScale = new Vector3(-1, 1, 1);
                        patrolDestination = 0;
                    }
                }
            }
        }

        if (animator != null)
            animator.SetFloat("Speed", Mathf.Abs(moveDir.x));
    }
}
