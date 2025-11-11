using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CutsceneEnd : MonoBehaviour
{
    [Header("Referências")]
    public Transform npc;
    public Transform stopPoint;
    public Image fadeImage;
    public GameObject mouseIconUI;
    public GameObject dialogueCanvas;
    public GameObject spaceIconUI; // 🔹 ÍCONE do botão de espaço
    public DialogueLine[] dialogueLines;

    [Header("Configuração da Cutscene")]
    public float triggerDistance = 3f;
    public float moveSpeed = 2f;
    public float closeCamSize = 3f;
    public float zoomDuration = 0.6f;
    public float fadeDuration = 2f;
    public float stopOffsetFromNpc = 1.5f;
    public float arriveEpsilon = 0.05f;
    public float moveTimeout = 10f;

    private Transform player;
    private Animator playerAnimator;
    private Animator npcAnimator;
    private DialogueManager dialogueManager;
    private MonoBehaviour playerMovementScript;

    private bool cutsceneStarted = false;
    private float originalCamSize;

    void Start()
    {
        FindPlayerAndComponents();
        npcAnimator = npc != null ? npc.GetComponent<Animator>() : null;
        dialogueManager = FindObjectOfType<DialogueManager>();

        if (mouseIconUI != null)
            mouseIconUI.SetActive(false);

        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);

        if (spaceIconUI != null)
            spaceIconUI.SetActive(false); // 🔹 começa desativado

        if (stopPoint == null)
            Debug.LogWarning("CutsceneEnd: stopPoint não atribuído.");
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
        Debug.Log("🎬 Cutscene iniciada!");

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
            playerAnimator.SetBool("isWalking", true);

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
            playerAnimator.SetBool("isWalking", false);

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

        while (!hasAttacked)
        {
            if (Input.GetKeyDown(KeyCode.Space) && Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;

                if (playerAnimator != null)
                    playerAnimator.SetTrigger("Attack");

                yield return new WaitForSeconds(0.5f);

                if (npcAnimator != null)
                    npcAnimator.SetTrigger("hit");

                hasAttacked = true;
            }

            yield return null;
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

        Debug.Log("🏁 Cutscene finalizada!");
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
