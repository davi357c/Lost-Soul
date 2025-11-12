using UnityEngine;
using System.Collections;

public class BossAI : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;                 // Arrasta o Player aqui ou ele acha pelo Tag
    public string playerTag = "Player";      // Tag do Player
    public string playerLayerName = "Player";// Layer do Player

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private int playerLayer;

    [Header("Sprite / Direção")]
    [Tooltip("Marque se o sprite padrão está olhando para a DIREITA. Desmarque se ele foi desenhado olhando para a ESQUERDA.")]
    public bool spriteFacesRight = true;

    [Header("Detecção / Acordar")]
    public float wakeUpDistance = 8f;        // Distância para acordar
    public float awakeAnimDuration = 1.5f;   // Usado só como fallback se não tiver Animator

    private bool isAwake = false;
    private bool isDead = false;

    [Header("Movimento / Idle inteligente (voando)")]
    public float moveSpeed = 4f;
    public float idleOffsetRange = 3f;       // Raio em torno do player para onde o boss tenta ir
    public float idleRetargetTime = 1.5f;    // Tempo para trocar de alvo de voo
    public float hoverHeightOffset = 1.5f;   // Altura média acima do player

    private Vector2 idleTarget;
    private float idleTimer;
    private Vector2 desiredVelocity = Vector2.zero; // direção * velocidade (unidades/segundo)

    // Limite mínimo de Y (posição inicial)
    private float minY;

    [Header("Alcances de Ataque")]
    public float meleeRange = 2f;
    public float meleeVerticalTolerance = 1.5f;
    public float rangedMinRange = 3f;
    public float rangedMaxRange = 9f;
    public float laserMinRange = 6f;

    [Header("Tempos de Ataque (lock de movimento)")]
    public float timeBetweenAttacks = 0.8f;
    public float meleeLockTime = 0.6f;
    public float rangedLockTime = 0.9f;
    public float laserLockTime = 1.3f;

    [Header("Fase Imune")]
    public int attacksBeforeImmune = 4;      // Depois de quantos ataques entra em boss_immune
    public float immuneDuration = 3f;        // Tempo em boss_immune

    private bool isImmune = false;           // estado lógico de imunidade
    private int attacksCount = 0;
    private bool isPerformingAction = false; // está em ataque/laser/immune/awake
    private float attackCooldown = 0f;

    [Header("Laser")]
    public GameObject laserHitbox;           // Objeto com o collider do laser (desativado por padrão)

    [Header("Câmera")]
    public CameraFollow cameraFollow;        // arrasta aqui ou ele acha sozinho
    private Transform previousCameraTarget;  // pra devolver a câmera depois do awake

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();

        if (!string.IsNullOrEmpty(playerLayerName))
            playerLayer = LayerMask.NameToLayer(playerLayerName);
        else
            playerLayer = -1;

        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        desiredVelocity = Vector2.zero;

        // Guarda o Y inicial como limite mínimo
        minY = transform.position.y;

        // Como o boss está voando, garantimos que a gravidade não puxe ele para o chão
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        // Tenta achar a câmera automaticamente
        if (cameraFollow == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                cameraFollow = cam.GetComponent<CameraFollow>();
        }
    }

    private void Update()
    {
        if (isDead)
        {
            desiredVelocity = Vector2.zero;
            return;
        }

        if (!isAwake)
        {
            CheckWakeUp();
            return;
        }

        attackCooldown -= Time.deltaTime;

        if (!isPerformingAction && !isImmune)
        {
            // Voo em volta do player
            HandleIdleMovement();

            if (attackCooldown <= 0f && player != null)
            {
                DecideNextAction();
            }
        }
        else
        {
            desiredVelocity = Vector2.zero;
        }

        // sempre olhar pro player enquanto estiver acordado e vivo
        if (isAwake && !isDead && player != null)
        {
            FacePlayer();
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            // MovePosition usa a posição atual + velocidade * deltaTime
            Vector2 newPos = rb.position + desiredVelocity * Time.fixedDeltaTime;

            // NÃO deixa ir abaixo da posição inicial
            if (newPos.y < minY)
                newPos.y = minY;

            rb.MovePosition(newPos);
        }
        else
        {
            Vector3 newPos = transform.position + (Vector3)(desiredVelocity * Time.fixedDeltaTime);

            if (newPos.y < minY)
                newPos.y = minY;

            transform.position = newPos;
        }
    }

    // --------- ACORDAR ---------

    private void CheckWakeUp()
    {
        if (player == null) return;

        if (playerLayer != -1 && player.gameObject.layer != playerLayer)
            return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= wakeUpDistance)
        {
            StartCoroutine(WakeUpRoutine());
        }
        else
        {
            desiredVelocity = Vector2.zero;
        }
    }

    private IEnumerator WakeUpRoutine()
    {
        if (isAwake) yield break;

        isAwake = true;
        isPerformingAction = true;   // <<< trava movimento ENQUANTO estiver no awake
        desiredVelocity = Vector2.zero;

        // *** NOVO: foca a câmera no boss durante o awake ***
        if (cameraFollow != null)
        {
            // guardamos o target atual (normalmente o player)
            previousCameraTarget = player;
            cameraFollow.SetTarget(transform);
        }

        if (anim != null)
        {
            anim.SetTrigger("awake"); // Trigger -> estado boss_awake

            // Espera um frame para o Animator processar o trigger
            yield return null;

            // Espera ENTRAR no estado "boss_awake" (com um timeout curto de segurança)
            float waitEnter = 0f;
            while (!anim.GetCurrentAnimatorStateInfo(0).IsName("boss_awake") && waitEnter < 0.5f)
            {
                waitEnter += Time.deltaTime;
                yield return null;
            }

            // Agora espera SAIR do estado "boss_awake"
            while (anim.GetCurrentAnimatorStateInfo(0).IsName("boss_awake"))
            {
                yield return null;
            }
        }
        else
        {
            // Fallback caso não tenha Animator configurado
            yield return new WaitForSeconds(awakeAnimDuration);
        }

        // *** NOVO: devolve a câmera pro player depois do awake ***
        if (cameraFollow != null)
        {
            if (previousCameraTarget != null)
                cameraFollow.SetTarget(previousCameraTarget);
            else if (player != null)
                cameraFollow.SetTarget(player);
        }

        isPerformingAction = false;
        attackCooldown = timeBetweenAttacks;
        // No Animator, deixe boss_awake voltar pra boss_idle por Exit Time
    }

    // --------- IDLE / VOO INTELIGENTE ---------

    private void HandleIdleMovement()
    {
        if (player == null)
        {
            desiredVelocity = Vector2.zero;
            return;
        }

        idleTimer -= Time.deltaTime;

        // De tempos em tempos escolhemos um novo ponto de voo em torno do player
        if (idleTimer <= 0f || Vector2.Distance(transform.position, idleTarget) > idleOffsetRange * 1.5f)
        {
            // Direção aleatória em um círculo
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            if (randomDir == Vector2.zero)
                randomDir = Vector2.right;

            // Distância moderada para não ir longe demais
            float randomDist = Random.Range(idleOffsetRange * 0.4f, idleOffsetRange);

            Vector2 offset = randomDir * randomDist;

            // Puxa um pouco pra cima do player
            offset.y += hoverHeightOffset;

            Vector2 candidate = (Vector2)player.position + offset;

            // Garante que o alvo nunca fique abaixo da posição inicial do boss
            if (candidate.y < minY)
                candidate.y = minY;

            idleTarget = candidate;
            idleTimer = idleRetargetTime;
        }

        Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 toTarget = idleTarget - currentPos;
        float distance = toTarget.magnitude;

        if (distance < 0.2f)
        {
            // Próximo do alvo, pode flutuar quase parado
            desiredVelocity = Vector2.zero;
        }
        else
        {
            Vector2 dir = toTarget / distance;
            desiredVelocity = dir * moveSpeed;

            // Agora quem manda é sempre o FacePlayer() chamado no Update()
        }
    }

    // --------- ESCOLHA DO PRÓXIMO ATAQUE ---------

    private void DecideNextAction()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        float verticalDiff = Mathf.Abs(player.position.y - transform.position.y);

        // Primeiro: checa se já é hora de entrar no modo imune
        if (!isImmune && attacksCount >= attacksBeforeImmune)
        {
            StartCoroutine(ImmuneRoutine());
            return;
        }

        // Muito perto → melee (precisa estar relativamente alinhado na vertical)
        if (dist <= meleeRange && verticalDiff <= meleeVerticalTolerance)
        {
            StartCoroutine(MeleeRoutine());
            return;
        }

        // Distância média → melee ou range
        if (dist > meleeRange && dist < laserMinRange)
        {
            float r = Random.value;
            if (r < 0.6f)
                StartCoroutine(RangedRoutine());
            else
                StartCoroutine(MeleeRoutine());
            return;
        }

        // Distância grande → laser ou range
        if (dist >= laserMinRange)
        {
            float r = Random.value;
            if (r < 0.7f)
                StartCoroutine(LaserRoutine());
            else
                StartCoroutine(RangedRoutine());
            return;
        }
    }

    // --------- MELEE ---------

    private IEnumerator MeleeRoutine()
    {
        isPerformingAction = true;
        attacksCount++;
        attackCooldown = timeBetweenAttacks + meleeLockTime;

        FacePlayer();
        desiredVelocity = Vector2.zero;

        if (anim != null)
            anim.SetTrigger("melee"); // Trigger -> estado boss_melee

        yield return new WaitForSeconds(meleeLockTime);

        isPerformingAction = false;
    }

    // --------- RANGE (PROJÉTEIS) ---------

    private IEnumerator RangedRoutine()
    {
        isPerformingAction = true;
        attacksCount++;
        attackCooldown = timeBetweenAttacks + rangedLockTime;

        FacePlayer();
        desiredVelocity = Vector2.zero;

        if (anim != null)
            anim.SetTrigger("rangeAtt"); // Trigger -> estado boss_rangeAttack (param chama rangeAtt)

        // Coloque um Animation Event na animação chamando um método SpawnProjectile()

        yield return new WaitForSeconds(rangedLockTime);

        isPerformingAction = false;
    }

    // --------- LASER ---------

    private IEnumerator LaserRoutine()
    {
        isPerformingAction = true;
        attacksCount++;
        attackCooldown = timeBetweenAttacks + laserLockTime;

        FacePlayer();
        desiredVelocity = Vector2.zero;

        if (anim != null)
            anim.SetTrigger("laser"); // Trigger -> estado boss_laser

        // Use Animation Events para StartLaserHitbox / StopLaserHitbox

        yield return new WaitForSeconds(laserLockTime);

        isPerformingAction = false;
    }

    // Chamadas por Animation Event na animação do laser
    public void StartLaserHitbox()
    {
        if (laserHitbox != null)
            laserHitbox.SetActive(true);
    }

    public void StopLaserHitbox()
    {
        if (laserHitbox != null)
            laserHitbox.SetActive(false);
    }

    // --------- IMUNE (BOOL NO ANIMATOR) ---------

    private IEnumerator ImmuneRoutine()
    {
        isImmune = true;
        isPerformingAction = true;
        desiredVelocity = Vector2.zero;

        if (anim != null)
            anim.SetBool("immune", true);   // entra em boss_immune

        yield return new WaitForSeconds(immuneDuration);

        if (anim != null)
            anim.SetBool("immune", false);  // Animator trata a transição pra boss_leavingImmune / boss_idle

        isImmune = false;
        isPerformingAction = false;
        attacksCount = 0;
        attackCooldown = timeBetweenAttacks;
    }

    // --------- UTILIDADES ---------

    private void HandleFlip(Vector2 dir)
    {
        if (sr == null) return;

        if (dir.x > 0.05f)
        {
            sr.flipX = !spriteFacesRight;
        }
        else if (dir.x < -0.05f)
        {
            sr.flipX = spriteFacesRight;
        }
    }

    private void FacePlayer()
    {
        if (player == null || sr == null) return;

        float dx = player.position.x - transform.position.x;

        if (dx > 0.05f)
        {
            sr.flipX = !spriteFacesRight;
        }
        else if (dx < -0.05f)
        {
            sr.flipX = spriteFacesRight;
        }
    }

    // Chame isto pelo script de vida quando a vida chegar a 0
    public void Die()
    {
        if (isDead) return;

        isDead = true;
        isAwake = true;
        isPerformingAction = false;
        desiredVelocity = Vector2.zero;

        if (anim != null)
            anim.SetTrigger("die"); // Trigger -> estado boss_death
    }

    // Para o script de vida checar se o boss está imune
    public bool IsImmune
    {
        get { return isImmune; }
    }
}
