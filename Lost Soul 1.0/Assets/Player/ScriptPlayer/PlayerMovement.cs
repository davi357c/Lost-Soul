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

    private Vector2 lastSafePosition;

    [Header("Respawn")]
    public Vector2 respawnOffset = new Vector2(0, 0.5f);
    public float edgePadding = 0.3f;

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

        if (Input.GetMouseButtonDown(0) && !isAttacking)
            StartCoroutine(AttackRoutine());

        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("isGrounded", isGrounded);
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

    // Respawn seguro sem borda
    public void Respawn()
    {
        Vector2 spawnPos = lastSafePosition;
        spawnPos.y += 0.5f; // mantém o offset original para não nascer dentro do chão

        // Raycast para baixo só para pegar a plataforma e seus limites
        RaycastHit2D hit = Physics2D.Raycast(spawnPos, Vector2.down, 5f, whatIsGround);
        if (hit.collider != null)
        {
            // ajusta horizontal para ficar dentro da plataforma
            float leftEdge = hit.collider.bounds.min.x + edgePadding;
            float rightEdge = hit.collider.bounds.max.x - edgePadding;
            spawnPos.x = Mathf.Clamp(lastSafePosition.x, leftEdge, rightEdge);
        }

        transform.position = spawnPos;
        rb.linearVelocity = Vector2.zero;
    }

}
