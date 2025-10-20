using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform cameraTargetPoint;

    void Start()
    {
        if (GameObject.FindWithTag("Player") == null)
        {
            GameObject player = Instantiate(playerPrefab, transform.position, Quaternion.identity);

            if (cameraTargetPoint != null)
            {
                cameraTargetPoint.position = player.transform.position;
                cameraTargetPoint.parent = player.transform;
            }
        }
    }
}
