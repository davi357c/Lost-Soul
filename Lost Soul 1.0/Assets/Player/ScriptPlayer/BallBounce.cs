using UnityEngine;

public class BallBounce : MonoBehaviour
{
    [Header("Knockback (default)")]
    public float defaultKnockbackForce = 10f;
    public float defaultKnockbackUpward = 4f;

    [Header("Bounce visual")]
    public float bounceHeight = 0.25f;
    public float bounceSpeed = 3f;
    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
    }

    // M�todo p�blico � chama quando o player ACERTA (hit) a bola.
    // hitterTransform: transform do player que acertou
    // force: for�a de knockback horizontal (multiplicador)
    // upward: for�a vertical extra
    public void ReceiveHit(Transform hitterTransform, float force, float upward)
    {
        if (hitterTransform == null) return;

        Rigidbody2D rb = hitterTransform.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // empurra o jogador para longe da bola
        Vector2 dir = (hitterTransform.position - transform.position).normalized;

        rb.linearVelocity = Vector2.zero; // limpa velocidade atual antes do impulso
        Vector2 final = dir * force + Vector2.up * upward;
        rb.AddForce(final, ForceMode2D.Impulse);

        // opcional: aqui voc� pode adicionar som/part�culas
    }

    // vers�o de ajuda que usa os valores default do inspector
    public void ReceiveHit(Transform hitterTransform)
    {
        ReceiveHit(hitterTransform, defaultKnockbackForce, defaultKnockbackUpward);
    }
}
