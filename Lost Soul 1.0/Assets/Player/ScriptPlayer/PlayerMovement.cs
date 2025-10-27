using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Wall Jump")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.4f;
    public LayerMask whatIsWall;
    public float wallSlideSpeed = 1.5f;
    public float wallJumpForce = 14f;
    public Vector2 wallJumpDirection = new Vector2(1f, 1.2f);
    public float wallJumpTime = 0.2f;
    public float wallStickTime = 1f; // tempo grudado na parede antes de escorregar

    // ==== FIX: variáveis auxiliares para wall logic ====
    [Tooltip("Tempo após o wall jump em que o player NÃO regruda na parede (anti-‘regrab’).")]
    public float wallNoAttachTime = 0.15f;

    [Tooltip("Multiplicador de gravidade durante o slide para suavizar a descida.")]
    public float wallSlideGravityScale = 0.4f;

    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping;
    private float wallJumpTimer;
    private float wallStickTimer;
    private float noAttachTimer;                // evita “regrudar” logo após o pulo
    private int lastWallSide = 0;               // -1 = parede à esquerda, +1 = direita, 0 = nenhuma
    private float defaultGravityScale = 1f;     // gravidade original do Rigidbody2D

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
    public float attackRange = 1f;
    public float downAttackRange = 0.5f;
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
        if (wallCheck == null)
            Debug.LogError("WallCheck não atribuído!");

        defaultGravityScale = rb.gravityScale;
        lastSafePosition = transform.position;
    }

    void Update()
    {
        if (!isDashing)
        {
            moveInput = Input.GetAxisRaw("Horizontal");

            if (!isWallJumping)
                rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

            if (moveInput > 0 && !isFacingRight) Flip();
            else if (moveInput < 0 && isFacingRight) Flip();

            if (Input.GetButtonDown("Jump") && isGrounded)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

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

            if (Input.GetKeyDown(KeyCode.Q))
            {
                StartCoroutine(DashRoutine());
            }

            WallSlideAndJump(); // >>> toda a lógica de parede está aqui
        }

        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("lookDown", isLookingDown);
        animator.SetBool("isWallSliding", isWallSliding);
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        if (isGrounded)
        {
            lastSafePosition = transform.position;
            isWallJumping = false;     // aterrissou -> encerra estado de wall jump
            noAttachTimer = 0f;        // pode voltar a grudar quando pular de novo
        }
    }

    // ===== WALL SLIDE + JUMP (corrigido) =====
    void WallSlideAndJump()
    {
        // Direção para checar parede baseada no facing
        int faceDir = isFacingRight ? 1 : -1;
        Vector2 checkDir = new Vector2(faceDir, 0f);

        // ==== FIX: usar wallCheck como origem da checagem ====
        Vector2 origin = wallCheck != null ? (Vector2)wallCheck.position : (Vector2)transform.position;

        // Se ainda estou no timer de "não regrudar", ignore contato com a MESMA parede do salto
        bool ignoreAttach = noAttachTimer > 0f;

        // Raycast para detectar parede
        RaycastHit2D wallHit = Physics2D.Raycast(origin, checkDir, wallCheckDistance, whatIsWall);
        isTouchingWall = wallHit.collider != null;

        // Descobrir o lado da parede detectada
        int currentWallSide = 0;
        if (isTouchingWall)
            currentWallSide = faceDir; // parede está no lado para o qual estou virado

        // Timer de não regrudar diminui ao longo do tempo
        if (noAttachTimer > 0f)
            noAttachTimer -= Time.deltaTime;

        // Condição para entrar em wall slide: tocando parede, no ar e caindo (ou parado verticalmente)
        bool canWallSlide = isTouchingWall && !isGrounded && rb.linearVelocity.y <= 0.05f;

        // Se devo ignorar regrudar (acabou de dar wall jump) e é a mesma parede, NÃO desliza
        if (ignoreAttach && currentWallSide != 0 && currentWallSide == lastWallSide)
        {
            canWallSlide = false;
        }

        if (canWallSlide)
        {
            // Entrou em wall slide
            isWallSliding = true;

            // FIX: controlar gravidade e velocidade de descida de forma estável
            rb.gravityScale = defaultGravityScale * wallSlideGravityScale;
            if (rb.linearVelocity.y < -wallSlideSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);

            // "Grudar" por um tempo antes de começar a escorregar
            if (wallStickTimer < wallStickTime)
            {
                // zera velocidade para dar sensação de 'grudar'
                rb.linearVelocity = new Vector2(0f, Mathf.Max(rb.linearVelocity.y, -0.01f));
                wallStickTimer += Time.deltaTime;
            }

            // Pulo na parede (afastando do lado dela)
            if (Input.GetButtonDown("Jump"))
            {
                // Direção de salto: sempre para longe da parede.
                // Se a parede está à direita (+1), salto para esquerda (-1), e vice-versa.
                int away = currentWallSide == 1 ? -1 : 1;

                Vector2 jumpDir = new Vector2(away * Mathf.Abs(wallJumpDirection.x), Mathf.Abs(wallJumpDirection.y));
                jumpDir = jumpDir.normalized;

                rb.gravityScale = defaultGravityScale; // restaura gravidade para o salto
                rb.linearVelocity = Vector2.zero;      // zera antes de aplicar o impulso
                rb.linearVelocity = jumpDir * wallJumpForce;

                // Garante a orientação do personagem para o lado do salto
                if (away > 0 && !isFacingRight) Flip();
                else if (away < 0 && isFacingRight) Flip();

                // Marca estados/temporizadores
                isWallJumping = true;
                wallJumpTimer = wallJumpTime;
                noAttachTimer = wallNoAttachTime; // importante!
                lastWallSide = currentWallSide;   // lembra de qual lado saltou

                // Sai do slide imediatamente
                isWallSliding = false;
                wallStickTimer = 0f;
            }
        }
        else
        {
            // Não está em wall slide
            if (isWallSliding)
            {
                // Ao sair do slide, restaura gravidade
                rb.gravityScale = defaultGravityScale;
            }

            isWallSliding = false;
            wallStickTimer = 0f;

            // Se não está em slide e não está no pulo de parede, mantém gravidade normal
            if (!isWallJumping)
                rb.gravityScale = defaultGravityScale;
        }

        // Timer do estado "wall jump" (impede movimento horizontal sobrescrever imediatamente)
        if (isWallJumping)
        {
            wallJumpTimer -= Time.deltaTime;
            if (wallJumpTimer <= 0f)
                isWallJumping = false;
        }
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

        int playerLayer = gameObject.layer;
        Physics2D.IgnoreLayerCollision(playerLayer, LayerMaskToLayer(dashThroughWalls), true);

        // Durante o dash, gravidade 0 para não cair
        float prevGrav = rb.gravityScale;
        rb.gravityScale = 0f;

        while (elapsed < dashDuration)
        {
            rb.linearVelocity = dashDir * dashSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = prevGrav;

        Physics2D.IgnoreLayerCollision(playerLayer, LayerMaskToLayer(dashThroughWalls), false);
        isDashing = false;
    }

    public bool IsDashing() => isDashing;

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
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.7f);
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

    // ================================
    // ZERAR VELOCIDADE AO TROCAR DE CENA
    // ================================
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Reinicia estados de parede ao carregar nova cena
        isWallSliding = false;
        isWallJumping = false;
        wallStickTimer = 0f;
        noAttachTimer = 0f;
        lastWallSide = 0;
        rb.gravityScale = defaultGravityScale;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }

        // Gizmos de parede
        if (wallCheck != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 origin = wallCheck.position;
            Vector3 dir = (isFacingRight ? Vector2.right : Vector2.left) * wallCheckDistance;
            Gizmos.DrawLine(origin, origin + dir);
            Gizmos.DrawWireSphere(origin + dir, 0.05f);
        }
    }
#endif
}
