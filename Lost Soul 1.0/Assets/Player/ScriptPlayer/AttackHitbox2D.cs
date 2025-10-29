using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class AttackHitbox2D : MonoBehaviour
{
    [Header("Janela de dano")]
    [Tooltip("True enquanto a janela do ataque está aberta (mostra no Inspector).")]
    public bool windowActive = false;

    [Tooltip("Se > 0, fecha a janela automaticamente após esse tempo.")]
    public float windowSeconds = 0.15f;

    [Tooltip("Evita múltiplos hits no mesmo pad num único swing.")]
    public bool onePadPerSwing = true;

    private HashSet<LightPad> hitThisSwing = new HashSet<LightPad>();

    [Header("Filtro")]
    [Tooltip("Camadas que esta hitbox deve atingir (ex.: 'PuzzlePad').")]
    public LayerMask padLayer;

    [Tooltip("Exige que o alvo tenha componente LightPad.")]
    public bool requireLightPadComponent = true;

    private Collider2D col;

    void Reset()
    {
        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (!col)
            Debug.LogError("[AttackHitbox2D] Adicione um Collider2D (IsTrigger = true).");
    }

    // Abra a janela a partir da animação/entradas
    public void OpenWindow()
    {
        windowActive = true;
        hitThisSwing.Clear();
        if (windowSeconds > 0f) Invoke(nameof(CloseWindow), windowSeconds);
    }

    public void CloseWindow()
    {
        windowActive = false;
        CancelInvoke(nameof(CloseWindow));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!windowActive) return;
        if ((padLayer.value & (1 << other.gameObject.layer)) == 0) return;

        var pad = other.GetComponentInParent<LightPad>() ?? other.GetComponent<LightPad>();
        if (requireLightPadComponent && !pad) return;

        if (onePadPerSwing && pad && hitThisSwing.Contains(pad)) return;

        pad?.OnHitByPlayer(); // <-- aciona o clique lógico do pad
        if (pad) hitThisSwing.Add(pad);
    }

    // Segurança para frames lentos: repetir enquanto encostado
    void OnTriggerStay2D(Collider2D other) => OnTriggerEnter2D(other);
}
