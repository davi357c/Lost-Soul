using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 5f;
    private float moveInput;
    private bool isFacingRight = true;

    [Header("Pulo")]
    public float jumpForce = 14f;
    private bool isGrounded;

    [Header("Checagem de chão")]
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask whatIsGround;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool isAttacking = false;
    private bool isLookingDown = false;
    private float holdTimeS = 0f;
    private float requiredHoldTime = 3f;

    private Vector2 lastSafePosition;

    [Header("Respawn")]
    public Vector2 respawnOffset = new Vector2(0, 0.5f);
    public float edgePadding = 0.3f;

    [Header("Ataque")]
    public float attackRange = 1f;          // horizontal
    public float downAttackRange = 0.5f;    // vertical
    public LayerMask enemyLayer;
    public int attackDamage = 1;
    public float knockbackForce = 5f;

    [Header("Dash")]
    public float dashDistance = 5f;
    public float dashDuration = 0.2f;
    public LayerMask dashThroughWalls;
    private bool isDashing = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (groundCheck == null)
            Debug.LogError("GroundCheck não atribuído!");

        lastSafePosition = transform.position;
    }

    void Update()
    {
        if (!isDashing)
        {
            moveInput = Input.GetAxisRaw("Horizontal");
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

            if (moveInput > 0 && !isFacingRight) Flip();
            else if (moveInput < 0 && isFacingRight) Flip();

            if (Input.GetButtonDown("Jump") && isGrounded)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

            // Ataque
            if (Input.GetMouseButtonDown(0) && !isAttacking)
            {
                if (Input.GetKey(KeyCode.S) && !isGrounded)
                    StartCoroutine(DownAttackRoutine());
                else if (Input.GetKey(KeyCode.W))
                    StartCoroutine(UpAttackRoutine());
                else
                    StartCoroutine(AttackRoutine());
            }

            HandleLookDown();

            // Dash
            if (Input.GetKeyDown(KeyCode.Q))
            {
                StartCoroutine(DashRoutine());
            }
        }

        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("lookDown", isLookingDown);
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        if (isGrounded)
            lastSafePosition = transform.position;
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        spriteRenderer.flipX = !spriteRenderer.flipX;
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        animator.SetTrigger("Dash");

        Vector2 dashDir = isFacingRight ? Vector2.right : Vector2.left;
        float dashSpeed = dashDistance / dashDuration;
        float elapsed = 0f;

        // Desliga colisão apenas com a layer dashThroughWalls
        int playerLayer = gameObject.layer;
        Physics2D.IgnoreLayerCollision(playerLayer, LayerMaskToLayer(dashThroughWalls), true);

        while (elapsed < dashDuration)
        {
            rb.linearVelocity = dashDir * dashSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        // Restaura colisão
        Physics2D.IgnoreLayerCollision(playerLayer, LayerMaskToLayer(dashThroughWalls), false);

        isDashing = false;
    }

    public bool IsDashing()
    {
        return isDashing;
    }


    // Converte LayerMask em Layer (suporta apenas 1 layer ativa no mask)
    private int LayerMaskToLayer(LayerMask mask)
    {
        int layer = 0;
        int maskValue = mask.value;
        while (maskValue > 1)
        {
            maskValue = maskValue >> 1;
            layer++;
        }
        return layer;
    }


    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(0.3f);

        Vector2 attackPos = transform.position + (isFacingRight ? Vector3.right : Vector3.left) * attackRange;
        DamageEnemies(attackPos);

        isAttacking = false;
    }

    IEnumerator DownAttackRoutine()
    {
        isAttacking = true;
        animator.SetTrigger("DownAttack");
        yield return new WaitForSeconds(0.3f);

        Vector2 attackPos = transform.position + Vector3.down * downAttackRange;
        bool hitEnemy = DamageEnemies(attackPos);

        if (hitEnemy)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.7f);
        }

        isAttacking = false;
    }

    IEnumerator UpAttackRoutine()
    {
        isAttacking = true;
        animator.SetTrigger("UpAttack");
        yield return new WaitForSeconds(0.3f);

        Vector2 attackPos = transform.position + Vector3.up * downAttackRange;
        DamageEnemies(attackPos);

        isAttacking = false;
    }

    private bool DamageEnemies(Vector2 attackPosition)
    {
        bool hitEnemy = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPosition, attackRange, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                Vector2 dir = (hit.transform.position - transform.position).normalized;
                enemy.TakeDamage(dir, attackDamage);
                hitEnemy = true;
            }
        }
        return hitEnemy;
    }

    void HandleLookDown()
    {
        bool canLookDown = isGrounded && moveInput == 0 && !isAttacking && Mathf.Abs(rb.linearVelocity.y) < 0.01f;

        if (Input.GetKey(KeyCode.S) && canLookDown)
        {
            holdTimeS += Time.deltaTime;
            if (holdTimeS >= requiredHoldTime && !isLookingDown)
            {
                isLookingDown = true;
                animator.SetBool("lookDown", true);

                CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                if (cam != null) cam.LookDown(true);
            }
        }
        else
        {
            holdTimeS = 0f;
            if (isLookingDown)
            {
                isLookingDown = false;
                animator.SetBool("lookDown", false);

                CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                if (cam != null) cam.LookDown(false);
            }
        }
    }

    public void Respawn(float delay = 0.3f)
    {
        StartCoroutine(RespawnRoutine(delay));
    }

    private IEnumerator RespawnRoutine(float delay)
    {
        enabled = false;
        yield return new WaitForSeconds(delay);

        Vector2 spawnPos = lastSafePosition;
        spawnPos.y += 0.5f;

        RaycastHit2D hit = Physics2D.Raycast(spawnPos, Vector2.down, 5f, whatIsGround);
        if (hit.collider != null)
        {
            float leftEdge = hit.collider.bounds.min.x + edgePadding;
            float rightEdge = hit.collider.bounds.max.x - edgePadding;
            spawnPos.x = Mathf.Clamp(lastSafePosition.x, leftEdge, rightEdge);
        }

        transform.position = spawnPos;
        rb.linearVelocity = Vector2.zero;
        enabled = true;
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
