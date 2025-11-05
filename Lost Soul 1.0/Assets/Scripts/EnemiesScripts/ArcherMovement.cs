using UnityEngine;

public class ArcherMovement : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;
    public int patrolDestination;

    [Header("Player / Perseguição")]
    public Transform playerTransform;
    public string playerTag = "Player";
    public bool isChasing;
    public float chaseDistance = 5f;

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
    private Vector3 originalScale;

    // para reduzir spam de log
    private float nextSearchLogTime = 0f;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
        originalScale = transform.localScale;
    }

    void Start()
    {
        TryFindPlayer();
        if (playerTransform == null)
            Debug.LogWarning($"[{name}] playerTransform null no Start(). Verifique tag '{playerTag}' e se o Player está ativo na cena.");
    }

    void Update()
    {
        // tenta encontrar player novamente caso esteja null (útil se player for instanciado depois)
        if (playerTransform == null)
        {
            // escreve no log no máximo a cada 2 segundos pra não spammar
            if (Time.time >= nextSearchLogTime)
            {
                Debug.Log($"[{name}] playerTransform ainda null. Tentando FindWithTag('{playerTag}')...");
                nextSearchLogTime = Time.time + 2f;
            }
            TryFindPlayer();
        }

        if (enemyHealth != null && enemyHealth.IsDead)
        {
            if (animator != null)
                animator.SetFloat("Speed", 0f);
            return;
        }

        if (playerTransform == null) return; // sem player, nada a fazer

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        // lógica simples: se estiver dentro de chaseDistance, ataca; se estiver um pouco mais longe, persegue
        if (dist <= chaseDistance)
        {
            isChasing = false;
            isAttacking = true;
        }
        else if (dist <= chaseDistance + 6f) // margem arbitrária para perseguição
        {
            isChasing = true;
            isAttacking = false;
        }
        else
        {
            isChasing = false;
            isAttacking = false;
        }

        if (isAttacking)
        {
            FaceTarget();
            if (animator != null)
                animator.SetFloat("Speed", 0f);
            HandleShooting();
        }
        else if (isChasing)
        {
            if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead)
            {
                isChasing = false;
                return;
            }

            Vector3 dir = (playerTransform.position - transform.position).normalized;
            transform.position += Vector3.right * Mathf.Sign(dir.x) * moveSpeed * Time.deltaTime;
            transform.localScale = new Vector3(Mathf.Sign(dir.x) * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

            if (animator != null)
                animator.SetFloat("Speed", Mathf.Abs(dir.x));
        }
        else // patrulha
        {
            Patrol();
        }
    }

    void TryFindPlayer()
    {
        if (playerTransform != null) return;
        GameObject p = GameObject.FindWithTag(playerTag);
        if (p != null)
        {
            playerTransform = p.transform;
            Debug.Log($"[{name}] Player encontrado automaticamente: {p.name}");
        }
    }

    void FaceTarget()
    {
        if (playerTransform.position.x < transform.position.x)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length < 2) return;
        Transform target = patrolPoints[patrolDestination];
        Vector2 moveDir = (target.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) < 0.2f)
            patrolDestination = 1 - patrolDestination;

        if (animator != null)
            animator.SetFloat("Speed", Mathf.Abs(moveDir.x));
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

    // visualizar chaseDistance no editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);
    }
}
