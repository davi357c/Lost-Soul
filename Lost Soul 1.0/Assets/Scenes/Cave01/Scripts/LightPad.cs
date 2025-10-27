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

    void Reset()
    {
        if (TryGetComponent(out Collider2D col)) col.isTrigger = false;
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>(true);
    }

    void Awake()
    {
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>(true);
        SetOff();
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

    /// Chamado pelo Player quando ele "bate" no pad
    public void OnHitByPlayer()
    {
        if (_interactable && _manager != null)
            _manager.OnPadClicked(this);
    }
}
