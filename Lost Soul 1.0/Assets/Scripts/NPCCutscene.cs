using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // ✅ necessário para mudar de cena

public class NPCCutscene : MonoBehaviour
{
    [Header("Referências do NPC")]
    public Transform npcTransform;
    public Animator npcAnimator;
    public Rigidbody2D npcRb;
    public Transform walkEndPoint;
    public Transform fallTarget;
    public AudioClip soundEffect;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Movimento")]
    public float walkSpeed = 2f;
    public float gravityScale = 2f;

    [Header("Fade")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.15f;
    public float fadeHoldTime = 0.4f;

    private CameraFollow cameraFollow;
    private AudioSource audioSource;

    private void Start()
    {
        GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (camObj != null)
            cameraFollow = camObj.GetComponent<CameraFollow>();

        audioSource = npcTransform.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = npcTransform.gameObject.AddComponent<AudioSource>();

        if (npcRb == null)
            npcRb = npcTransform.GetComponent<Rigidbody2D>();

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0f;
    }

    public void StartCutscene(Transform player)
    {
        StartCoroutine(CutsceneRoutine(player));
    }

    private IEnumerator CutsceneRoutine(Transform player)
    {
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (cameraFollow != null)
            cameraFollow.SetTarget(npcTransform);

        if (npcAnimator != null) npcAnimator.SetTrigger("Walk");
        if (soundEffect != null && audioSource != null)
            audioSource.PlayOneShot(soundEffect, soundVolume);

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

        if (npcRb != null)
        {
            npcRb.gravityScale = gravityScale;
            npcRb.linearVelocity = new Vector2(npcRb.linearVelocity.x, 0f);

            if (npcAnimator != null)
            {
                Debug.Log("Ativando animação de FALL...");
                npcAnimator.ResetTrigger("Walk");
                npcAnimator.SetTrigger("Fall");
            }
        }

        while (npcTransform.position.y > fallTarget.position.y)
        {
            if (cameraFollow != null)
            {
                Vector3 targetPos = npcTransform.position + new Vector3(0f, 0f, -10f);
                cameraFollow.transform.position = Vector3.Lerp(cameraFollow.transform.position, targetPos, Time.deltaTime * 5f);
            }

            yield return null;
        }

        // 🔹 FADE pra preto
        yield return StartCoroutine(FadeRoutine(0f, 1f, fadeDuration, fadeHoldTime));

        // 🔸 Quando a tela estiver totalmente preta, troca pra cena do menu
        SceneManager.LoadScene("MenuScene");
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

        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);
    }

    public void StartCutsceneNoArgs()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            StartCutscene(player);
        else
            Debug.LogWarning("Player não encontrado para StartCutsceneNoArgs");
    }
}
