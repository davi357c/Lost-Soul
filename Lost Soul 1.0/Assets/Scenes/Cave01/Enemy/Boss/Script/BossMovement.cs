using UnityEngine;

public class BossMovement : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;                 // Pode arrastar o Player aqui, ou ele acha pelo tag
    public string playerTag = "Player";      // Tag do Player
    public string playerLayerName = "Player";// Layer do Player

    [Header("Detecção do Player")]
    public float wakeUpDistance = 8f;        // Distância para acordar o boss

    [Header("Movimentação")]
    public float moveSpeed = 3f;             // Velocidade do boss
    public float offsetRange = 3f;           // Variação aleatória no X em torno do player
    public float timeBetweenNewTargets = 2f; // Tempo pra trocar de alvo aleatório

    private Rigidbody2D rb;
    private Animator anim;

    private bool isAwake = false;
    private Vector2 currentTarget;
    private float targetTimer;
    private int playerLayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Pega o índice da layer "Player"
        if (!string.IsNullOrEmpty(playerLayerName))
            playerLayer = LayerMask.NameToLayer(playerLayerName);
        else
            playerLayer = -1;

        // Se não tiver referência manual, tenta achar pelo tag "Player"
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (!isAwake)
        {
            CheckWakeUp();
        }
        else
        {
            UpdateTargetPosition();
        }
    }

    private void FixedUpdate()
    {
        if (isAwake)
        {
            MoveBoss();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // Verifica se o player está perto o suficiente para acordar
    private void CheckWakeUp()
    {
        if (player == null) return;

        // Garante que é mesmo o player na layer certa (Player)
        if (playerLayer != -1 && player.gameObject.layer != playerLayer)
            return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= wakeUpDistance)
        {
            isAwake = true;

            if (anim != null)
            {
                anim.SetTrigger("awake");
            }
        }
    }

    // Alvo "inteligente" e meio aleatório ao redor do player
    private void UpdateTargetPosition()
    {
        if (player == null) return;

        targetTimer -= Time.deltaTime;

        if (targetTimer <= 0f)
        {
            float randomOffsetX = Random.Range(-offsetRange, offsetRange);

            // Anda no X do player +- offset, mantendo o Y atual do boss
            currentTarget = new Vector2(player.position.x + randomOffsetX, rb.position.y);

            targetTimer = timeBetweenNewTargets;
        }
    }

    // Movimento até o alvo atual
    private void MoveBoss()
    {
        float distanceToTarget = Vector2.Distance(rb.position, currentTarget);

        // Se já está bem perto do alvo, para um pouco
        if (distanceToTarget < 0.2f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = (currentTarget - rb.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        // Flip pra direção que ele está indo
        if (direction.x > 0.05f)
        {
            transform.localScale = new Vector3(10f, 10f, 10f);
        }
        else if (direction.x < -0.05f)
        {
            transform.localScale = new Vector3(-10f, 10f, 10f);
        }
    }

    // Gizmo pra ver o range de wake-up
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, wakeUpDistance);
    }
}
