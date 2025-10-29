using UnityEngine;

public class PersistentEventSystem : MonoBehaviour
{
    private static PersistentEventSystem instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // mantém o EventSystem entre cenas
        }
        else
        {
            Destroy(gameObject); // evita duplicar
        }
    }
}
