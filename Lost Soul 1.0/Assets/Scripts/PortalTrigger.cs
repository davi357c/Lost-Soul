using UnityEngine;

public class PortalTrigger : MonoBehaviour
{
    public string sceneToLoad; // nome da pr�xima cena (ex: "Fase2")

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Inicia a transi��o para a nova cena
            SceneTransition.instance.FadeToScene(sceneToLoad);
        }
    }
}
