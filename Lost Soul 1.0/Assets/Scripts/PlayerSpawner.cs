using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;      // Arraste o prefab do player aqui
    public Transform cameraTargetPoint;  // Um Empty que a câmera já está seguindo

    void Start()
    {
        // Instancia o player no spawn
        GameObject player = Instantiate(playerPrefab, transform.position, Quaternion.identity);

        // Move o placeholder da câmera para o player
        if (cameraTargetPoint != null)
        {
            cameraTargetPoint.position = player.transform.position;

            // Faz o placeholder seguir o player
            cameraTargetPoint.parent = player.transform;
        }
    }
}
