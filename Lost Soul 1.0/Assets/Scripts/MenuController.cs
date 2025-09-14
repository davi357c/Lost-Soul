using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void IniciarJogo()
    {
        SceneManager.LoadScene("TutorialScene"); 
    }

    public void AbrirOpcoes()
    {
        Debug.Log("Abrindo Op��es...");
    }

    public void SairDoJogo()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; 
#endif
    }
}
