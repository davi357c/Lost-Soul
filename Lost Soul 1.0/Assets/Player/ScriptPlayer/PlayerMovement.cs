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

    [Header("Agilidade (buff temporário)")]
    public bool isAgilityBoosted = false;
    public float agilityMultiplier = 1.5f;
    public float agilityDuration = 15f;
    private Coroutine agilityCoroutine;

    [Tooltip("Velocidade vertical máxima (módulo) permitida durante o slide. (Opcional, 0 = sem limite)")]
    public float wallSlideMaxDownSpeed = 8f;
    public float wallJumpForce = 14f;
    public Vector2 wallJumpDirection = new Vector2(1f, 1.2f);
    public float wallJumpTime = 0.2f;
    public float wallStickTime = 1f;

    [Header("Wall Slide (Aceleração Progressiva)")]
    public float wallSlideMinGravityScale = 0.25f;
    public float wallSlideMaxGravityScale = 1.0f;
    public float wallSlideAccelDuration = 1.0f;
    public AnimationCurve wallSlideAccelCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float wallNoAttachTime = 0.15f;

    [Header("Desbloqueios")]
    [Tooltip("Se falso, o player NÃO consegue escalar/pular na parede. É liberado pelo puzzle.")]
    public bool canWallClimb = false;

    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping;
    private float wallJumpTimer;
    private float noAttachTimer;
    private int lastWallSide = 0;
    private float defaultGravityScale = 1f;
    private float wallSlideElapsed = 0f;

    private float wallStickTimer;
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
    public int pogoForce = 8;

    [Tooltip("Referências para os Hitboxes de ataque")]
    public GameObject AttackHitboxFront;
    public GameObject AttackHitboxUp;
    public GameObject AttackHitboxDown;

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

        if (groundCheck == null) Debug.LogError("GroundCheck não atribuído!");
        if (wallCheck == null) Debug.LogError("WallCheck não atribuído!");

        defaultGravityScale = rb.gravityScale;

        if (PlayerPrefs.HasKey("LastCheckpointX") && PlayerPrefs.HasKey("LastCheckpointY"))
        {
            float x = PlayerPrefs.GetFloat("LastCheckpointX");
            float y = PlayerPrefs.GetFloat("LastCheckpointY");
            transform.position = new Vector2(x, y);
            Debug.Log($"[PlayerMovement] Player carregado no último checkpoint: ({x}, {y})");
        }
        else
        {
            lastSafePosition = transform.position;
        }
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

            // SISTEMA DE ATAQUE COM HITBOXES
            if (Input.GetMouseButtonDown(0) && !isAttacking)
            {
                if (Input.GetKey(KeyCode.S) && !isGrounded)
                {
                    animator.SetTrigger("DownAttack"); // dispara animação imediatamente
                    StartCoroutine(DownAttackRoutine());
                }
                else if (Input.GetKey(KeyCode.W))
                {
                    animator.SetTrigger("UpAttack");
                    StartCoroutine(UpAttackRoutine());
                }
                else
                {
                    animator.SetTrigger("Attack");
                    StartCoroutine(AttackRoutine());
                }
            }

            HandleLookDown();

            if (Input.GetKeyDown(KeyCode.Q))
                StartCoroutine(DashRoutine());

            // SÓ PERMITE SLIDE / JUMP EM PAREDE SE O PUZZLE JÁ TIVER SIDO CONCLUÍDO
            if (canWallClimb)
            {
                WallSlideAndJump();
            }
            else
            {
                // Garante que não fique "grudado" na parede nem com gravidade alterada
                isWallSliding = false;
                if (!isWallJumping)
                    rb.gravityScale = defaultGravityScale;
            }
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
            isWallJumping = false;
            noAttachTimer = 0f;
        }
    }

    void WallSlideAndJump()
    {
        int faceDir = isFacingRight ? 1 : -1;
        Vector2 checkDir = new Vector2(faceDir, 0f);
        Vector2 origin = wallCheck != null ? (Vector2)wallCheck.position : (Vector2)transform.position;
        bool ignoreAttach = noAttachTimer > 0f;
        RaycastHit2D wallHit = Physics2D.Raycast(origin, checkDir, wallCheckDistance, whatIsWall);
        isTouchingWall = wallHit.collider != null;
        int currentWallSide = 0;
        if (isTouchingWall) currentWallSide = faceDir;

        if (noAttachTimer > 0f) noAttachTimer -= Time.deltaTime;

        bool canWallSlideInternal = isTouchingWall && !isGrounded && rb.linearVelocity.y <= 0.05f;
        if (ignoreAttach && currentWallSide != 0 && currentWallSide == lastWallSide)
            canWallSlideInternal = false;

        if (canWallSlideInternal)
        {
            if (!isWallSliding)
            {
                isWallSliding = true;
                wallSlideElapsed = 0f;
                lastWallSide = currentWallSide;
            }
            else wallSlideElapsed += Time.deltaTime;

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            float t = wallSlideAccelDuration > 0f ? Mathf.Clamp01(wallSlideElapsed / wallSlideAccelDuration) : 1f;
            float curveEval = Mathf.Clamp01(wallSlideAccelCurve.Evaluate(t));
            float gravMul = Mathf.Lerp(wallSlideMinGravityScale, wallSlideMaxGravityScale, curveEval);
            rb.gravityScale = defaultGravityScale * gravMul;

            if (wallSlideMaxDownSpeed > 0f)
            {
                float maxDownNow = Mathf.Lerp(0.5f, wallSlideMaxDownSpeed, curveEval);
                if (rb.linearVelocity.y < -maxDownNow)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxDownNow);
            }

            if (Input.GetButtonDown("Jump"))
            {
                int away = currentWallSide == 1 ? -1 : 1;
                Vector2 jumpDir = new Vector2(away * Mathf.Abs(wallJumpDirection.x), Mathf.Abs(wallJumpDirection.y)).normalized;
                rb.gravityScale = defaultGravityScale;
                rb.linearVelocity = jumpDir * wallJumpForce;
                if (away > 0 && !isFacingRight) Flip();
                else if (away < 0 && isFacingRight) Flip();

                isWallJumping = true;
                wallJumpTimer = wallJumpTime;
                noAttachTimer = wallNoAttachTime;
                isWallSliding = false;
                wallSlideElapsed = 0f;
            }
        }
        else
        {
            if (isWallSliding)
                rb.gravityScale = defaultGravityScale;

            isWallSliding = false;
            wallSlideElapsed = 0f;
            if (!isWallJumping)
                rb.gravityScale = defaultGravityScale;
        }

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
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
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

    // ==== COROUTINES DE ATAQUE ====
    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        if (AttackHitboxFront != null) AttackHitboxFront.SetActive(true);
        yield return new WaitForSeconds(0.3f);
        if (AttackHitboxFront != null) AttackHitboxFront.SetActive(false);
        isAttacking = false;
    }


    IEnumerator UpAttackRoutine()
    {
        isAttacking = true;
        if (AttackHitboxUp != null) AttackHitboxUp.SetActive(true);
        yield return new WaitForSeconds(0.3f);
        if (AttackHitboxUp != null) AttackHitboxUp.SetActive(false);
        isAttacking = false;
    }

    IEnumerator DownAttackRoutine()
    {
        isAttacking = true;

        if (AttackHitboxDown != null) AttackHitboxDown.SetActive(true);

        // Espera um passo de física para garantir que o collider do hitbox já está ativo
        yield return new WaitForFixedUpdate();

        Vector2 center = AttackHitboxDown != null ? (Vector2)AttackHitboxDown.transform.position : rb.position;

        // Só considera colisores na Layer Enemy dentro do raio definido
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(center, downAttackRange, enemyLayer);

        bool hitValidEnemy = false;
        for (int i = 0; i < hitEnemies.Length; i++)
        {
            if (hitEnemies[i] == null) continue;

            // Garante que o inimigo está abaixo do player (ataque realmente "pra baixo")
            if (hitEnemies[i].transform.position.y < transform.position.y)
            {
                hitValidEnemy = true;
                break;
            }
        }

        // Só dá o "pogo" se realmente acertou um inimigo válido
        if (hitValidEnemy)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, pogoForce);
        }

        yield return new WaitForSeconds(0.3f);

        if (AttackHitboxDown != null) AttackHitboxDown.SetActive(false);

        isAttacking = false;
    }

    // ANIMATION EVENTS (mantidos por compatibilidade)
    public void OnAttackHit() { }
    public void OnDownAttackHit() { }
    public void OnUpAttackHit() { }

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

    public void EnableWallClimb()
    {
        canWallClimb = true;
        Debug.Log("[PlayerMovement] Escalada em parede liberada pelo puzzle!");
    }

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
        if (rb != null) rb.linearVelocity = Vector2.zero;
        isWallSliding = false;
        isWallJumping = false;
        wallStickTimer = 0f;
        wallSlideElapsed = 0f;
        noAttachTimer = 0f;
        lastWallSide = 0;
        rb.gravityScale = defaultGravityScale;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = UnityEngine.Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
        if (wallCheck != null)
        {
            Gizmos.color = UnityEngine.Color.cyan;
            Vector3 origin = wallCheck.position;
            Vector3 dir = (isFacingRight ? Vector2.right : Vector2.left) * wallCheckDistance;
            Gizmos.DrawLine(origin, origin + dir);
            Gizmos.DrawWireSphere(origin + dir, 0.05f);
        }

        // Visualização da área de ataque para baixo (apenas no editor)
        if (AttackHitboxDown != null)
        {
            Gizmos.color = UnityEngine.Color.magenta;
            Gizmos.DrawWireSphere(AttackHitboxDown.transform.position, downAttackRange);
        }
    }

    public void ApplyAgilityBoost(float multiplier, float duration)
    {
        if (agilityCoroutine != null) StopCoroutine(agilityCoroutine);
        agilityCoroutine = StartCoroutine(AgilityBoostRoutine(multiplier, duration));
    }

    private IEnumerator AgilityBoostRoutine(float multiplier, float duration)
    {
        isAgilityBoosted = true;
        float originalSpeed = moveSpeed;
        moveSpeed *= multiplier;
        Debug.Log($"[PlayerMovement] Agilidade aumentada para {moveSpeed} por {duration}s!");
        yield return new WaitForSeconds(duration);
        moveSpeed = originalSpeed;
        isAgilityBoosted = false;
        Debug.Log("[PlayerMovement] Agilidade voltou ao normal.");
    }

#endif
}
