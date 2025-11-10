using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerNameText;
    public GameObject dialogueBox;

    [Header("Opções")]
    public GameObject optionsPanel;               // painel onde os botões serão instanciados (deve ter VerticalLayoutGroup)
    public Button optionButtonPrefab;             // prefab do botão (Unity UI Button) com um TextMeshProUGUI filho para o texto

    [Header("Áudio e digitação")]
    public AudioClip voiceFemale;
    public AudioClip voiceMale;
    public float typingSpeed = 0.03f;

    private AudioSource audioSource;

    private DialogueLine[] currentLines;
    private int currentIndex = 0;
    private bool isActive = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    // Lista para limpar botões quando necessário
    private List<Button> spawnedOptionButtons = new List<Button>();

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (dialogueBox != null)
            dialogueBox.SetActive(false);
    }

    public void StartDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0) return;

        currentLines = lines;
        currentIndex = 0;
        isActive = true;
        dialogueBox.SetActive(true);
        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (currentIndex < 0 || currentIndex >= currentLines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentLines[currentIndex];

        speakerNameText.text = line.speakerName ?? "";

        // Se existirem opções -> mostrar opções
        if (line.options != null && line.options.Length > 0)
        {
            // Para segurança: pare qualquer digitação em andamento
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            dialogueText.text = line.text; // mostra texto completo (ou você pode querer digitar também)
            isTyping = false;

            ShowOptions(line.options);
        }
        else
        {
            // Sem opções: mostra o texto com digitação
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeText(line.text));
        }
    }

    IEnumerator TypeText(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        AudioClip currentVoice = null;
        string speaker = currentLines[currentIndex].speakerName?.ToLower() ?? "";

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
        typingCoroutine = null;
    }

    void Update()
    {
        if (!isActive) return;

        // Se o painel de opções estiver ativo, não permita avançar com clique do mouse (jogador precisa escolher)
        if (optionsPanel != null && optionsPanel.activeSelf)
            return;

        if (Input.GetMouseButtonDown(0))
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

    private void ShowOptions(DialogueOption[] options)
    {
        if (optionsPanel == null || optionButtonPrefab == null)
        {
            Debug.LogWarning("Options panel or optionButtonPrefab not assigned in DialogueManager.");
            return;
        }

        ClearOptionButtons();
        optionsPanel.SetActive(true);

        for (int i = 0; i < options.Length; i++)
        {
            DialogueOption opt = options[i]; // capture local
            Button btn = Instantiate(optionButtonPrefab, optionsPanel.transform);
            spawnedOptionButtons.Add(btn);

            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = opt.optionText;

            btn.onClick.AddListener(() => OnOptionSelected(opt));
        }
    }

    private void OnOptionSelected(DialogueOption selected)
    {
        // Invoca evento/desencadeadores configurados no Inspector
        if (selected.onSelected != null)
            selected.onSelected.Invoke();

        // Fecha painel de opções e remove botões
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        ClearOptionButtons();

        // Decide fluxo: se nextLineIndex >= 0 pule para lá, se -1 então continue linear (próxima linha)
        if (selected.nextLineIndex >= 0 && currentLines != null && selected.nextLineIndex < currentLines.Length)
        {
            currentIndex = selected.nextLineIndex;
        }
        else
        {
            currentIndex++; // apenas avança normalmente
        }

        ShowCurrentLine();
    }

    private void ClearOptionButtons()
    {
        foreach (var b in spawnedOptionButtons)
        {
            if (b != null) Destroy(b.gameObject);
        }
        spawnedOptionButtons.Clear();
    }

    public void EndDialogue()
    {
        isActive = false;
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        ClearOptionButtons();
    }

    public bool IsDialogueActive()
    {
        return isActive;
    }
}
