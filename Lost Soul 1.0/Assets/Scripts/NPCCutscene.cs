using UnityEngine;
using System.Collections;

public class NPCCutscene : MonoBehaviour
{
    [Header("Referências do NPC")]
    public Transform npcTransform;      // NPC que vai se mover
    public Animator npcAnimator;        // Animator do NPC
    public Rigidbody2D npcRb;
    public Transform walkEndPoint;      // Ponto final do andar
    public Transform fallTarget;        // Altura de referência da queda
    public AudioClip soundEffect;       // Som opcional (ex: rugido)
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Movimento")]
    public float walkSpeed = 2f;        // velocidade horizontal
    public float gravityScale = 2f;     // aceleração vertical

    [Header("Fade")]
    public CanvasGroup fadeCanvasGroup; // CanvasGroup de um painel preto na tela
    public float fadeDuration = 0.15f;  // Duração do fade (rápido)
    public float fadeHoldTime = 0.4f;   // Quanto tempo fica totalmente preto

    private CameraFollow cameraFollow;
    private AudioSource audioSource;

    private void Start()
    {
        // Procura a câmera automaticamente
        GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (camObj != null)
            cameraFollow = camObj.GetComponent<CameraFollow>();

        // AudioSource
        audioSource = npcTransform.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = npcTransform.gameObject.AddComponent<AudioSource>();

        // Rigidbody2D
        if (npcRb == null)
            npcRb = npcTransform.GetComponent<Rigidbody2D>();

        // Garante que o fade começa transparente
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0f;
    }

    public void StartCutscene(Transform player)
    {
        StartCoroutine(CutsceneRoutine(player));
    }

    private IEnumerator CutsceneRoutine(Transform player)
    {
        // 1️⃣ Trava o player
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.enabled = false;

        // 2️⃣ Câmera já muda pro NPC SEM fade
        if (cameraFollow != null)
            cameraFollow.SetTarget(npcTransform);

        // 3️⃣ Toca animação de andar e som
        if (npcAnimator != null) npcAnimator.SetTrigger("Walk");
        if (soundEffect != null && audioSource != null)
            audioSource.PlayOneShot(soundEffect, soundVolume);

        // 4️⃣ ANDAR horizontal
        while (npcTransform.position.x < walkEndPoint.position.x)
        {
            npcTransform.position += new Vector3(walkSpeed * Time.deltaTime, 0f, 0f);

            if (cameraFollow != null)
            {
                Vector3 targetPos = npcTransform.position + new Vector3(0f, 0f, -10f);
                cameraFollow.transform.position = Vector3.Lerp(cameraFollow.transform.position, targetPos, Time.deltaTime * 5f);
            }

            yield return null;
        }

        // 5️⃣ COMEÇA a cair (agora vai atravessar o fallTarget)
        // 5️⃣ COMEÇA a cair (agora vai atravessar o fallTarget)
        if (npcRb != null)
        {
            npcRb.gravityScale = gravityScale;
            npcRb.linearVelocity = new Vector2(npcRb.linearVelocity.x, 0f);

            // 🔽 Troca para animação de queda
            if (npcAnimator != null)
            {
                Debug.Log("Ativando animação de FALL...");
                npcAnimator.ResetTrigger("Walk"); // garante que o Walk não trava a transição
                npcAnimator.SetTrigger("Fall");
            }
        }


        // Enquanto ele estiver ACIMA do fallTarget, a câmera acompanha
        while (npcTransform.position.y > fallTarget.position.y)
        {
            if (cameraFollow != null)
            {
                Vector3 targetPos = npcTransform.position + new Vector3(0f, 0f, -10f);
                cameraFollow.transform.position = Vector3.Lerp(cameraFollow.transform.position, targetPos, Time.deltaTime * 5f);
            }

            yield return null;
        }

        // 👉 A partir daqui ele JÁ ATRAVESSOU o fallTarget
        // Não travamos mais posição nem desligamos gravidade, ele continua caindo pra fora da cena

        // 6️⃣ FADE pra preto (rápido) + segura um pouquinho preto
        yield return StartCoroutine(FadeRoutine(0f, 1f, fadeDuration, fadeHoldTime));

        // 7️⃣ Câmera volta para o player enquanto está tudo preto
        if (cameraFollow != null)
            cameraFollow.SetTarget(player);

        // Se quiser, aqui você pode desativar o NPC pra não ficar caindo infinito:
        // npcTransform.gameObject.SetActive(false);

        // 8️⃣ Fade de volta pro jogo (preto -> cena)
        yield return StartCoroutine(FadeRoutine(1f, 0f, fadeDuration, 0f));

        // 9️⃣ Libera player
        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, float holdTime = 0f)
    {
        if (fadeCanvasGroup == null || duration <= 0f)
        {
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        fadeCanvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;

        // tempo que a tela fica parada toda preta
        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);
    }

    // dentro da classe NPCCutscene
    public void StartCutsceneNoArgs()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            StartCutscene(player);
        else
            Debug.LogWarning("Player não encontrado para StartCutsceneNoArgs");
    }

}