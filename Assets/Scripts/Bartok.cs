using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

// Это перечисление определяет разные этапы в течение одного игрового хода
public enum TurnPhase
{
    Idle,
    Pre,
    Waiting,
    Post,
    GameOver
}

public class Bartok : MonoBehaviour
{
    static public Bartok S;
    static public Player CURRENT_PLAYER;

    [Header("Set in Inspector")]
    public TextAsset DeckXML;
    public TextAsset LayoutXML;
    public Vector3 LayoutCenter = Vector3.zero;
    public float HandFanDegrees = 10f;
    public int NumStartingCards = 7;
    public float DrawTimeStagger = 0.1f;

    [Header("Set Dynamically")]
    public Deck Deck;
    public List<CardBartok> DrawPile;
    public List<CardBartok> DiscardPile;
    public List<Player> Players;
    public CardBartok TargetCard;
    public TurnPhase Phase = TurnPhase.Idle;

    private BartokLayout _layout;
    private Transform _layoutAnchor;

    private void Awake()
    {
        Culturator();   // Метод по преобразованию говна в конфетку
        S = this;
    }

    private void Start()
    {
        Deck = GetComponent<Deck>();    // Получить компонент Deck
        Deck.InitDeck(DeckXML.text);    // Передать ему DeckXML
        Deck.Shuffle(ref Deck.Cards);   // Перетасовать колоду

        _layout = GetComponent<BartokLayout>();   // Получить ссылку на компонент Layout
        _layout.ReadLayout(LayoutXML.text);       // Передать ему LayoutXML

        DrawPile = UpgradeCardsList(Deck.Cards);
        LayoutGame();
    }

    /*
    // Метод Update() временно используется для проверки длобавлениякарты в руки игрока
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Players[0].AddCard(Draw());
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Players[1].AddCard(Draw());
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Players[2].AddCard(Draw());
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Players[3].AddCard(Draw());
        }
    }
    */

    private List<CardBartok> UpgradeCardsList(List<Card> lCD)
    {
        List<CardBartok> lCB = new List<CardBartok>();
        foreach (Card tCD in lCD)
        {
            lCB.Add(tCD as CardBartok);
        }
        return lCB;
    }

    // Позиционирует все карты в DrawPile
    public void ArrangeDrawPile()
    {
        CardBartok tCardBartok;
        for (int i = 0; i < DrawPile.Count; i++)
        {
            tCardBartok = DrawPile[i];
            tCardBartok.transform.SetParent(_layoutAnchor);
            tCardBartok.transform.localPosition = _layout.DrawPile.Position;
            // Угол поворота начинается с 0
            tCardBartok.FaceUp = false;
            tCardBartok.SetSortingLayerName(_layout.DrawPile.LayerName);
            tCardBartok.SetSortOrder(-i * 4);   // Упорядочить от первых к последним
            tCardBartok.State = CBState.Drawpile;
        }
    }

    // Выполняет первоначальную раздачу карт в игре
    private void LayoutGame()
    {
        // Создать пустой GameObject - точку привязки для раскладки
        if (_layoutAnchor == null)
        {
            GameObject gameObject = new GameObject("_LayoutAnchor");
            _layoutAnchor = gameObject.transform;
            _layoutAnchor.transform.position = LayoutCenter;
        }

        // Позиционировать свободные карты
        ArrangeDrawPile();

        // Настроить игроков
        Player player;
        Players = new List<Player>();
        foreach (SlotDef slotDef in _layout.SlotDefs)
        {
            player = new Player();
            player.HandSlotDef = slotDef;
            Players.Add(player);
            player.PlayerNum = slotDef.Player;
        }

        Players[0].Type = PlayerType.Human;   // 0-ой игрок - человек

        CardBartok tCardBartok;
        // Раздать игрокам по 7 карт
        for (int i = 0; i < NumStartingCards; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                tCardBartok = Draw();   // Снять карту
                // Немного отложить начало перемещения карты
                tCardBartok.TimeStart = Time.time + DrawTimeStagger * (i * 4 + j);

                Players[(j + 1) % 4].AddCard(tCardBartok);
            }
        }

