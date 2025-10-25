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
    public float downAttackRange = 0.5f; // alcance do ataque para baixo
    public LayerMask enemyLayer;         // camada dos inimigos

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
        moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (moveInput > 0 && !isFacingRight)
            Flip();
        else if (moveInput < 0 && isFacingRight)
            Flip();

        if (Input.GetButtonDown("Jump") && isGrounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

        // Ataque normal, para baixo (só se não estiver no chão) ou para cima
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            if (Input.GetKey(KeyCode.S) && !isGrounded) // ataque para baixo só no ar
                StartCoroutine(DownAttackRoutine());
            else if (Input.GetKey(KeyCode.W)) // ataque para cima
                StartCoroutine(UpAttackRoutine());
            else // ataque normal
                StartCoroutine(AttackRoutine());
        }

        HandleLookDown();

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

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }

    // Ataque para baixo
    IEnumerator DownAttackRoutine()
    {
        isAttacking = true;
        animator.SetTrigger("DownAttack");
        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }

    // Animation Event: chamada no frame do ataque para baixo
    public void CheckDownAttackHit()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position + Vector3.down * 0.5f, downAttackRange, enemyLayer);
        if (hit != null)
        {
            // Impulso para cima só se acertar inimigo
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.7f);

            // Aplica dano no inimigo
            // hit.GetComponent<Enemy>().TakeDamage(1);
        }
    }

    // Ataque para cima
    IEnumerator UpAttackRoutine()
    {
        isAttacking = true;
        animator.SetTrigger("UpAttack");
        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }

    // Animation Event: chamada no frame do ataque para cima
    public void CheckUpAttackHit()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position + Vector3.up * 0.5f, downAttackRange, enemyLayer);
        if (hit != null)
        {
            // Aplica dano no inimigo
            // hit.GetComponent<Enemy>().TakeDamage(1);
        }
    }

    void HandleLookDown()
    {
        bool canLookDown = isGrounded && moveInput == 0 && !isAttacking && Mathf.Abs(rb.linearVelocity.y) < 0.01f;

        if (Input.GetKey(KeyCode.S) && canLookDown)
        {
            holdTimeS += Time.deltaTime;

            if (holdTimeS >= requiredHoldTime)
            {
                if (!isLookingDown)
                {
                    isLookingDown = true;
                    animator.SetBool("lookDown", true);

                    CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                    if (cam != null)
                        cam.LookDown(true);
                }
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
                if (cam != null)
                    cam.LookDown(false);
            }
        }
    }

    public void Respawn()
    {
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
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

}
