using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerNameText;
    public GameObject dialogueBox;

    private DialogueLine[] currentLines;
    private int currentIndex = 0;
    private bool isActive = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    public AudioClip voiceFemale;
    public AudioClip voiceMale;
    private AudioSource audioSource;


    public float typingSpeed = 0.03f;

    public void StartDialogue(DialogueLine[] lines)
    {
        currentLines = lines;
        currentIndex = 0;
        isActive = true;
        dialogueBox.SetActive(true);
        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (currentIndex < currentLines.Length)
        {
            speakerNameText.text = currentLines[currentIndex].speakerName;

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeText(currentLines[currentIndex].text));
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeText(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        AudioClip currentVoice = null;

        // Escolhe a voz com base no personagem
        string speaker = currentLines[currentIndex].speakerName.ToLower();
        if (speaker.Contains("guerreira"))
            currentVoice = voiceFemale;
        else if (speaker.Contains("alma"))
            currentVoice = voiceMale;

        foreach (char c in line)
        {
            dialogueText.text += c;

            if (currentVoice != null && !char.IsWhiteSpace(c))
                audioSource.PlayOneShot(currentVoice);

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }


    void Update()
    {
        if (isActive && Input.GetMouseButtonDown(0)) // Botão esquerdo do mouse
        {
            if (isTyping)
            {
                // Completa instantaneamente
                if (typingCoroutine != null)
                    StopCoroutine(typingCoroutine);

                dialogueText.text = currentLines[currentIndex].text;
                isTyping = false;
            }
            else
            {
                // Avança para a próxima linha
                currentIndex++;
                ShowCurrentLine();
            }
        }
    }

    public void EndDialogue()
    {
        isActive = false;
        dialogueBox.SetActive(false);
    }

    public bool IsDialogueActive()
    {
        return isActive;
    }
}
