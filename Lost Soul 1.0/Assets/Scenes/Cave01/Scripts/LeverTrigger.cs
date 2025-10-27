using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))] // exige collider no mesmo objeto
public class LeverTrigger : MonoBehaviour
{
    [Header("Interact")]
    public KeyCode interactKey = KeyCode.E;
    public bool oneUseOnly = true;
    public float delayToActivate = 0.35f;

    [Header("Animation (opcional)")]
    public Animator animator;          // arrasta no Inspector (pode ser filho)
    public string triggerParam = "Pull";

    [Header("Puzzle")]
    public PuzzleManager puzzleManager; // arrasta no Inspector

    [Header("Events")]
    public UnityEvent onPulled;

    bool _playerNearby;
    bool _activated;

    void Awake()
    {
        // NÃO cria nada. Só valida o mínimo e avisa.
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
            Debug.LogWarning($"[Lever] '{name}': Collider2D deve estar como Trigger pra detectar entrada do player.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        _playerNearby = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        _playerNearby = false;
    }

    void Update()
    {
        if (_playerNearby && !_activated && Input.GetKeyDown(interactKey))
            Pull();
    }

    public void Pull()
    {
        if (_activated && oneUseOnly) return;

        if (animator && !string.IsNullOrEmpty(triggerParam))
            animator.SetTrigger(triggerParam);

        onPulled?.Invoke();

        StartCoroutine(ActivateAfterDelay());

        if (oneUseOnly) _activated = true;
    }

    IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(delayToActivate);

        if (puzzleManager)
            puzzleManager.ActivatePuzzle();
        else
            Debug.LogWarning("[Lever] PuzzleManager não atribuído no Inspector.");
    }
}
