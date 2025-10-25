using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [Header("Referências")]
    public List<LightPad> pads = new List<LightPad>(); // arraste os 5 aqui

    [Header("Timings")]
    [Tooltip("Quanto tempo cada passo da sequência fica ACESO em vermelho.")]
    public float sequenceStepOn = 0.5f;
    [Tooltip("Pausa apagado entre passos da sequência.")]
    public float sequenceStepOff = 0.25f;
    [Tooltip("Intervalo para piscadas simultâneas (erro e vitória).")]
    public float globalBlinkInterval = 0.5f;
    [Tooltip("Duração do verde quando o jogador acerta um clique.")]
    public float hitGreenDuration = 0.2f;

    [Header("Regras")]
    public int totalRounds = 5;           // 5 rodadas
    public int blinksOnError = 3;         // quantas piscadas vermelhas no erro
    public int blinksOnFinalWin = 3;      // quantas piscadas verdes na vitória

    // Estado interno
    private List<int> _sequence = new List<int>();
    private int _currentRound = 1;
    private int _playerIndex = 0;
    private bool _inputEnabled = false;
    private System.Random _rng;

    private Camera _cam;

    private void Start()
    {
        if (pads == null || pads.Count == 0)
        {
            Debug.LogError("PuzzleManager: arraste os LightPads na lista 'pads'.");
            enabled = false;
            return;
        }

        foreach (var p in pads) p.SetApagado();

        _rng = new System.Random();
        _cam = Camera.main;

        StartCoroutine(GameLoop());
    }

    private void Update()
    {
        // Lê "ataque": clique esquerdo exatamente em cima de um pad
        if (_inputEnabled && Input.GetMouseButtonDown(0))
        {
            var pad = GetPadUnderMouse();
            if (pad != null)
            {
                HandlePadClicked(pad);
            }
        }
    }

    private LightPad GetPadUnderMouse()
    {
        if (_cam == null) return null;
        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point2D = new Vector2(mouseWorld.x, mouseWorld.y);

        // Detecta qualquer collider no ponto do mouse
        Collider2D col = Physics2D.OverlapPoint(point2D);
        if (!col) return null;

        return col.GetComponent<LightPad>();
    }

    private IEnumerator GameLoop()
    {
        while (true)
        {
            yield return StartCoroutine(PlayAllRounds());
            // Vitória final: todas verdes simultaneamente
            yield return StartCoroutine(BlinkAllGreen(blinksOnFinalWin, globalBlinkInterval));
            yield return new WaitForSeconds(0.75f);
        }
    }

    private IEnumerator PlayAllRounds()
    {
        _currentRound = 1;

        while (_currentRound <= totalRounds)
        {
            GenerateSequence(_currentRound);       // nova sequência com tamanho = número da rodada
            yield return StartCoroutine(ShowSequence()); // mostra em vermelho (input desabilitado)

            _playerIndex = 0;
            _inputEnabled = true;

            // Espera até acertar toda a sequência (ou errar)
            while (_inputEnabled && _playerIndex < _sequence.Count)
            {
                yield return null;
            }

            // Se erro, _inputEnabled vira false em HandleFail() e reiniciamos TUDO
            if (!_inputEnabled)
            {
                yield break; // sai para GameLoop → recomeça
            }

            // Concluiu a rodada
            _currentRound++;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void GenerateSequence(int length)
    {
        _sequence.Clear();
        for (int i = 0; i < length; i++)
        {
            int padId = _rng.Next(0, pads.Count); // 0..(n-1)
            _sequence.Add(padId);
        }
    }

    private IEnumerator ShowSequence()
    {
        _inputEnabled = false;

        // Garante tudo apagado
        SetAllOff();
        yield return new WaitForSeconds(0.25f);

        // Exibe passo a passo em vermelho
        for (int i = 0; i < _sequence.Count; i++)
        {
            var pad = pads[_sequence[i]];
            pad.SetVermelho();
            yield return new WaitForSeconds(sequenceStepOn);
            pad.SetApagado();
            yield return new WaitForSeconds(sequenceStepOff);
        }
    }

    private void HandlePadClicked(LightPad pad)
    {
        // Verifica se é o pad esperado
        int expectedPadId = _sequence[_playerIndex];
        if (pad.id == expectedPadId)
        {
            // Acertou este passo → feedback verde
            StartCoroutine(pad.FlashVerde(hitGreenDuration));
            _playerIndex++;
            // Se completar a sequência da rodada, o loop principal avança
        }
        else
        {
            // Errou → falha geral
            StartCoroutine(HandleFail());
        }
    }

    private IEnumerator HandleFail()
    {
        _inputEnabled = false;

        // Pisca todas em vermelho simultâneo com intervalo de 0.5s
        yield return StartCoroutine(BlinkAllRed(blinksOnError, globalBlinkInterval));

        // Pequena pausa e reinicia as rodadas do começo
        yield return new WaitForSeconds(0.5f);

        StopAllCoroutines();       // interrompe qualquer exibição corrente
        StartCoroutine(GameLoop()); // recomeça
    }

    private IEnumerator BlinkAllRed(int times, float interval)
    {
        for (int i = 0; i < times; i++)
        {
            SetAllRed();
            yield return new WaitForSeconds(interval);
            SetAllOff();
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator BlinkAllGreen(int times, float interval)
    {
        for (int i = 0; i < times; i++)
        {
            SetAllGreen();
            yield return new WaitForSeconds(interval);
            SetAllOff();
            yield return new WaitForSeconds(interval);
        }
    }

    private void SetAllOff()
    {
        foreach (var p in pads) p.SetApagado();
    }
    private void SetAllRed()
    {
        foreach (var p in pads) p.SetVermelho();
    }
    private void SetAllGreen()
    {
        foreach (var p in pads) p.SetVerde();
    }
}
