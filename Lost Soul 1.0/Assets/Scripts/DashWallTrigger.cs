using UnityEngine;
using UnityEngine.Tilemaps;

public class DashWallTrigger : MonoBehaviour
{
    public Tilemap invisibleWallTilemap;  // Tilemap das paredes que cobrem a sala
    public float fadeDuration = 0.5f;     // Tempo pra sumir a parede
    private bool hasActivated = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerMovement player = collision.GetComponent<PlayerMovement>();
        if (player != null && player.IsDashing() && !hasActivated)
        {
            hasActivated = true;
            StartCoroutine(FadeOutInvisibleWalls());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerMovement player = collision.GetComponent<PlayerMovement>();
        if (player != null)
        {
            invisibleWallTilemap.gameObject.SetActive(true);
            Color c = invisibleWallTilemap.color;
            c.a = 1f;
            invisibleWallTilemap.color = c;
            hasActivated = false;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        PlayerMovement player = collision.GetComponent<PlayerMovement>();
        if (player != null && player.IsDashing() && !hasActivated)
        {
            hasActivated = true;
            StartCoroutine(FadeOutInvisibleWalls());
        }
    }



    private System.Collections.IEnumerator FadeOutInvisibleWalls()
    {
        if (invisibleWallTilemap == null)
            yield break;

        // 🔹 Desativa todos os colliders imediatamente (inclusive Composite ou filhos)
        Collider2D[] colliders = invisibleWallTilemap.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
            col.enabled = false;

        // Continua com o fade normal
        Color startColor = invisibleWallTilemap.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            invisibleWallTilemap.color = Color.Lerp(startColor, targetColor, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        invisibleWallTilemap.color = targetColor;

        // Opcional: desativa o tilemap todo depois
        invisibleWallTilemap.gameObject.SetActive(false);
    }


}
