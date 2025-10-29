using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class LightPad : MonoBehaviour
{
    [Tooltip("Ordem lógica deste pad (0..N).")]
    public int id;

    [Header("Visual")]
    public SpriteRenderer sr;
    public Sprite spriteOff;
    public Sprite spriteGreen;
    public Sprite spriteRed;

    bool _interactable;
    PuzzleManager _manager;

    // =========================
    //  H I T B O X   D O   P L A Y E R
    // =========================
    [Header("Player Hitbox")]
    [SerializeField] string playerHitboxLayer = "PlayerHitbox";

    [SerializeField, Tooltip("Evita múltiplos hits em swings muito longos ou múltiplos colliders.")]
    float hitCooldown = 0.20f; // ↑ um pouco mais seguro que 0.08

    int _playerHitboxLayerId = -1;
    float _lastHitTime = -999f;

    // Um hit por overlap:
    // Enquanto a hitbox estiver encostando, não contabiliza novos hits.
    bool _blockedUntilExit = false;
    int _touchCount = 0; // conta quantos colliders da hitbox estão sobrepondo

    void Reset()
    {
        if (TryGetComponent(out Collider2D col)) col.isTrigger = false; // pad = colisor sólido (não-trigger)
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>(true);
    }

    void Awake()
    {
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>(true);
        SetOff();

        _playerHitboxLayerId = LayerMask.NameToLayer(playerHitboxLayer);
        if (_playerHitboxLayerId < 0)
            Debug.LogWarning($"[LightPad] Layer '{playerHitboxLayer}' não existe. Crie em Project Settings > Tags and Layers.");
    }

    public void Bind(PuzzleManager m) => _manager = m;
    public void SetInteractable(bool v) => _interactable = v;

    public void SetOff()
    {
        if (sr && spriteOff) sr.sprite = spriteOff;
    }

    public void FlashGreen(float time = 0.2f)
    {
        StopAllCoroutines();
        StartCoroutine(FlashSprite(spriteGreen, time));
    }

    public void FlashRed(float time = 0.35f)
    {
        StopAllCoroutines();
        StartCoroutine(FlashSprite(spriteRed, time));
    }

    /// Exibição da sequência (sempre em vermelho).
    public void ShowRed(float time)
    {
        StopAllCoroutines();
        StartCoroutine(FlashSprite(spriteRed, time));
    }

    IEnumerator FlashSprite(Sprite s, float t)
    {
        if (!sr) yield break;
        var prev = sr.sprite;
        sr.sprite = s;
        yield return new WaitForSeconds(t);
        sr.sprite = spriteOff ? spriteOff : prev;
    }

    void OnMouseDown()
    {
        if (_interactable && _manager != null)
            _manager.OnPadClicked(this);
    }

    /// Chamado explicitamente (se preferir acionar por script externo)
    public void OnHitByPlayer()
    {
        if (_interactable && _manager != null)
            _manager.OnPadClicked(this);
    }

    // =========================
    //  D E T E C Ç Ã O   D A   H I T B O X
    // =========================
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_interactable) return;
        if (_playerHitboxLayerId < 0) return;
        if (other.gameObject.layer != _playerHitboxLayerId) return;

        // registramos que há sobreposição com a hitbox (pode ter múltiplos colliders)
        _touchCount++;

        // Já contabilizou um hit e ainda não saiu? segura.
        if (_blockedUntilExit) return;

        // anti-spam por tempo (backup)
        if (Time.time - _lastHitTime < hitCooldown) return;
        _lastHitTime = Time.time;

        // bloqueia novos hits até a hitbox sair completamente
        _blockedUntilExit = true;

        OnHitByPlayer();
    }

    // Enquanto encostado NÃO processamos nada aqui.
    // Isso evitava “hits fantasmas” ao abrir a rodada seguinte.
    void OnTriggerStay2D(Collider2D other) { /* intencionalmente vazio */ }

    void OnTriggerExit2D(Collider2D other)
    {
        if (_playerHitboxLayerId < 0) return;
        if (other.gameObject.layer != _playerHitboxLayerId) return;

        _touchCount = Mathf.Max(0, _touchCount - 1);
        if (_touchCount == 0)
        {
            // só libera quando TODOS os colliders da hitbox saírem
            _blockedUntilExit = false;
        }
    }
}
