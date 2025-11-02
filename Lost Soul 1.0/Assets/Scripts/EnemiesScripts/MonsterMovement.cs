using UnityEngine;

public class MonsterMovement : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float moveSpeed;
    public int patrolDestination;

public Transform playerTransform;
    public bool isChasing;
    public float chaseDistance;

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
                animator.SetFloat("Speed", 0f); // para animação de movimento
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

            if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead)
            {
                isChasing = false;
                return;
            }

            if (Vector2.Distance(transform.position, playerTransform.position) > chaseDistance + 2f)
            {
                isChasing = false;
                return;
            }

            if (transform.position.x > playerTransform.position.x)
            {
                transform.localScale = new Vector3(1, 1, 1);
                transform.position += Vector3.left * moveSpeed * Time.deltaTime;
                moveDir = Vector2.left;
            }
            else if (transform.position.x < playerTransform.position.x)
            {
                transform.localScale = new Vector3(-1, 1, 1);
                transform.position += Vector3.right * moveSpeed * Time.deltaTime;
                moveDir = Vector2.right;
            }
        }
        else
        {
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < chaseDistance)
            {
                isChasing = true;
            }

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

        if (animator != null)
            animator.SetFloat("Speed", Mathf.Abs(moveDir.x));
    }

}
