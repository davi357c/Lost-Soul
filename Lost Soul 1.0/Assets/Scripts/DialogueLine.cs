using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class DialogueOption
{
    [Tooltip("Texto mostrado no botão da escolha")]
    public string optionText;

    [Tooltip("Índice da próxima linha de diálogo (-1 = próxima linha normal)")]
    public int nextLineIndex = -1;

    [Tooltip("Evento chamado ao escolher esta opção (arraste objetos/métodos no Inspector)")]
    public UnityEvent onSelected;
}

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea(2, 5)]
    public string text;

    [Tooltip("Se preenchido, mostra botões de escolha nesta linha")]
    public DialogueOption[] options;
}
