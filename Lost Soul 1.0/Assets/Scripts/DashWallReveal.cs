using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapRenderer))]
[RequireComponent(typeof(TilemapCollider2D))]
public class DashWallReveal: MonoBehaviour
{
    private Tilemap tilemap;
    private TilemapCollider2D tilemapCollider;
    private bool playerInside = false;

    [Header("Aparência da parede")]
    [Range(0f, 1f)] public float visibleAlpha = 1f; // fora da sala
    [Range(0f, 1f)] public float hiddenAlpha = 0.1f; // dentro da sala

    void Start()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapCollider = GetComponent<TilemapCollider2D>();
        SetAlpha(visibleAlpha);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null && player.IsDashing())
        {
            // Player atravessou a parede durante o dash
            playerInside = true;
            SetAlpha(hiddenAlpha);

            // Desativa o collider para não prender o player
            tilemapCollider.enabled = false;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            // Saiu da sala — parede volta ao normal
            playerInside = false;
            SetAlpha(visibleAlpha);

            // Reativa o collider
            tilemapCollider.enabled = true;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (tilemap != null)
        {
            Color c = tilemap.color;
            c.a = alpha;
            tilemap.color = c;
        }
    }
}
