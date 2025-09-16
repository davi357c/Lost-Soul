using UnityEngine;

public class PortalTrigger : MonoBehaviour
{
    public string sceneToLoad; // nome da próxima cena (ex: "Fase2")

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Inicia a transição para a nova cena
            SceneTransition.instance.FadeToScene(sceneToLoad);
        }
    }
}
