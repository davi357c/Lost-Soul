using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;

    void Start()
    {
        // Evita duplicar o player
        GameObject existingPlayer = GameObject.FindWithTag("Player");
        if (existingPlayer == null)
        {
            // Cria o novo player
            GameObject player = Instantiate(playerPrefab, transform.position, Quaternion.identity);

            // Conecta a câmera persistente ao novo player
            CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(player.transform);
            }
        }
        else
        {
            // Se já existe, só reposiciona no ponto certo
            existingPlayer.transform.position = transform.position;

            CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(existingPlayer.transform);
            }
        }
    }
}
