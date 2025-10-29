using UnityEngine;

public class PlayerAttackHitController : MonoBehaviour
{
    [Header("Referências de origem do hit (arraste os 3 GameObjects)")]
    public Transform forwardHitOrigin;
    public Transform upHitOrigin;
    public Transform downHitOrigin;

    [Header("Área do hit (comum)")]
    public bool useBox = true; // true = OverlapBox, false = OverlapCircle
    public Vector2 hitBoxSize = new Vector2(1.2f, 0.8f);
    public float circleRadius = 0.6f;
    public LayerMask hitMask; // configure para incluir a Layer das bolas

    [Header("Forças (custom por direção)")]
    public float forwardForce = 12f;
    public float forwardUpward = 4f;
    public float upForce = 14f;
    public float upUpward = 6f;
    public float downForce = 10f;
    public float downUpward = 2f;

    [Header("Debug")]
    public bool drawDebug = false;
    public float debugDuration = 0.06f;

    // ---------- Métodos públicos (chamar por Animation Event no frame exato) ----------
    // Use estes nomes nos Animation Events do seu clip de ataque:
    public void PerformForwardHit_NoArgs() => PerformInstantHit(forwardHitOrigin, forwardForce, forwardUpward);
    public void PerformUpHit_NoArgs() => PerformInstantHit(upHitOrigin, upForce, upUpward);
    public void PerformDownHit_NoArgs() => PerformInstantHit(downHitOrigin, downForce, downUpward);

    // ---------- Implementação ----------
    private void PerformInstantHit(Transform origin, float force, float upward)
    {
        if (origin == null) return;

        Vector2 center = origin.position;
        Collider2D[] hits;

        if (useBox)
            hits = Physics2D.OverlapBoxAll(center, hitBoxSize, 0f, hitMask);
        else
            hits = Physics2D.OverlapCircleAll(center, circleRadius, hitMask);

        foreach (var c in hits)
        {
            if (c == null) continue;
            BallBounce ball = c.GetComponent<BallBounce>();
            if (ball != null)
            {
                // chama o método da bola para aplicar knockback no player (this.transform = player)
                ball.ReceiveHit(this.transform, force, upward);
            }
        }

        // visual debug (Editor)
        if (drawDebug)
        {
#if UNITY_EDITOR
            if (useBox)
            {
                Vector2 a = center + new Vector2(hitBoxSize.x / 2f, hitBoxSize.y / 2f);
                Vector2 b = center + new Vector2(hitBoxSize.x / 2f, -hitBoxSize.y / 2f);
                Vector2 c2 = center + new Vector2(-hitBoxSize.x / 2f, -hitBoxSize.y / 2f);
                Vector2 d = center + new Vector2(-hitBoxSize.x / 2f, hitBoxSize.y / 2f);
                Debug.DrawLine(a, b, Color.red, debugDuration);
                Debug.DrawLine(b, c2, Color.red, debugDuration);
                Debug.DrawLine(c2, d, Color.red, debugDuration);
                Debug.DrawLine(d, a, Color.red, debugDuration);
            }
            else
            {
                Debug.DrawLine(center + Vector2.up * circleRadius, center - Vector2.up * circleRadius, Color.red, debugDuration);
                Debug.DrawLine(center + Vector2.right * circleRadius, center - Vector2.right * circleRadius, Color.red, debugDuration);
            }
#endif
        }
    }

    // opcional: método público que aceita parâmetros (se preferir AnimationEvent com float)
    public void PerformForwardHit_WithParams(float unused) => PerformForwardHit_NoArgs();
}
