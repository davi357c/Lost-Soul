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

    // Última posição segura
    private Vector2 lastSafePosition;

    [Header("Respawn Offset")]
    public Vector2 respawnOffset = new Vector2(0, 0.5f); // sobe 0.5 unidades na hora do respawn

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

        // Flip do sprite
        if (moveInput > 0 && !isFacingRight)
            Flip();
        else if (moveInput < 0 && isFacingRight)
            Flip();

        // Pulo
        if (Input.GetButtonDown("Jump") && isGrounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

        // Ataque
        if (Input.GetMouseButtonDown(0) && !isAttacking)
            StartCoroutine(AttackRoutine());

        // Animações
        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("isGrounded", isGrounded);
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        // Atualiza posição segura só quando no chão
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

    // Chamado pelos espinhos
    public void Respawn()
    {
        Vector2 spawnPos = lastSafePosition;

        // Ajusta X para não nascer colado na borda
        float offsetX = 1f; // 0.5 unidades dentro da plataforma

        // Se o player estava à direita do centro do lastSafePosition, respawna para a esquerda
        if (transform.position.x > lastSafePosition.x)
            spawnPos.x -= offsetX;
        else
            spawnPos.x += offsetX;

        // Sobe um pouco para não nascer dentro do chão
        spawnPos.y += 0.5f;

        transform.position = spawnPos;
        rb.linearVelocity = Vector2.zero;
    }

}
