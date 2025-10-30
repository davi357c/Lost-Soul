using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;

    void Start()
    {
        string lastSpawn = PlayerPrefs.GetString("LastSpawnPoint", "");
        Debug.Log("Último spawn salvo: " + lastSpawn);
        Debug.Log("Este spawner: " + gameObject.name);

        // Tenta achar o spawn salvo
        if (!string.IsNullOrEmpty(lastSpawn))
        {
            GameObject correctSpawner = GameObject.Find(lastSpawn);
            if (correctSpawner != null && correctSpawner != gameObject)
            {
                // Se existir outro spawner com esse nome, este aqui não faz nada
                return;
            }
        }

        // Se chegou até aqui, este spawner é o certo (ou padrão)
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        GameObject existingPlayer = GameObject.FindWithTag("Player");

        if (existingPlayer == null)
        {
            GameObject player = Instantiate(playerPrefab, transform.position, Quaternion.identity);

            var cameraFollow = FindObjectOfType<CameraFollow>();
            if (cameraFollow != null)
                cameraFollow.SetTarget(player.transform);
        }
        else
        {
            existingPlayer.transform.position = transform.position;

            var cameraFollow = FindObjectOfType<CameraFollow>();
            if (cameraFollow != null)
                cameraFollow.SetTarget(existingPlayer.transform);
        }
    }
}
