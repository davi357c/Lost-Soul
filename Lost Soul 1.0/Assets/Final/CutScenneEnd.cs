using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class CutsceneEnd : MonoBehaviour
{
    [Header("Referências")]
    public Transform npc;
    public Transform stopPoint;
    public Image fadeImage;
    public GameObject mouseIconUI;
    public GameObject dialogueCanvas;
    public GameObject spaceIconUI;
    public DialogueLine[] dialogueLines;
    public TextMeshProUGUI warningText; // 🔹 Texto TMP que vai piscar
    public RectTransform warningArea;   // Área (Canvas/Panel) onde as mensagens podem aparecer

    [Header("Configuração da Cutscene")]
    public float triggerDistance = 3f;
    public float moveSpeed = 2f;
    public float closeCamSize = 3f;
    public float zoomDuration = 0.6f;
    public float fadeDuration = 2f;
    public float stopOffsetFromNpc = 1.5f;
    public float arriveEpsilon = 0.05f;
    public float moveTimeout = 10f;

    [Header("Configuração da morte (rotação)")]
    public float deathRotateAngle = -70f;
    public float deathRotateDuration = 0.7f;
    public float deathRotateDelay = 0.1f;

    [Header("Configuração do knockback")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.25f;
    public float knockbackVerticalFactor = 0.4f;
    public float knockbackDistance = 0.6f;
    public bool knockbackDisablePhysicsAfter = true;

    [Header("Frases de aviso (quando demora pra atacar)")]
    public string[] warningPhrases = {
        "Aperte ESPAÇO!",
        "Vamos, ataque logo!",
        "O que está esperando?",
        "Não hesite!"
    };
    public float warningDelay = 4f; // tempo antes de começar a piscar

    [Header("Configuração do comportamento aleatório")]
    public float minIntervalBetween = 0.15f; // tempo mínimo entre aparições
    public float maxIntervalBetween = 0.9f;  // tempo máximo entre aparições
    public float flashInTime = 0.08f;        // tempo de fade-in rápido
    public float visibleTime = 0.18f;        // tempo que a frase fica visível
    public float flashOutTime = 0.10f;       // tempo de fade-out
    public float screenPadding = 60f;        // padding em pixels das bordas

    private Transform player;
    private Animator playerAnimator;
    private Animator npcAnimator;
    private DialogueManager dialogueManager;
    private MonoBehaviour playerMovementScript;
    private bool cutsceneStarted = false;
    private float originalCamSize;
    private bool npcDead = false;

    void Start()
    {
        FindPlayerAndComponents();
        npcAnimator = npc != null ? npc.GetComponent<Animator>() : null;
        dialogueManager = FindObjectOfType<DialogueManager>();

        if (mouseIconUI != null) mouseIconUI.SetActive(false);
        if (dialogueCanvas != null) dialogueCanvas.SetActive(false);
        if (spaceIconUI != null) spaceIconUI.SetActive(false);
        if (warningText != null) warningText.gameObject.SetActive(false);

        // se warningArea não foi setado, tenta pegar o primeiro Canvas da cena
        if (warningText != null && warningArea == null)
        {
            Canvas c = FindObjectOfType<Canvas>();
            if (c != null)
                warningArea = c.GetComponent<RectTransform>();
        }
    }

    void FindPlayerAndComponents()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerAnimator = p.GetComponent<Animator>();
            playerMovementScript = p.GetComponent("PlayerMovement") as MonoBehaviour;
        }
    }

    void Update()
    {
        if (player == null)
            FindPlayerAndComponents();

        if (cutsceneStarted || player == null || npc == null)
            return;

        float distance = Vector2.Distance(player.position, npc.position);
        if (distance <= triggerDistance)
        {
            StartCoroutine(CutsceneSequence());
            cutsceneStarted = true;
        }
    }

    IEnumerator CutsceneSequence()
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        FlipPlayerToNPC();

        Camera cam = Camera.main;
        float elapsed = 0f;
        float startSize = cam != null ? cam.orthographicSize : 0f;
        if (cam != null)
        {
            while (elapsed < zoomDuration)
            {
                cam.orthographicSize = Mathf.Lerp(startSize, closeCamSize, elapsed / zoomDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            cam.orthographicSize = closeCamSize;
            originalCamSize = startSize;
        }

        float dirToNpc = Mathf.Sign(npc.position.x - player.position.x);
        if (dirToNpc == 0f) dirToNpc = 1f;

        float desiredStopX = npc.position.x - (stopOffsetFromNpc * dirToNpc);

        Vector3 targetWorld;
        if (stopPoint != null)
            targetWorld = new Vector3(stopPoint.position.x, player.position.y, player.position.z);
        else
            targetWorld = new Vector3(desiredStopX, player.position.y, player.position.z);

        if (player.position.x < npc.position.x)
            targetWorld.x = Mathf.Min(targetWorld.x, desiredStopX);
        else
            targetWorld.x = Mathf.Max(targetWorld.x, desiredStopX);

        float minX = Mathf.Min(player.position.x, desiredStopX);
        float maxX = Mathf.Max(player.position.x, desiredStopX);
        targetWorld.x = Mathf.Clamp(targetWorld.x, minX, maxX);

        if (playerAnimator != null)
            playerAnimator.SetFloat("Speed", moveSpeed);

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        bool restoredKinematic = false;
        RigidbodyType2D originalBodyType = RigidbodyType2D.Dynamic;
        bool originalSimulated = true;

        if (rb != null)
        {
            originalSimulated = rb.simulated;
            originalBodyType = rb.bodyType;
            try
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                restoredKinematic = true;
            }
            catch
            {
                rb.simulated = false;
                restoredKinematic = false;
            }
        }

        float startTime = Time.time;
        int safety = 0;

        while (Vector2.Distance(player.position, targetWorld) > arriveEpsilon)
        {
            player.position = Vector3.MoveTowards(player.position, targetWorld, moveSpeed * Time.deltaTime);

            safety++;
            if (safety > 5000 || Time.time - startTime > moveTimeout)
                break;

            yield return null;
        }

        player.position = targetWorld;

        if (rb != null)
        {
            if (restoredKinematic)
                rb.bodyType = originalBodyType;
            else
                rb.simulated = originalSimulated;
            rb.linearVelocity = Vector2.zero;
        }

        if (playerAnimator != null)
            playerAnimator.SetFloat("Speed", 0f);

        // 🔹 Mostra diálogo
        if (dialogueManager != null && dialogueLines != null && dialogueLines.Length > 0)
        {
            if (dialogueCanvas != null)
                dialogueCanvas.SetActive(true);

            dialogueManager.StartDialogue(dialogueLines);

            while (dialogueManager.IsDialogueActive())
                yield return null;

            if (dialogueCanvas != null)
                dialogueCanvas.SetActive(false);
        }

        // 🔹 Mostra ícone do mouse
        if (mouseIconUI != null)
            mouseIconUI.SetActive(true);

        while (!Input.GetMouseButtonDown(0))
            yield return null;

        if (mouseIconUI != null)
            mouseIconUI.SetActive(false);

        // 🔹 Mostra ícone de ESPAÇO para ataque
        if (spaceIconUI != null)
            spaceIconUI.SetActive(true);

        float attackCooldown = 0.6f;
        bool hasAttacked = false;
        float lastAttackTime = -999f;
        float waitTime = 0f;
        Coroutine warningRoutine = null;

        while (!hasAttacked)
        {
            waitTime += Time.deltaTime;

            // se passou do delay, começa o piscante aleatório
            if (waitTime >= warningDelay && warningRoutine == null && warningText != null)
            {
                warningRoutine = StartCoroutine(FlashWarningTextRandom());
            }

            if (Input.GetKeyDown(KeyCode.Space) && Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;

                if (playerAnimator != null)
                    playerAnimator.SetTrigger("Attack");

                // pequeno delay para sincronizar o impacto
                yield return new WaitForSeconds(0.2f);

                // aplica knockback e depois faz a morte + rotação
                StartCoroutine(HandleNPCHitAndDeath());

                hasAttacked = true;
            }

            yield return null;
        }

        // encerra o piscante
        if (warningRoutine != null)
        {
            StopCoroutine(warningRoutine);
            warningRoutine = null;
            if (warningText != null)
                warningText.gameObject.SetActive(false);
        }

        if (spaceIconUI != null)
            spaceIconUI.SetActive(false);

        // 🔹 Fade final
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            float t = 0f;
            float startA = c.a;
            while (t < fadeDuration)
            {
                c.a = Mathf.Lerp(startA, 1f, t / fadeDuration);
                fadeImage.color = c;
                t += Time.deltaTime;
                yield return null;
            }
            c.a = 1f;
            fadeImage.color = c;
        }

        // 🔹 Restaura zoom e player
        elapsed = 0f;
        if (cam != null)
        {
            while (elapsed < zoomDuration)
            {
                cam.orthographicSize = Mathf.Lerp(closeCamSize, originalCamSize, elapsed / zoomDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            cam.orthographicSize = originalCamSize;
        }

        if (playerMovementScript != null)
            playerMovementScript.enabled = true;
    }

    // nova versão: aparece em posições aleatórias dentro de warningArea,
    // cada aparição faz fade-in -> fica -> fade-out, depois pula pra nova posição aleatória
    // nova versão do coroutine: mesma funcionalidade anterior, com tremor nas palavras
    IEnumerator FlashWarningTextRandom()
    {
        if (warningText == null || warningPhrases.Length == 0)
            yield break;

        // garante warningArea válido
        if (warningArea == null)
        {
            Canvas c = FindObjectOfType<Canvas>();
            if (c != null) warningArea = c.GetComponent<RectTransform>();
            if (warningArea == null) yield break;
        }

        warningText.gameObject.SetActive(true);

        // Força atualização das dimensões
        Canvas.ForceUpdateCanvases();

        RectTransform txtRT = warningText.rectTransform;
        Rect parentRect = warningArea.rect;

        while (true)
        {
            // escolhe frase aleatória
            string phrase = warningPhrases[Random.Range(0, warningPhrases.Length)];
            warningText.text = phrase;

            // calcula posição aleatória dentro da área (com padding)
            float halfW = parentRect.width * 0.5f - screenPadding;
            float halfH = parentRect.height * 0.5f - screenPadding;
            if (halfW < 10f) halfW = parentRect.width * 0.5f * 0.9f;
            if (halfH < 10f) halfH = parentRect.height * 0.5f * 0.9f;

            Vector2 anchoredPos = new Vector2(
                Random.Range(-halfW, halfW),
                Random.Range(-halfH, halfH)
            );

            // base da posição sem shake (vamos aplicar offsets relativos a ela)
            txtRT.anchoredPosition = anchoredPos;
            Vector2 baseAnchored = anchoredPos;

            // parâmetros do tremor — ajuste aqui se quiser mais/menos tremor
            float shakeMagnitude = 10f;   // amplitude máxima do tremor em pixels
            float shakeFrequency = 35f;   // frequência do shake (quanto maior, mais rápido)

            // fade in com tremor
            float t = 0f;
            Color baseC = warningText.color;
            while (t < flashInTime)
            {
                float a = Mathf.Lerp(0f, 1f, t / Mathf.Max(0.0001f, flashInTime));
                // aplica alpha
                warningText.color = new Color(baseC.r, baseC.g, baseC.b, a);

                // aplica tremor — decresce um pouco enquanto faz fade-in
                float shakeT = 1f - (t / Mathf.Max(0.0001f, flashInTime)); // 1 -> 0
                Vector2 jitter = Random.insideUnitCircle * (shakeMagnitude * 0.9f * shakeT);
                // opcional: adicionar um oscilador rápido para sensação mais direcional
                float osc = Mathf.Sin(Time.time * shakeFrequency) * 0.5f;
                jitter += new Vector2(osc * 2f, -osc * 1.5f);

                txtRT.anchoredPosition = baseAnchored + jitter;

                t += Time.deltaTime;
                yield return null;
            }
            warningText.color = new Color(baseC.r, baseC.g, baseC.b, 1f);
            txtRT.anchoredPosition = baseAnchored;

            // fica visível — aplicar tremor regular enquanto estiver visível
            float stay = visibleTime;
            float stayT = 0f;
            while (stayT < stay)
            {
                // jitter contínuo (menos intenso que no início)
                Vector2 jitter = Random.insideUnitCircle * (shakeMagnitude * 0.6f);
                float osc = Mathf.Sin(Time.time * shakeFrequency * 0.8f) * 0.5f;
                jitter += new Vector2(osc * 1.5f, -osc * 1f);

                txtRT.anchoredPosition = baseAnchored + jitter;

                stayT += Time.deltaTime;
                yield return null;
            }

            // fade out com tremor decrescente
            t = 0f;
            while (t < flashOutTime)
            {
                float a = Mathf.Lerp(1f, 0f, t / Mathf.Max(0.0001f, flashOutTime));
                warningText.color = new Color(baseC.r, baseC.g, baseC.b, a);

                // tremor decrescendo
                float shakeT = 1f - (t / Mathf.Max(0.0001f, flashOutTime)); // 1 -> 0
                Vector2 jitter = Random.insideUnitCircle * (shakeMagnitude * 0.7f * shakeT);
                float osc = Mathf.Sin(Time.time * shakeFrequency * 0.6f) * 0.5f;
                jitter += new Vector2(osc * 1f, -osc * 0.7f);

                txtRT.anchoredPosition = baseAnchored + jitter;

                t += Time.deltaTime;
                yield return null;
            }

            // garante alpha zero e volta a posição base
            warningText.color = new Color(baseC.r, baseC.g, baseC.b, 0f);
            txtRT.anchoredPosition = baseAnchored;

            // espera um intervalo aleatório pequeno antes da próxima aparição
            float wait = Random.Range(minIntervalBetween, maxIntervalBetween);
            float w = 0f;
            while (w < wait)
            {
                w += Time.deltaTime;
                yield return null;
            }
        }
    }


    IEnumerator HandleNPCHitAndDeath()
    {
        if (npc == null || npcDead) yield break;

        // direção do player para NPC (do player para o npc)
        Vector2 dir = (npc.position - player.position);
        dir.Normalize();

        Rigidbody2D npcRb = npc.GetComponent<Rigidbody2D>();

        if (npcRb != null)
        {
            // salva estado
            RigidbodyType2D savedType = npcRb.bodyType;
            bool savedSimulated = npcRb.simulated;

            // garante que estamos em Dynamic para aplicar impulso
            if (npcRb.bodyType != RigidbodyType2D.Dynamic)
                npcRb.bodyType = RigidbodyType2D.Dynamic;

            Vector2 impulse = new Vector2(dir.x, dir.y * (1f + knockbackVerticalFactor)).normalized * knockbackForce;
            npcRb.AddForce(impulse, ForceMode2D.Impulse);

            // espera a duração do knockback
            yield return new WaitForSeconds(knockbackDuration);

            if (knockbackDisablePhysicsAfter)
            {
                // desativa física para evitar conflitos durante a animação de morte/rotação
                npcRb.linearVelocity = Vector2.zero;
                npcRb.simulated = false;
                npcRb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                // restaura estado (ou apenas zera velocidade)
                npcRb.linearVelocity = Vector2.zero;
                npcRb.bodyType = savedType;
                npcRb.simulated = savedSimulated;
            }
        }
        else
        {
            // movimento manual do knockback caso não exista Rigidbody2D
            Vector3 start = npc.position;
            Vector3 target = start + new Vector3(dir.x, knockbackVerticalFactor, 0f) * knockbackDistance;
            float t = 0f;
            while (t < knockbackDuration)
            {
                npc.position = Vector3.Lerp(start, target, t / knockbackDuration);
                t += Time.deltaTime;
                yield return null;
            }
            npc.position = target;
        }

        // dá um pequeno delay antes de disparar a animação de morte (ajuste se quiser)
        yield return new WaitForSeconds(0.05f);

        if (npcAnimator != null)
            npcAnimator.SetTrigger("Death");

        // inicia rotação de queda (uma vez)
        if (!npcDead)
        {
            npcDead = true;
            StartCoroutine(PlayNPCDeathRotation());
        }
    }

    IEnumerator PlayNPCDeathRotation()
    {
        if (npc == null) yield break;

        // espera opcional para sincronia com o início da animação
        if (deathRotateDelay > 0f)
            yield return new WaitForSeconds(deathRotateDelay);

        // duração do "rolar" (usa deathRotateDuration como base, mas estende um pouco)
        float rollDuration = Mathf.Max(0.5f, deathRotateDuration * 1.8f);

        // direção para rolar (para longe do player)
        float rollDir = 1f;
        if (player != null)
        {
            rollDir = Mathf.Sign(npc.position.x - player.position.x);
            if (rollDir == 0f) rollDir = 1f;
        }

        // parâmetros do "rolar"
        float spins = 2.5f;
        float rollHorizontalDistance = 2.0f;
        float rollVerticalDistance = 1.2f;
        float totalRotation = 360f * spins * rollDir;

        float startZ = npc.eulerAngles.z;
        Vector3 startPos = npc.position;
        Vector3 targetPos = startPos + new Vector3(rollHorizontalDistance * rollDir, -Mathf.Abs(rollVerticalDistance), 0f);

        float t = 0f;
        while (t < rollDuration)
        {
            float p = t / rollDuration;
            float eased = Mathf.SmoothStep(0f, 1f, p);

            float z = startZ + Mathf.Lerp(0f, totalRotation, eased);
            npc.eulerAngles = new Vector3(0f, 0f, z);
            npc.position = Vector3.Lerp(startPos, targetPos, eased);

            t += Time.deltaTime;
            yield return null;
        }

        npc.eulerAngles = new Vector3(0f, 0f, startZ + totalRotation);
        npc.position = targetPos;
    }

    void FlipPlayerToNPC()
    {
        if (player == null || npc == null) return;
        Vector3 ls = player.localScale;
        float dir = Mathf.Sign(npc.position.x - player.position.x);
        if (dir == 0) return;
        ls.x = Mathf.Abs(ls.x) * (dir > 0 ? 1f : -1f);
        player.localScale = ls;
    }
}
