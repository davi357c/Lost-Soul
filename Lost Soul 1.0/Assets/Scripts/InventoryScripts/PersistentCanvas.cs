using UnityEngine;

public class PersistentCanvas : MonoBehaviour
{
    private static PersistentCanvas instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // mantém o Canvas e todos os filhos
        }
        else
        {
            Destroy(gameObject); // evita duplicatas
        }
    }
}
