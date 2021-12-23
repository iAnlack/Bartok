using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

// ��� ������������ ���������� ������ ����� � ������� ������ �������� ����
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
        Culturator();   // ����� �� �������������� ����� � ��������
        S = this;
    }

    private void Start()
    {
        Deck = GetComponent<Deck>();    // �������� ��������� Deck
        Deck.InitDeck(DeckXML.text);    // �������� ��� DeckXML
        Deck.Shuffle(ref Deck.Cards);   // ������������ ������

        _layout = GetComponent<BartokLayout>();   // �������� ������ �� ��������� Layout
        _layout.ReadLayout(LayoutXML.text);       // �������� ��� LayoutXML

        DrawPile = UpgradeCardsList(Deck.Cards);
        LayoutGame();
    }

    /*
    // ����� Update() �������� ������������ ��� �������� ���������������� � ���� ������
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

    // ������������� ��� ����� � DrawPile
    public void ArrangeDrawPile()
    {
        CardBartok tCardBartok;
        for (int i = 0; i < DrawPile.Count; i++)
        {
            tCardBartok = DrawPile[i];
            tCardBartok.transform.SetParent(_layoutAnchor);
            tCardBartok.transform.localPosition = _layout.DrawPile.Position;
            // ���� �������� ���������� � 0
            tCardBartok.FaceUp = false;
            tCardBartok.SetSortingLayerName(_layout.DrawPile.LayerName);
            tCardBartok.SetSortOrder(-i * 4);   // ����������� �� ������ � ���������
            tCardBartok.State = CBState.Drawpile;
        }
    }

    // ��������� �������������� ������� ���� � ����
    private void LayoutGame()
    {
        // ������� ������ GameObject - ����� �������� ��� ���������
        if (_layoutAnchor == null)
        {
            GameObject gameObject = new GameObject("_LayoutAnchor");
            _layoutAnchor = gameObject.transform;
            _layoutAnchor.transform.position = LayoutCenter;
        }

        // ��������������� ��������� �����
        ArrangeDrawPile();

        // ��������� �������
        Player player;
        Players = new List<Player>();
        foreach (SlotDef slotDef in _layout.SlotDefs)
        {
            player = new Player();
            player.HandSlotDef = slotDef;
            Players.Add(player);
            player.PlayerNum = slotDef.Player;
        }

        Players[0].Type = PlayerType.Human;   // 0-�� ����� - �������

        CardBartok tCardBartok;
        // ������� ������� �� 7 ����
        for (int i = 0; i < NumStartingCards; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                tCardBartok = Draw();   // ����� �����
                // ������� �������� ������ ����������� �����
                tCardBartok.TimeStart = Time.time + DrawTimeStagger * (i * 4 + j);

                Players[(j + 1) % 4].AddCard(tCardBartok);
            }
        }

        Invoke("DrawFirstTarget", DrawTimeStagger * (NumStartingCards * 4 + 4));
    }

    public void DrawFirstTarget()
    {
        // ����������� ������ ������� ����� ������� �������� �����
        CardBartok tCardBartok = MoveToTarget(Draw());
        // ������� ����� CBCallback �������� Bartok, ����� ����� �������� �����������
        tCardBartok.ReportFinishTo = this.gameObject;
    }

    // ���� �������� ����� ������������ ��������� ��������� ������ � ������ ����
    public void CBCallback(CardBartok cardBartok)
    {
        // ������ ���������� �������� � ������ ������, ��� �����
        Utils.tr("Bartok:CBCallback()", cardBartok.name);
        StartGame();   // ������ ����
    }

    public void StartGame()
    {
        // ����� ������� ���� ����������� ������ ����� �� ��������
        PassTurn(1);
    }

    public void PassTurn(int num = -1)
    {
        // ���� ���������� ����� ������ �� ������, ������� ���������� �� �����
        if (num == -1)
        {
            int ndx = Players.IndexOf(CURRENT_PLAYER);
            num = (ndx + 1) % 4;
        }

        int lastPlayerNum = -1;
        if (CURRENT_PLAYER != null)
        {
            lastPlayerNum = CURRENT_PLAYER.PlayerNum;
            // ��������� ���������� ���� � ������������� ������������ ������ ���������� ����
            if (CheckGameOver())
            {
                return;
            }
        }

        CURRENT_PLAYER = Players[num];
        Phase = TurnPhase.Pre;

        CURRENT_PLAYER.TakeTurn();

        // �������� � �������� ����
        Utils.tr("Bartok:PassTurn()", "Old: " + lastPlayerNum, "New: " + CURRENT_PLAYER.PlayerNum);
    }

    public bool CheckGameOver()
    {
        // ��������� ����� �� ������������ ������ ���������� ���� �
        // ��������� � � ������ ��������� ����
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

        // ��������� ������ �������� ������
        if (CURRENT_PLAYER.Hand.Count == 0)
        {
            // �����, ������ ��� ��������� ���, �������!
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

    // ValidPlay ��������� ����������� ������� ��������� ������
    public bool ValidPlay(CardBartok cardBartok)
    {
        // ������ ����� �������, ���� ��� ����� ����� �� �����������, ��� ������� �����
        if (cardBartok.Rank == TargetCard.Rank)
        {
            return true;
        }

        // ������ ����� �������, ���� � ����� ��������� � ������ ������� �����
        if (cardBartok.Suit == TargetCard.Suit)
        {
            return true;
        }

        // ����� ������� false
        return false;
    }

    // ������ ��������� ����� �������
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

    // ������� Draw() ������� ������� ����� �� ������ ��������� ���� � ���������� �
    public CardBartok Draw()
    {
        CardBartok cd = DrawPile[0];   // ������� 0-�� �����

        if (DrawPile.Count == 0)   // ���� ������ DrawPile �������
        {
            // ... ����� ������������ ���������� ����� � ���������� �� � ������ ��������� ����
            int ndx;
            while (DiscardPile.Count > 0)
            {
                // ������ ��������� ����� �� ������ ���������� ����
                ndx = Random.Range(0, DiscardPile.Count);
                DrawPile.Add(DiscardPile[ndx]);
                DiscardPile.RemoveAt(ndx);
            }

            ArrangeDrawPile();
            // �������� ����������� ���� � ������ ��������� ����
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

        DrawPile.RemoveAt(0);          // ������� � �� ������ DrawPile
        return cd;                     // � �������
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
                // ����� ������� �����, �� ����������� ��, �� ������� �������� ������
                CardBartok cardBartok = CURRENT_PLAYER.AddCard(Draw());
                cardBartok.CallbackPlayer = CURRENT_PLAYER;
                Utils.tr("Bartok:CardClicked()", "Draw", cardBartok.name);
                Phase = TurnPhase.Waiting;
                break;

            case CBState.Hand:
                // ��������� ������������ ��������� �����
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
                    // ������������ ����� ������������ �����, �� �������� � ������� ������
                    Utils.tr("Bartok:CardClicked()", "Attempted to Play", tCardBartok.name, TargetCard.name + " is target");
                }
                break;
        }
    }



    // �����, �������� �������� �������������� ��������� ������������ ��������, ��������� � CultureInfo
    private void Culturator()
    {
        CultureInfo cultureInfo = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        CultureInfo.CurrentCulture = cultureInfo;
    }
}
