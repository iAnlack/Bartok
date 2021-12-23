using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Перечисление со всеми возможными событиями начисления очков
public enum eScoreEvent
{
    Draw,
    Mine,
    MineGold,
    GameWin,
    GameLoss
}

// ScoreManager управляет подсчётос очков
public class ScoreManager : MonoBehaviour
{
    static private ScoreManager S;

    static public int SCORE_FROM_PREV_ROUND = 0;
    static public int HIGH_SCORE = 0;

    [Header("Set Dinamically")]
    // Поля для хранения информации о заработанных очках
    public int Chain = 0;
    public int ScoreRun = 0;
    public int Score = 0;

    static public int CHAIN
    {
        get
        {
            return S.Chain;
        }
    }

    static public int SCORE
    {
        get
        {
            return S.Score;
        }
    }

    static public int SCORE_RUN
    {
        get
        {
            return S.ScoreRun;
        }
    }

    private void Awake()
    {
        if (S == null)
        {
            S = this;   // Подготовка скрытого объекта одиночки
        }
        else
        {
            Debug.LogError("ERROR: ScoreManager.Awake(): S is already set!");
        }

        // Проверить рекорд в PlayerPrefs
        if (PlayerPrefs.HasKey ("ProspectorHighScore"))
        {
            HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");
        }

        // Добавить очки, заработанные в последнем раунде, которые должны быть > 0, если раунд завершился победой
        Score += SCORE_FROM_PREV_ROUND;
        // И сбросить SCORE_FROM_PREV_ROUND
        SCORE_FROM_PREV_ROUND = 0;
    }

    static public void EVENT(eScoreEvent evt)
    {
        try
        {
            // try-catch не позволит ошибке аварийно завершить программу
            S.Event(evt);
        }
        catch (System.NullReferenceException nre)
        {

            Debug.LogError("ScoreManager: EVENT() called while S = null.\n" + nre);
        }
    }

    private void Event(eScoreEvent evt)
    {
        switch (evt)
        {
            // В случае победы, проигрыша и завершения хода выполняются одни и те же действия
            case eScoreEvent.Draw:       // Выбор свободной карты
            case eScoreEvent.GameWin:    // Победа в раунде
            case eScoreEvent.GameLoss:   // Поражение в раунде
                Chain = 0;               // сбросить цепочку подсчёта очков
                Score += ScoreRun;       // добавить ScoreRun к общему числу очков
                ScoreRun = 0;            // сбросить ScoreRun
                break;

            case eScoreEvent.Mine:       // Удаление карты из основной раскладки
                Chain++;                 // увеличить количество очков в цепочке
                ScoreRun += Chain;       // добавить очки в карту
                break;
        }

        // Эта вторая инструкция switch обрабатывает победу и проигрыш в раунде
        switch (evt)
        {
            case eScoreEvent.GameWin:
                // В случае победы перенести очки в следующий раунд статистические поля НЕ сбрасываются вызовом
                // SceneManager.LoadScene()
                SCORE_FROM_PREV_ROUND = Score;
                Debug.Log("You won this round! Round score: " + Score);
                break;

            case eScoreEvent.GameLoss:
                // В случае проигрыша сравнить с рекордом
                if (HIGH_SCORE <= Score)
                {
                    Debug.Log("You got the high score! High score: " + Score);
                    HIGH_SCORE = Score;
                    PlayerPrefs.SetInt("ProsprectorHighScore", Score);
                }
                else
                {
                    Debug.Log("Your final score for the game was: " + Score);
                }
                break;

            default:
                Debug.Log("Score: " + Score + " ScoreRun:" + ScoreRun + " Chain:" + Chain);
                break;
        }
    }
}
