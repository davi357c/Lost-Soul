using UnityEngine;

public class EnemyAIPlatformerFixed : MonoBehaviour
{
    [Header("Player & Movement")]
    public Transform player;
    public float moveSpeed = 3f;
    public float jumpForce = 7f;
    public float chaseRange = 6f;

    [Header("Detection")]
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask groundLayer;
    public float wallCheckDistance = 0.3f;
    public float groundCheckDistance = 0.5f;

    [Header("Patrol")]
    public float patrolDistance = 3f;

    private Rigidbody2D rb;
    private bool isFacingRight = true;
    private bool isGrounded;
    private Vector2 startPos;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
    }

    void Update()
    {
        if (player == null) return;

        CheckGround();

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= chaseRange)
            ChasePlayer();
        else
            Patrol();
    }

    void CheckGround()
    {
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
    }

    void ChasePlayer()
    {
        float direction = player.position.x - transform.position.x;

        // vira o inimigo
        if (direction > 0 && !isFacingRight) Flip();
        else if (direction < 0 && isFacingRight) Flip();

        MoveAndJump(direction);
    }

    void Patrol()
    {
        float direction = isFacingRight ? 1 : -1;

        MoveAndJump(direction);

        // se chegou no limite da patrulha, vira
        if (Mathf.Abs(transform.position.x - startPos.x) >= patrolDistance)
            Flip();
    }

    void MoveAndJump(float direction)
    {
        rb.linearVelocity = new Vector2(Mathf.Sign(direction) * moveSpeed, rb.linearVelocity.y);

        Vector2 rayDir = isFacingRight ? Vector2.right : Vector2.left;
        bool wallAhead = Physics2D.Raycast(wallCheck.position, rayDir, wallCheckDistance, groundLayer);
        bool noGroundAhead = !Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);

        Debug.DrawRay(wallCheck.position, rayDir * wallCheckDistance, Color.red);
        Debug.DrawRay(groundCheck.position, Vector2.down * groundCheckDistance, Color.green);

        if (isGrounded && (wallAhead || noGroundAhead))
            Flip();
    }


    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck) Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        if (wallCheck) Gizmos.DrawLine(wallCheck.position, wallCheck.position + (isFacingRight ? Vector3.right : Vector3.left) * wallCheckDistance);

        if (player)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(startPos, startPos + Vector2.right * patrolDistance);
        Gizmos.DrawLine(startPos, startPos + Vector2.left * patrolDistance);
    }
}
