using UnityEngine;
using System.Collections;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("Referências do Boss")]
    public Transform bossTransform;
    public EnemyAI bossAI;
    public Animator bossAnimator;
    public float focusDuration = 2f; // tempo que a câmera fica no boss

    [Header("Som do Rugido")]
    public AudioSource audioSource; // onde o som vai tocar (no boss)
    public AudioClip roarClip;      // som do rugido

    private CameraFollow cameraFollow;
    private bool triggered = false;

    private void Start()
    {
        // Acha automaticamente a câmera
        cameraFollow = FindObjectOfType<CameraFollow>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggered) return;
        if (!collision.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(BossIntroSequence(collision.transform));
    }

    private IEnumerator BossIntroSequence(Transform player)
    {
        // Pega o movimento do player
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.enabled = false; // 🔒 trava movimento

        // Desativa IA do boss
        if (bossAI != null)
            bossAI.enabled = false;

        // Foca a câmera no boss
        if (cameraFollow != null)
            cameraFollow.SetTarget(bossTransform);

        // 🔊 Toca som de rugido (antes ou junto da animação)
        if (audioSource != null && roarClip != null)
            audioSource.PlayOneShot(roarClip);

        // Toca a animação de rugido
        if (bossAnimator != null)
            bossAnimator.SetTrigger("Roar");

        // Espera o tempo da animação
        yield return new WaitForSeconds(focusDuration);

        // Volta a câmera pro player
        if (cameraFollow != null)
            cameraFollow.SetTarget(player);

        // Reativa IA do boss
        if (bossAI != null)
            bossAI.enabled = true;

        // Libera o movimento do player
        if (playerMovement != null)
            playerMovement.enabled = true; // 🔓 destrava
    }
}
