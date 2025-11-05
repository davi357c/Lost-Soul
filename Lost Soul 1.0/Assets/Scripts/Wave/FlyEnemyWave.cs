using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyEnemyWave : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 2f;
    public float chaseDistance = 20f; // distância máxima para detectar o player

    [Header("Referências")]
    public Transform playerTransform;

    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D col;
    private FlyEnemyHealth enemyHealth;

    private bool isMovementDisabled = false;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        enemyHealth = GetComponent<FlyEnemyHealth>();

        // procura automaticamente o player se não tiver setado no inspetor
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void Update()
    {
        if (isMovementDisabled) return;

        // Se morreu, desativa tudo
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            DisableMovement();
            return;
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else return;
        }

        // Segue o player continuamente
        Vector3 targetPos = playerTransform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        Vector2 moveDir = (targetPos - transform.position).normalized;

        // vira pro lado certo
        if (moveDir.x > 0.01f)
            transform.localScale = new Vector3(-1, 1, 1);
        else if (moveDir.x < -0.01f)
            transform.localScale = new Vector3(1, 1, 1);

        if (animator != null)
            animator.SetFloat("Speed", moveDir.magnitude);
    }

    public void DisableMovement()
    {
        if (isMovementDisabled) return;
        isMovementDisabled = true;

        if (animator != null)
            animator.SetFloat("Speed", 0f);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
            rb.simulated = false;
        }

        // Desativa todos colliders (objeto + filhos)
        foreach (var c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        if (col != null)
            col.enabled = false;

        enabled = false;
    }
}
