using UnityEngine;
using System.Collections;

public class ArcherMovement : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float moveSpeed;
    public int patrolDestination;

[Header("Player / Perseguição")]
    public Transform playerTransform;
    public bool isChasing;
    public float chaseDistance;

    [Header("Tiro")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float fireRate = 1f;
    public float arrowSpeed = 7f;
    public float arrowLifeTime = 4f;
    public int arrowDamage = 1;
    public bool useAnimationEventForSpawn = true;

    private Animator animator;
    private EnemyHealth enemyHealth;

    private float fireCooldown = 0f;
    private bool isAttacking = false;

    // guarda a escala original
    private Vector3 originalScale;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
        originalScale = transform.localScale; // guarda a escala original

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

        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < chaseDistance)
        {
            isChasing = false;
            isAttacking = true;
        }
        else
        {
            isAttacking = false;
        }

        if (isAttacking)
        {
            if (playerTransform == null) return;

            // FLIP CORRIGIDO
            if (playerTransform.position.x < transform.position.x)
                transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z); // esquerda
            else
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);  // direita

            if (animator != null)
                animator.SetFloat("Speed", 0f);

            HandleShooting();
        }
        else if (isChasing)
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
                transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z); // esquerda
                transform.position += Vector3.left * moveSpeed * Time.deltaTime;
                moveDir = Vector2.left;
            }
            else if (transform.position.x < playerTransform.position.x)
            {
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z); // direita
                transform.position += Vector3.right * moveSpeed * Time.deltaTime;
                moveDir = Vector2.right;
            }

            if (animator != null)
                animator.SetFloat("Speed", Mathf.Abs(moveDir.x));
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
                    transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z); // esquerda
                    patrolDestination = 1;
                }
            }
            else if (patrolDestination == 1)
            {
                transform.position = Vector2.MoveTowards(transform.position, patrolPoints[1].position, moveSpeed * Time.deltaTime);
                moveDir = (patrolPoints[1].position - transform.position).normalized;

                if (Vector2.Distance(transform.position, patrolPoints[1].position) < .2f)
                {
                    transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z); // direita
                    patrolDestination = 0;
                }
            }

            if (animator != null)
                animator.SetFloat("Speed", Mathf.Abs(moveDir.x));
        }
    }

    void HandleShooting()
    {
        if (arrowPrefab == null || firePoint == null || playerTransform == null) return;

        if (fireCooldown > 0f)
        {
            fireCooldown -= Time.deltaTime;
            return;
        }

        if (animator != null)
            animator.SetTrigger("Shoot");

        if (!useAnimationEventForSpawn)
            SpawnArrow();

        fireCooldown = 1f / fireRate;
    }

    public void SpawnArrow()
    {
        if (arrowPrefab == null || firePoint == null || playerTransform == null) return;

        Vector2 dir = (playerTransform.position - firePoint.position).normalized;
        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Rigidbody2D rb = arrow.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = dir * arrowSpeed;

        Arrow arrowScript = arrow.GetComponent<Arrow>();
        if (arrowScript != null)
        {
            arrowScript.damage = arrowDamage;
            arrowScript.lifeTime = arrowLifeTime;
        }

        Collider2D arrowCol = arrow.GetComponent<Collider2D>();
        Collider2D enemyCol = GetComponent<Collider2D>();
        if (arrowCol != null && enemyCol != null)
            Physics2D.IgnoreCollision(arrowCol, enemyCol);

        Destroy(arrow, arrowLifeTime + 0.5f);
    }

}
