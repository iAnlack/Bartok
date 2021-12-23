using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Перечисление со всеми возможными состояниями FloatingScore
public enum eFSState
{
    Idle,
    Pre,
    Active,
    Post
}

// FloatingScore может перемещаться на экране по траектории которая определяется кривой Безье
public class FloatingScore : MonoBehaviour
{
    [Header("Set Dynamically")]
    public eFSState State = eFSState.Idle;

    [SerializeField] protected int Score = 0;
    public string ScoreString;

    // Свойство Score устанавливает два поля: Score и ScoreString
    public int ScoreSetter
    {
        get
        {
            return Score;
        }
        set
        {
            Score = value;
            ScoreString = Score.ToString("N0");   // Аргумент "N0" требует добавить точки в число

            // Поищите в интернете по фразе "C# Строки стандартных числовых форматов", чтобы найти
            // описание форматов, поддерживаемых методом ToString
            GetComponent<Text>().text = ScoreString;
        }
    }

    public List<Vector2> BezierPoints;          // Точки, определяющие кривую Безье
    public List<float> FontSizes;               // Точки кривой Безье для масштабирования шрифта
    public float TimeStart = -1f;
    public float TimeDuration = 1f;
    public string EasingCurve = Easing.InOut;   // Функция сглаживания из Utils.cs

    // Игровой объект, для которого будет вызван метод SendMessage,
    // когда этот экземпляр FloatingScore закончит движение
    public GameObject ReportFinishTo = null;

    private RectTransform _rectTransform;
    private Text _text;

    private void Update()
    {
        // Если этот объект никуда не перемещается, просто выйти
        if (State == eFSState.Idle)
        {
            return;
        }

        // Вычислить u на основе текущего времени и продолжительности движения
        // u изменяется от 0 до 1 (обычно)
        float u = (Time.time - TimeStart) / TimeDuration;
        // Использовать класс Easing из Utils для корректировки значения u
        float uC = Easing.Ease(u, EasingCurve);
        if (u < 0)
        {
            // Если u < 0, объект не должен двигаться
            State = eFSState.Pre;
            _text.enabled = false;   // Изначально скрыть число
        }
        else
        {
            if (u >= 1)
            {
                // Если u >= 1, выполняется движение
                uC = 1; // Установить uC = 1, чтобы не выйти за крайнюю точку
                State = eFSState.Post;
                if (ReportFinishTo != null)
                {
                    // Если игровой объект указан, использовать SendMessage для вызова метода FSCallback
                    // и передачи ему текущего экземпляра в параметре
                    ReportFinishTo.SendMessage("FSCallback", this);
                    // После отправки сообщения уничтожить gameObject
                    Destroy(gameObject);
                }
                else
                {
                    // Если не указан, не уничтожать текущий экземпляр. Просто оставить его в покое
                    State = eFSState.Idle;
                }
            }
            else
            {
                // Если 0 <= u < 1, значит, текущий экземпляр активен и движется
                State = eFSState.Active;
                _text.enabled = true;   // Показать число очков
            }

            // Использовать кривую Безье для перемещения к заданной точке
            Vector2 pos = Utils.Bezier(uC, BezierPoints);
            // Опорные точки RectTransform можно использовать для позиционирования объектов
            // пользовательского интерфейса относительно общего размера экрана
            _rectTransform.anchorMin = _rectTransform.anchorMax = pos;
            if (FontSizes != null && FontSizes.Count > 0)
            {
                // Если список FontSizes содержит значения, то скорректировать FontSize этого объекта GUIText
                int size = Mathf.RoundToInt(Utils.Bezier(uC, FontSizes));
                GetComponent<Text>().fontSize = size;
            }
        }
    }

    // Настроить FloatingScore и параметры движения
    // Обратите внимание, что для параметров eTimeS и eTimeD определены значения по умолчанию
    public void Init(List<Vector2> ePoints, float eTimeS = 0, float eTimeD = 1)
    {
        _rectTransform = GetComponent<RectTransform>();
        _rectTransform.anchoredPosition = Vector2.zero;

        _text = GetComponent<Text>();

        BezierPoints = new List<Vector2>(ePoints);

        if (ePoints.Count == 1)   // Если задана только одна точка, то просто переместиться в неё
        {
            transform.position = ePoints[0];
            return;
        }

        // Если eTimeS имеет значение по умолчанию, запустить отсчёт от текущего времени
        if (eTimeS == 0)
        {
            eTimeS = Time.time;
        }

        TimeStart = eTimeS;
        TimeDuration = eTimeD;

        State = eFSState.Pre;   // Установить состояние pre - готовность начать движение
    }

    public void FSCallBack(FloatingScore floatingScore)
    {
        // Когда SendMessage вызовет эту функцию, она должна добавить очки
        // из вызвавшего экземпляра FloatingScore
        ScoreSetter += floatingScore.ScoreSetter;
    }
}
