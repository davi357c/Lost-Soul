using UnityEngine;

public class DontDestroy : MonoBehaviour
{
    private static GameObject[] persistentObjects = new GameObject[3];
    public int ObjectIndex;
    void Awake()
    {
        if(persistentObjects[ObjectIndex] == null)
        {
            persistentObjects[ObjectIndex] = gameObject;
            DontDestroyOnLoad(gameObject);
        }

        else if(persistentObjects[ObjectIndex] != null)
        {
            Destroy(gameObject);
        }


        
    }
}
