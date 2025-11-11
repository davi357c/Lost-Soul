using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    void Start()
    {
        GameObject spawn = GameObject.Find("PlayerSpawn");
        if (spawn != null)
        {
            transform.position = spawn.transform.position;
        }
    }
}
