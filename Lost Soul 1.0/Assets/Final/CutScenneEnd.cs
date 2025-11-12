using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement; // 🔹 ADICIONADO

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
    public string nextSceneName = "MainScene"; // 🔹 ADICIONADO — nome da cena para carregar depois do fade

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
    public float warningDelay = 4f;

    [Header("Configuração do comportamento aleatório")]
    public float minIntervalBetween = 0.15f;
    public float maxIntervalBetween = 0.9f;
    public float flashInTime = 0.08f;
    public float visibleTime = 0.18f;
    public float flashOutTime = 0.10f;
    public float screenPadding = 60f;

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

        if (mouseIconUI != null)
            mouseIconUI.SetActive(true);

        while (!Input.GetMouseButtonDown(0))
            yield return null;

        if (mouseIconUI != null)
            mouseIconUI.SetActive(false);

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

            if (waitTime >= warningDelay && warningRoutine == null && warningText != null)
            {
                warningRoutine = StartCoroutine(FlashWarningTextRandom());
            }

            if (Input.GetKeyDown(KeyCode.Space) && Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;

                if (playerAnimator != null)
                    playerAnimator.SetTrigger("Attack");

                yield return new WaitForSeconds(0.2f);
                StartCoroutine(HandleNPCHitAndDeath());
                hasAttacked = true;
            }

            yield return null;
        }

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

            // 🔹 ADICIONADO: muda para a próxima cena depois do fade
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
                yield break;
            }
        }

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

    IEnumerator FlashWarningTextRandom()
    {
        if (warningText == null || warningPhrases.Length == 0)
            yield break;

        if (warningArea == null)
        {
            Canvas c = FindObjectOfType<Canvas>();
            if (c != null) warningArea = c.GetComponent<RectTransform>();
            if (warningArea == null) yield break;
        }

        warningText.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        RectTransform txtRT = warningText.rectTransform;
        Rect parentRect = warningArea.rect;

        while (true)
        {
            string phrase = warningPhrases[Random.Range(0, warningPhrases.Length)];
            warningText.text = phrase;

            float halfW = parentRect.width * 0.5f - screenPadding;
            float halfH = parentRect.height * 0.5f - screenPadding;
            if (halfW < 10f) halfW = parentRect.width * 0.5f * 0.9f;
            if (halfH < 10f) halfH = parentRect.height * 0.5f * 0.9f;

            Vector2 anchoredPos = new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));
            txtRT.anchoredPosition = anchoredPos;
            Vector2 baseAnchored = anchoredPos;

            float shakeMagnitude = 10f;
            float shakeFrequency = 35f;

            float t = 0f;
            Color baseC = warningText.color;
            while (t < flashInTime)
            {
                float a = Mathf.Lerp(0f, 1f, t / flashInTime);
                warningText.color = new Color(baseC.r, baseC.g, baseC.b, a);
                Vector2 jitter = Random.insideUnitCircle * (shakeMagnitude * 0.9f);
                txtRT.anchoredPosition = baseAnchored + jitter;
                t += Time.deltaTime;
                yield return null;
            }

            warningText.color = new Color(baseC.r, baseC.g, baseC.b, 1f);
            txtRT.anchoredPosition = baseAnchored;

            float stayT = 0f;
            while (stayT < visibleTime)
            {
                Vector2 jitter = Random.insideUnitCircle * (shakeMagnitude * 0.6f);
                txtRT.anchoredPosition = baseAnchored + jitter;
                stayT += Time.deltaTime;
                yield return null;
            }

            t = 0f;
            while (t < flashOutTime)
            {
                float a = Mathf.Lerp(1f, 0f, t / flashOutTime);
                warningText.color = new Color(baseC.r, baseC.g, baseC.b, a);
                t += Time.deltaTime;
                yield return null;
            }

            warningText.color = new Color(baseC.r, baseC.g, baseC.b, 0f);
            txtRT.anchoredPosition = baseAnchored;

            yield return new WaitForSeconds(Random.Range(minIntervalBetween, maxIntervalBetween));
        }
    }

    IEnumerator HandleNPCHitAndDeath()
    {
        if (npc == null || npcDead) yield break;
        Vector2 dir = (npc.position - player.position);
        dir.Normalize();

        Rigidbody2D npcRb = npc.GetComponent<Rigidbody2D>();
        if (npcRb != null)
        {
            RigidbodyType2D savedType = npcRb.bodyType;
            bool savedSimulated = npcRb.simulated;

            if (npcRb.bodyType != RigidbodyType2D.Dynamic)
                npcRb.bodyType = RigidbodyType2D.Dynamic;

            Vector2 impulse = new Vector2(dir.x, dir.y * (1f + knockbackVerticalFactor)).normalized * knockbackForce;
            npcRb.AddForce(impulse, ForceMode2D.Impulse);
            yield return new WaitForSeconds(knockbackDuration);

            if (knockbackDisablePhysicsAfter)
            {
                npcRb.linearVelocity = Vector2.zero;
                npcRb.simulated = false;
                npcRb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                npcRb.linearVelocity = Vector2.zero;
                npcRb.bodyType = savedType;
                npcRb.simulated = savedSimulated;
            }
        }
        else
        {
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

        yield return new WaitForSeconds(0.05f);

        if (npcAnimator != null)
            npcAnimator.SetTrigger("Death");

        if (!npcDead)
        {
            npcDead = true;
            StartCoroutine(PlayNPCDeathRotation());
        }
    }

    IEnumerator PlayNPCDeathRotation()
    {
        if (npc == null) yield break;
        if (deathRotateDelay > 0f)
            yield return new WaitForSeconds(deathRotateDelay);

        float rollDuration = Mathf.Max(0.5f, deathRotateDuration * 1.8f);
        float rollDir = 1f;
        if (player != null)
        {
            rollDir = Mathf.Sign(npc.position.x - player.position.x);
            if (rollDir == 0f) rollDir = 1f;
        }

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
