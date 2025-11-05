using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class PuzzleManager : MonoBehaviour
{
    [Header("Pads (preencha ou eu encontro)")]
    public List<LightPad> pads = new List<LightPad>();

    [Header("Regras")]
    [Tooltip("Total de rodadas (1..N). Cada rodada adiciona +1 passo à sequência.")]
    public int totalRounds = 5;

    [Header("Tempos")]
    [Tooltip("Quanto tempo cada pad fica aceso ao mostrar a sequência (vermelho).")]
    public float litTime = 0.35f;
    [Tooltip("Intervalo entre cada passo da sequência.")]
    public float showDelay = 0.55f;
    [Tooltip("Intervalo para as piscadas simultâneas (erro/sucesso final).")]
    public float blinkInterval = 0.5f;

    [Header("Eventos")]
    public UnityEvent onPuzzleStart;
    public UnityEvent onPuzzleSuccess;
    public UnityEvent onPuzzleFail; // dispara em ERRO (antes de resetar)

    [Header("Recompensas")]
    [Tooltip("Player que terá a escalada em parede liberada após o sucesso.")]
    public PlayerMovement playerMovement;

    List<int> sequence = new List<int>();
    int currentRoundLength = 1;
    int inputIndex = 0;
    bool active;
    bool showing;

    void Awake()
    {
        if (pads == null || pads.Count == 0)
            pads = FindObjectsOfType<LightPad>(true).OrderBy(p => p.id).ToList();

        foreach (var p in pads)
        {
            p.Bind(this);
            p.SetOff();
        }

        // Se não for atribuído via Inspector, tenta achar um PlayerMovement na cena
        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerMovement>();
    }

    // Chamado pela alavanca
    public void ActivatePuzzle()
    {
        if (pads == null || pads.Count == 0)
        {
            Debug.LogError("[Puzzle] Sem pads. Adicione LightPad na cena e configure IDs.");
            return;
        }

        StopAllCoroutines();
        foreach (var p in pads) { p.SetInteractable(false); p.SetOff(); }

        currentRoundLength = 1;
        inputIndex = 0;
        active = true;
        onPuzzleStart?.Invoke();

        StartCoroutine(BeginRoundAfter(0.3f));
    }

    IEnumerator BeginRoundAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartRound();
    }

    void StartRound()
    {
        BuildSequence(currentRoundLength);
        inputIndex = 0;
        StartCoroutine(ShowSequenceRed());
    }

    void BuildSequence(int length)
    {
        sequence.Clear();
        var rnd = new System.Random();
        for (int i = 0; i < length; i++)
            sequence.Add(rnd.Next(0, pads.Count));

        Debug.Log($"[Puzzle] Round {currentRoundLength}/{totalRounds} - Seq: {string.Join(",", sequence)}");
    }

    IEnumerator ShowSequenceRed()
    {
        showing = true;
        foreach (var p in pads) { p.SetInteractable(false); p.SetOff(); }

        yield return new WaitForSeconds(0.35f);

        for (int i = 0; i < sequence.Count; i++)
        {
            int idx = sequence[i];
            var pad = pads[idx];
            pad.ShowRed(litTime);
            yield return new WaitForSeconds(litTime + showDelay);
        }

        foreach (var p in pads) p.SetInteractable(true);
        showing = false;
        Debug.Log("[Puzzle] Sua vez! Clique na ordem.");
    }

    public void OnPadClicked(LightPad pad)
    {
        if (!active || showing) return;

        int expectedIdx = sequence[inputIndex];
        int clickedIdx = pads.IndexOf(pad);

        if (clickedIdx == expectedIdx)
        {
            pad.FlashGreen(0.2f);
            inputIndex++;

            // Completou a rodada atual?
            if (inputIndex >= currentRoundLength)
            {
                // Acabou o puzzle (última rodada)?
                if (currentRoundLength >= totalRounds)
                {
                    StartCoroutine(WinSequence());
                }
                else
                {
                    // Próxima rodada (aumenta +1 passo)
                    foreach (var p in pads) p.SetInteractable(false);
                    currentRoundLength++;
                    inputIndex = 0;
                    StartCoroutine(BeginRoundAfter(0.8f));
                }
            }
        }
        else
        {
            // Errou: todas piscam vermelho simultâneo (intervalo 0.5s)
            StartCoroutine(LoseSequence());
        }
    }

    IEnumerator WinSequence()
    {
        active = false;
        foreach (var p in pads) p.SetInteractable(false);

        // Todas VERDE simultâneo com intervalo de 0.5s (3 piscadas)
        yield return StartCoroutine(BlinkAll(sprite: "green", times: 3, interval: blinkInterval));

        onPuzzleSuccess?.Invoke();

        // Libera a escalada em parede no player
        if (playerMovement != null)
        {
            playerMovement.EnableWallClimb();
        }

        // Depois do sucesso, garante tudo apagado
        foreach (var p in pads) p.SetOff();
    }

    IEnumerator LoseSequence()
    {
        active = false;
        foreach (var p in pads) p.SetInteractable(false);

        onPuzzleFail?.Invoke();

        // Todas VERMELHO simultâneo com intervalo de 0.5s (3 piscadas)
        yield return StartCoroutine(BlinkAll(sprite: "red", times: 3, interval: blinkInterval));

        // Reset TOTAL: volta para a rodada 1
        foreach (var p in pads) p.SetOff();
        currentRoundLength = 1;
        inputIndex = 0;
        active = true;

        yield return new WaitForSeconds(0.6f);
        StartRound();
    }

    IEnumerator BlinkAll(string sprite, int times, float interval)
    {
        for (int i = 0; i < times; i++)
        {
            if (sprite == "red") foreach (var p in pads) p.FlashRed(interval * 0.9f);
            if (sprite == "green") foreach (var p in pads) p.FlashGreen(interval * 0.9f);
            yield return new WaitForSeconds(interval);
            foreach (var p in pads) p.SetOff();
            yield return new WaitForSeconds(0.05f); // pequeno respiro
        }
    }
}
