using System.Collections;
using UnityEngine;

/// <summary>
/// IA t�tica estilo Silksong para um inimigo 2D com patrulha, persegui��o,
/// recuo estrat�gico, ataque com cooldown, dano/knockback e anima��es completas.
/// OBS: O ataque aqui apenas dispara a ANIMA��O. A hitbox de ataque � gerida externamente.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public class EnemyGolemBlue : MonoBehaviour, IDamageable
{
    // ====== Estados ======
    public enum State { Idle, Patrolling, Chasing, Attacking, Hurt, Dying }
    [SerializeField] private State currentState = State.Idle;

    // ====== Refer�ncias ======
    [Header("References")]
    [SerializeField] private Transform player;            // Se vazio, ser� encontrado por tag "Player" no Start
    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D col;

    // ====== Atributos ======
    [Header("Attributes")]
    [SerializeField] private int maxHP = 30;
    [SerializeField] private int touchDamage = 0;         // Dano por contato (opcional; deixar 0 para desativar)
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float chaseSpeed = 2.8f;
    [SerializeField] private float retreatSpeed = 2.4f;

    [Tooltip("Dist�ncia em que o inimigo come�a a perseguir o jogador.")]
    [SerializeField] private float detectionRadius = 8f;

    [Tooltip("Se o jogador sair al�m disso, o inimigo larga a persegui��o e retorna a patrulha.")]
    [SerializeField] private float giveUpChaseRadius = 11f;

    [Tooltip("Dist�ncia para tentar manter do jogador (se ficar menor do que isso, recua).")]
    [SerializeField] private float keepAwayDistance = 1.2f;

    [Tooltip("Alcance para considerar ataque (apenas anima��o e cooldown).")]
    [SerializeField] private float attackRange = 1.9f;

    [Tooltip("Tempo entre ataques.")]
    [SerializeField] private float attackCooldown = 1.2f;

    [Tooltip("Atraso (wind-up) antes do ataque (tempo parado preparando).")]
    [SerializeField] private float attackWindup = 0.18f;

    [Tooltip("Tempo de recupera��o ap�s atacar (sem se mover).")]
    [SerializeField] private float attackRecovery = 0.25f;

    [Tooltip("Tempo de invulnerabilidade breve ap�s levar dano.")]
    [SerializeField] private float hurtStaggerTime = 0.18f;

    [Tooltip("For�a do knockback quando recebe dano.")]
    [SerializeField] private float knockbackForce = 7.5f;

    [Tooltip("Delay para sumir ap�s a anima��o de morte iniciar.")]
    [SerializeField] private float deathCleanupDelay = 1.6f;

    // ====== Patrulha ======
    [Header("Patrol")]
    [Tooltip("Pontos de patrulha (opcional). Se n�o setado, patrulha num raio em torno da posi��o inicial.")]
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;

    [Tooltip("Se n�o usar pontos, usa raio a partir do spawn.")]
    [SerializeField] private float patrolRadius = 3.5f;

    [Tooltip("Espera ao tocar um limite de patrulha.")]
    [SerializeField] private Vector2 patrolPauseRange = new Vector2(0.3f, 1.0f);

    private Vector3 spawnPos;
    private int hp;
    private float attackTimer = 0f;
    private bool facingRight = true;
    private bool invulnerable = false;
    private bool canDecide = true;             // Evita decis�es a cada frame (timing mais "org�nico")
    private float decideIntervalMin = 0.12f;   // Janela m�nima para recalcular inten��o
    private float decideIntervalMax = 0.25f;

    // ====== Detec��o & Solo/Obst�culos ======
    [Header("Sensing")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask playerMask;
    [Tooltip("Offset vertical do ponto de vis�o (Raycast) para evitar ch�o.")]
    [SerializeField] private float sightYOffset = 0.5f;
    [Tooltip("Evitar cair de bordas durante patrulha/persegui��o.")]
    [SerializeField] private bool avoidLedges = true;
    [SerializeField] private float ledgeCheckDistance = 0.5f;

    // ====== Animator Params (nomes) ======
    // Use estes nomes exatamente no Animator
    private static readonly int HashMove = Animator.StringToHash("Moving");
    private static readonly int HashRetreat = Animator.StringToHash("Retreat");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private static readonly int HashHurt = Animator.StringToHash("Hurt");
    private static readonly int HashDie = Animator.StringToHash("Die");

    // ====== Outros ======
    private Vector2 patrolTarget;      // alvo atual de patrulha (quando sem waypoints)
    private bool hasWaypoints => leftPoint != null && rightPoint != null;

    // ======================= Unity =======================
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 3.5f;
        rb.freezeRotation = true;
    }

    private void Start()
    {
        // Procura o primeiro objeto na layer "Player"
        if (player == null)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.layer == playerLayer)
                {
                    player = obj.transform;
                    break;
                }
            }
        }

        spawnPos = transform.position;
        hp = maxHP;

        currentState = State.Patrolling;
        ChooseNextPatrolTarget();
        StartCoroutine(DecisionTicker());
    }

    private void Update()
    {
        if (currentState == State.Dying) return;

        attackTimer -= Time.deltaTime;

        // Avalia mudan�as de estado reativas (ex.: perdeu vis�o do player)
        EvaluateState();

        // Par�metros de anima��o de locomo��o
        bool moving = Mathf.Abs(rb.linearVelocity.x) > 0.05f && currentState != State.Attacking && currentState != State.Hurt;
        anim.SetBool(HashMove, moving);
    }

    private void FixedUpdate()
    {
        if (currentState == State.Dying) { rb.linearVelocity = Vector2.zero; return; }

        switch (currentState)
        {
            case State.Idle:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;

            case State.Patrolling:
                PatrolMove();
                break;

            case State.Chasing:
                ChaseOrRetreatMove();
                break;

            case State.Attacking:
                // Movimento travado durante windup/recovery
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;

            case State.Hurt:
                // Knockback est� sendo processado pela f�sica; n�o for�a movimento aqui
                break;
        }
    }

    // ======================= FSM & L�gica =======================
    private void EvaluateState()
    {
        if (currentState == State.Hurt || currentState == State.Attacking || currentState == State.Dying) return;

        bool seesPlayer = PlayerInSight(detectionRadius);
        float dist = PlayerDistance();

        if (!seesPlayer)
        {
            // Se estava perseguindo e perdeu o player (ou est� muito longe), volta � patrulha
            if (currentState == State.Chasing && dist > giveUpChaseRadius)
                SwitchState(State.Patrolling);
            else if (currentState != State.Patrolling)
                SwitchState(State.Patrolling);

            return;
        }

        // Se v� o player: avaliar aproxima��o/ataque/recuo
        if (dist <= attackRange && attackTimer <= 0f)
        {
            StartCoroutine(DoAttack());
            return;
        }

        // Caso contr�rio, perseguir (com eventual recuo j� tratado em ChaseOrRetreatMove)
        if (currentState != State.Chasing)
            SwitchState(State.Chasing);
    }

    private void SwitchState(State next)
    {
        if (currentState == next) return;

        currentState = next;

        // Flags de anima��o de recuo
        bool retreating = (currentState == State.Chasing) && PlayerDistance() < keepAwayDistance;
        anim.SetBool(HashRetreat, retreating);

        if (next == State.Patrolling)
            ChooseNextPatrolTarget();
    }

    private IEnumerator DoAttack()
    {
        SwitchState(State.Attacking);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Olha para o player antes
        FaceToTarget(player.position.x);

        // Wind-up (timing antes do golpe)
        yield return new WaitForSeconds(attackWindup);

        // Dispara anima��o (hitbox/resultado do ataque � gerido fora deste script)
        anim.ResetTrigger(HashAttack);
        anim.SetTrigger(HashAttack);

        // Recovery (n�o se mover um pouco ap�s atacar)
        yield return new WaitForSeconds(attackRecovery);

        attackTimer = attackCooldown;

        // Ap�s atacar, faz um micro-recuo t�tico se estiver muito perto
        if (PlayerDistance() < keepAwayDistance * 0.9f)
        {
            StartCoroutine(ShortRetreatBurst(0.20f));
        }

        // Volta a decidir: perseguir ou patrulhar
        SwitchState(PlayerInSight(detectionRadius) ? State.Chasing : State.Patrolling);
    }

    private IEnumerator ShortRetreatBurst(float t)
    {
        float timer = t;
        while (timer > 0f)
        {
            if (player != null)
            {
                Vector2 dir = (transform.position.x < player.position.x) ? Vector2.left : Vector2.right;
                TryMove(dir * retreatSpeed);
                anim.SetBool(HashRetreat, true);
            }
            timer -= Time.deltaTime;
            yield return null;
        }
        anim.SetBool(HashRetreat, false);
    }

    private IEnumerator DecisionTicker()
    {
        // Evita recalcular decis�o todo frame; deixa o comportamento mais humano
        while (true)
        {
            canDecide = true;
            float wait = Random.Range(decideIntervalMin, decideIntervalMax);
            yield return new WaitForSeconds(wait);
        }
    }

    // ======================= Movimento =======================
    private void PatrolMove()
    {
        Vector2 target;
        if (hasWaypoints)
        {
            // Vai na dire��o do waypoint mais pr�ximo do alvo atual
            target = patrolTarget;
            Vector2 dir = new Vector2(Mathf.Sign(target.x - transform.position.x), 0f);
            bool reached = Mathf.Abs(target.x - transform.position.x) <= 0.1f;

            if (reached || BlockedAhead(dir) || LedgeAheadMissing(dir))
            {
                // Altera alvo e espera um pouco
                ChooseNextPatrolTarget();
                StartCoroutine(PatrolPause());
            }
            else
            {
                TryMove(dir * moveSpeed);
            }
        }
        else
        {
            // Patrulha entre (spawn - radius) e (spawn + radius)
            float left = spawnPos.x - patrolRadius;
            float right = spawnPos.x + patrolRadius;

            if (patrolTarget == Vector2.zero)
                patrolTarget = new Vector2(Random.Range(left, right), transform.position.y);

            Vector2 dir = new Vector2(Mathf.Sign(patrolTarget.x - transform.position.x), 0f);
            bool reached = Mathf.Abs(patrolTarget.x - transform.position.x) <= 0.1f;

            if (reached || BlockedAhead(dir) || LedgeAheadMissing(dir))
            {
                patrolTarget = new Vector2(Random.Range(left, right), transform.position.y);
                StartCoroutine(PatrolPause());
            }
            else
            {
                TryMove(dir * moveSpeed);
            }
        }
    }

    private IEnumerator PatrolPause()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        float w = Random.Range(patrolPauseRange.x, patrolPauseRange.y);
        yield return new WaitForSeconds(w);
    }

    private void ChooseNextPatrolTarget()
    {
        if (hasWaypoints)
        {
            if (patrolTarget == Vector2.zero)
            {
                // Decide o primeiro alvo com base no mais distante
                float distL = Mathf.Abs(transform.position.x - leftPoint.position.x);
                float distR = Mathf.Abs(transform.position.x - rightPoint.position.x);
                patrolTarget = distL > distR ? leftPoint.position : rightPoint.position;
            }
            else
            {
                // Troca (left <-> right)
                Vector2 next = (Mathf.Abs(patrolTarget.x - leftPoint.position.x) < 0.1f) ? rightPoint.position : leftPoint.position;
                patrolTarget = next;
            }
        }
        else
        {
            // Definido no PatrolMove quando necess�rio
            patrolTarget = Vector2.zero;
        }
    }

    private void ChaseOrRetreatMove()
    {
        if (player == null) return;

        float dist = PlayerDistance();

        // Decide frente
        FaceToTarget(player.position.x);

        // Recuo t�tico se perto demais
        bool shouldRetreat = dist < keepAwayDistance * 0.95f;

        if (shouldRetreat)
        {
            anim.SetBool(HashRetreat, true);
            Vector2 away = (transform.position.x < player.position.x) ? Vector2.left : Vector2.right;
            TryMove(away * retreatSpeed);
            return;
        }

        anim.SetBool(HashRetreat, false);

        // Aproxima��o (mas sem cair de penhascos/blocar em paredes)
        if (dist > attackRange * 0.95f)
        {
            Vector2 towards = (transform.position.x < player.position.x) ? Vector2.right : Vector2.left;
            TryMove(towards * chaseSpeed);
        }
        else
        {
            // Em alcance de ataque, n�o "colar": micro ajustes
            rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0f, 20f * Time.fixedDeltaTime), rb.linearVelocity.y);
        }
    }

    private void TryMove(Vector2 desiredVelocity)
    {
        Vector2 dir = new Vector2(Mathf.Sign(desiredVelocity.x), 0f);

        // Evitar queda de bordas e paredes
        if (BlockedAhead(dir) || LedgeAheadMissing(dir))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(desiredVelocity.x, rb.linearVelocity.y);
    }

    private bool BlockedAhead(Vector2 dir)
    {
        // Raycast curto para parede/obst�culo
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.25f;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, 0.3f, obstacleMask);
        return hit.collider != null;
    }

    private bool LedgeAheadMissing(Vector2 dir)
    {
        if (!avoidLedges) return false;
        Vector2 origin = (Vector2)transform.position + dir * 0.25f;
        RaycastHit2D ground = Physics2D.Raycast(origin, Vector2.down, ledgeCheckDistance, groundMask);
        return ground.collider == null;
    }

    private void FaceToTarget(float targetX)
    {
        bool wantRight = targetX >= transform.position.x;
        if (wantRight != facingRight)
            Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }

    // ======================= Detec��o =======================
    private bool PlayerInSight(float radius)
    {
        if (player == null) return false;

        // Primeiro: dentro do raio?
        if (Vector2.Distance(transform.position, player.position) > radius) return false;

        // Segundo: LOS (line of sight) simples
        Vector2 origin = (Vector2)transform.position + Vector2.up * sightYOffset;
        Vector2 dir = (player.position - (Vector3)origin).normalized;
        float dist = Vector2.Distance(origin, player.position);

        RaycastHit2D block = Physics2D.Raycast(origin, dir, dist, obstacleMask);
        return block.collider == null;
    }

    private float PlayerDistance()
    {
        if (player == null) return Mathf.Infinity;
        return Vector2.Distance(transform.position, player.position);
    }

    // ======================= Dano / Knockback / Morte =======================
    public void ApplyDamage(int amount, Vector2 sourcePosition, float knockback)
    {
        if (currentState == State.Dying || invulnerable) return;

        hp -= amount;
        if (hp <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(HurtRoutine(sourcePosition, knockback <= 0 ? knockbackForce : knockback));
    }

    private IEnumerator HurtRoutine(Vector2 source, float kb)
    {
        invulnerable = true;
        SwitchState(State.Hurt);

        // Anima��o de dano
        anim.ResetTrigger(HashHurt);
        anim.SetTrigger(HashHurt);

        // Knockback
        Vector2 dir = ((Vector2)transform.position - source).normalized;
        Vector2 impulse = new Vector2(Mathf.Sign(dir.x), 0.6f).normalized * kb;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(impulse, ForceMode2D.Impulse);

        yield return new WaitForSeconds(hurtStaggerTime);

        invulnerable = false;

        // Decide pr�ximo estado de forma inteligente
        if (PlayerInSight(detectionRadius))
            SwitchState(State.Chasing);
        else
            SwitchState(State.Patrolling);
    }

    private void Die()
    {
        if (currentState == State.Dying) return;

        currentState = State.Dying;
        anim.ResetTrigger(HashDie);
        anim.SetTrigger(HashDie);

        // Desarma colis�o ativa e movimento
        rb.linearVelocity = Vector2.zero;
        col.enabled = false;

        // Opcional: desativar qualquer hitbox filha
        foreach (var c in GetComponentsInChildren<Collider2D>())
        {
            if (c != col) c.enabled = false;
        }

        // Limpa ap�s delay
        StartCoroutine(DeathCleanup());
    }

    private IEnumerator DeathCleanup()
    {
        yield return new WaitForSeconds(deathCleanupDelay);
        gameObject.SetActive(false); // ou Destroy(gameObject);
    }

    // ======================= Contato (touch damage opcional) =======================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (touchDamage <= 0) return;
        if (((1 << collision.gameObject.layer) & playerMask.value) != 0)
        {
            // Se quiser, chame algo no player aqui (ex.: IDamageable do player).
            var idmg = collision.gameObject.GetComponentInParent<IDamageable>();
            if (idmg != null)
            {
                idmg.ApplyDamage(touchDamage, transform.position, knockbackForce * 0.7f);
            }
        }
    }

    // ======================= Gizmos =======================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, keepAwayDistance);

        if (hasWaypoints)
        {
            Gizmos.color = Color.green;
            if (leftPoint) Gizmos.DrawSphere(leftPoint.position, 0.07f);
            if (rightPoint) Gizmos.DrawSphere(rightPoint.position, 0.07f);
            if (leftPoint && rightPoint) Gizmos.DrawLine(leftPoint.position, rightPoint.position);
        }
        else
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(spawnPos == Vector3.zero ? transform.position : spawnPos, patrolRadius);
        }
    }
}

/// <summary>
/// Interface simples para receber dano (usada pelo PlayerHitbox para atingir o inimigo).
/// </summary>
public interface IDamageable
{
    void ApplyDamage(int amount, Vector2 sourcePosition, float knockback);
}
