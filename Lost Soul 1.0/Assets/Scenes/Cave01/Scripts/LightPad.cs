using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class LightPad : MonoBehaviour
{
    [Header("Identificação")]
    public int id;

    [Header("Sprites")]
    public Sprite spriteApagado;
    public Sprite spriteVerde;
    public Sprite spriteVermelho;

    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        SetApagado();
    }

    public void SetApagado()
    {
        if (_sr && spriteApagado) _sr.sprite = spriteApagado;
    }

    public void SetVermelho()
    {
        if (_sr && spriteVermelho) _sr.sprite = spriteVermelho;
    }

    public void SetVerde()
    {
        if (_sr && spriteVerde) _sr.sprite = spriteVerde;
    }

    public IEnumerator FlashVermelho(float duracao)
    {
        SetVermelho();
        yield return new WaitForSeconds(duracao);
        SetApagado();
    }

    public IEnumerator FlashVerde(float duracao)
    {
        SetVerde();
        yield return new WaitForSeconds(duracao);
        SetApagado();
    }
}
