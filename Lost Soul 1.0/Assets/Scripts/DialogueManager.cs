using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public float typingSpeed = 0.05f;

    private string[] sentences;
    private int index;
    private bool isTyping = false;
    private bool dialogueActive = false;

    void Update()
    {
        if (!dialogueActive)
            return;

        if (Input.GetMouseButtonDown(0)) 
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueText.text = sentences[index];
                isTyping = false;
            }
            else
            {
                NextSentence();
            }
        }
    }

    public void StartDialogue(string[] newSentences)
    {
        sentences = newSentences;
        index = 0;
        dialoguePanel.SetActive(true);
        dialogueActive = true;
        StartCoroutine(TypeSentence());
    }

    IEnumerator TypeSentence()
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in sentences[index].ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    public void NextSentence()
    {
        index++;
        if (index < sentences.Length)
        {
            StartCoroutine(TypeSentence());
        }
        else
        {
            dialoguePanel.SetActive(false);
            dialogueActive = false;
        }
    }
    public bool IsDialogueActive()
    {
        return dialogueActive;
    }

}