        Invoke("DrawFirstTarget", DrawTimeStagger * (NumStartingCards * 4 + 4));
    }

    public void DrawFirstTarget()
    {
        // Перевернуть первую целевую карту лицевой стороной вверх
        CardBartok tCardBartok = MoveToTarget(Draw());
        // Вызвать метод CBCallback сценария Bartok, когда карта закончит перемещение
        tCardBartok.ReportFinishTo = this.gameObject;
    }

    // Этот обратный вызов используется последней розданной картой в начале игры
    public void CBCallback(CardBartok cardBartok)
    {
        // Иногда желательно сообщить о вызове метода, как здесь
        Utils.tr("Bartok:CBCallback()", cardBartok.name);
        StartGame();   // Начать игру
    }

    public void StartGame()
    {
        // Право первого хода принадлежит игроку слева от человека
        PassTurn(1);
    }

    public void PassTurn(int num = -1)
    {
        // Если порядковый номер игрока не указан, выбрать следующего по кругу
        if (num == -1)
        {
            int ndx = Players.IndexOf(CURRENT_PLAYER);
            num = (ndx + 1) % 4;
        }

        int lastPlayerNum = -1;
        if (CURRENT_PLAYER != null)
        {
            lastPlayerNum = CURRENT_PLAYER.PlayerNum;
            // Проверить завершение игры и необходимость перетасовать стопку сброшенных карт
            if (CheckGameOver())
            {
                return;
            }
        }

        CURRENT_PLAYER = Players[num];
        Phase = TurnPhase.Pre;

        CURRENT_PLAYER.TakeTurn();

        // Сообщить о передаче хода
        Utils.tr("Bartok:PassTurn()", "Old: " + lastPlayerNum, "New: " + CURRENT_PLAYER.PlayerNum);
    }

    public bool CheckGameOver()
    {
        // Проверить нужно ли перетасовать стопку сброшенных карт и
        // перенести её в стопку свободных карт
        if (DrawPile.Count == 0)
        {
            List<Card> cards = new List<Card>();
            foreach (CardBartok cardBartok in DiscardPile)
            {
                cards.Add(cardBartok);
            }
            DiscardPile.Clear();
            Deck.Shuffle(ref cards);
            ArrangeDrawPile();
        }

        // Проверить победу текущего игрока
        if (CURRENT_PLAYER.Hand.Count == 0)
        {
            // Игрок, только что сделавший ход, победил!
            Phase = TurnPhase.GameOver;
            Invoke("RestartGame", 1);
            return true;
        }

        return false;
    }

    public void RestartGame()
    {
        CURRENT_PLAYER = null;
        SceneManager.LoadScene("Bartok Scene 0");
    }

    // ValidPlay проверяет возможность сыграть выбранной картой
    public bool ValidPlay(CardBartok cardBartok)
    {
        // Картой можно сыграть, если она имеет такое же достоинство, как целевая карта
        if (cardBartok.Rank == TargetCard.Rank)
        {
            return true;
        }

        // Картой можно сыграть, если её масть совпадает с мастью целевой карты
        if (cardBartok.Suit == TargetCard.Suit)
        {
            return true;
        }

        // Иначе вернуть false
        return false;
    }

    // Делает указанную карту целевой
    public CardBartok MoveToTarget(CardBartok tCardBartok)
    {
        tCardBartok.TimeStart = 0;
        tCardBartok.MoveTo(_layout.DiscardPile.Position + Vector3.back);
        tCardBartok.State = CBState.ToTarget;
        tCardBartok.FaceUp = true;

        tCardBartok.SetSortingLayerName("10");
        tCardBartok.EventualSortLayer = _layout.Target.LayerName;
        if (TargetCard != null)
        {
            MoveToDiscard(TargetCard);
        }

        TargetCard = tCardBartok;

        return tCardBartok;
    }

    public CardBartok MoveToDiscard(CardBartok tCardBartok)
    {
        tCardBartok.State = CBState.Discard;
        DiscardPile.Add(tCardBartok);
        tCardBartok.SetSortingLayerName(_layout.DiscardPile.LayerName);
        tCardBartok.SetSortOrder(DiscardPile.Count * 4);
        tCardBartok.transform.localPosition = _layout.DiscardPile.Position + Vector3.back / 2;

        return tCardBartok;
    }

    // Функция Draw() снимает верхнюю карту со стопки свободных карт и возвращает её
    public CardBartok Draw()
    {
        CardBartok cd = DrawPile[0];   // Извлечь 0-ую карту

        if (DrawPile.Count == 0)   // Если список DrawPile опустел
        {
            // ... нужно перетасовать сброшенные карты и переложить их в стопку свободных карт
            int ndx;
            while (DiscardPile.Count > 0)
            {
                // Вынуть случайную карту из стопки сброшенных карт
                ndx = Random.Range(0, DiscardPile.Count);
                DrawPile.Add(DiscardPile[ndx]);
                DiscardPile.RemoveAt(ndx);
            }

            ArrangeDrawPile();
            // Показать перемещение карт в стопку свободных карт
            float t = Time.time;
            foreach (CardBartok tCardBartok in DrawPile)
            {
                tCardBartok.transform.localPosition = _layout.DiscardPile.Position;
                tCardBartok.CallbackPlayer = null;
                tCardBartok.MoveTo(_layout.DrawPile.Position);
                tCardBartok.TimeStart = t;
                t += 0.02f;
                tCardBartok.State = CBState.ToDrawpile;
                tCardBartok.EventualSortLayer = "0";
            }
        }

        DrawPile.RemoveAt(0);          // Удалить её из списка DrawPile
        return cd;                     // и вернуть
    }

    public void CardClicked(CardBartok tCardBartok)
    {
        if (CURRENT_PLAYER.Type != PlayerType.Human)
        {
            return;
        }
        if (Phase == TurnPhase.Waiting)
        {
            return;
        }

        switch (tCardBartok.State)
        {
            case CBState.Drawpile:
                // Взять верхнюю карту, не обязательно ту, по которой выполнен щелчок
                CardBartok cardBartok = CURRENT_PLAYER.AddCard(Draw());
                cardBartok.CallbackPlayer = CURRENT_PLAYER;
                Utils.tr("Bartok:CardClicked()", "Draw", cardBartok.name);
                Phase = TurnPhase.Waiting;
                break;

            case CBState.Hand:
                // Проверить допустимость выбранной карты
                if (ValidPlay(tCardBartok))
                {
                    CURRENT_PLAYER.RemoveCard(tCardBartok);
                    MoveToTarget(tCardBartok);
                    tCardBartok.CallbackPlayer = CURRENT_PLAYER;
                    Utils.tr("Bartok:CardClicked()", "Play", tCardBartok.name, TargetCard.name + " is target");
                    Phase = TurnPhase.Waiting;
                }
                else
                {
                    // Игнорировать выбор недопустимой карты, но сообщать о попытке игрока
                    Utils.tr("Bartok:CardClicked()", "Attempted to Play", tCardBartok.name, TargetCard.name + " is target");
                }
                break;
        }
    }



    // Метод, решающий проблему преобразования локальных особенностей символов, связанной с CultureInfo
    private void Culturator()
    {
        CultureInfo cultureInfo = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        CultureInfo.CurrentCulture = cultureInfo;
    }
}
