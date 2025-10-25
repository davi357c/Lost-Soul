using UnityEngine;
using System.Collections;

public class PlayerSpawnPoint : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(SetPlayerPosition());
    }

    private IEnumerator SetPlayerPosition()
    {
        // Espera até o player existir
        GameObject player = null;
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null; // espera 1 frame
        }

        string lastSpawn = PlayerPrefs.GetString("LastSpawnPoint", "");

        if (lastSpawn == gameObject.name)
        {
            player.transform.position = transform.position;

            // Se o player tiver Rigidbody2D, zera a velocidade
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }
}
