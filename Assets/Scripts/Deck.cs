using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    [Header("Set in Inspector")]
    public bool StartFaceUp = false;
    // Масти
    public Sprite SuitClub;
    public Sprite SuitDiamond;
    public Sprite SuitHeart;
    public Sprite SuitSpade;

    public Sprite[] FaceSprites;
    public Sprite[] RankSprites;

    public Sprite CardBack;
    public Sprite CardBackGold;
    public Sprite CardFront;
    public Sprite CardFrontGold;

    // Шаблоны
    public GameObject PrefabCard;
    public GameObject PrefabSprite;

    [Header("Set Dynamically")]
    public PT_XMLReader XMLR;
    public List<string> CardNames;
    public List<Card> Cards;
    public List<Decorator> Decorators;
    public List<CardDefinition> CardDefinitions;
    public Transform DeckAnchor;
    public Dictionary<string, Sprite> DictionarySuits;

    // InitDeck вызывается экземпляром Prospector, когда будет готов
    public void InitDeck(string deckXMLText)
    {
        // Создать точку привязки для всех игровых объектов Card в иерархии
        if (GameObject.Find("Deck") == null)
        {
            GameObject anchorGO = new GameObject("Deck");
            DeckAnchor = anchorGO.transform;
        }

        // Инициализировать словарь со спрайтами значков мастей
        DictionarySuits = new Dictionary<string, Sprite>()
        {
            {"C", SuitClub },
            {"D", SuitDiamond },
            {"H", SuitHeart },
            {"S", SuitSpade }
        };

        ReadDeck(deckXMLText);

        MakeCards();
    }

    // ReadDeck читает указанный XML-файл и создаёт массив экземпляров CardDefinition
    public void ReadDeck(string deckXMLText)
    {
        XMLR = new PT_XMLReader();   // Создать новый экземпляр PT_XMLReader
        XMLR.Parse(deckXMLText);     // Использовать его для  чтения DeckXML

        // Вывод проверочной строки, чтобы показать, как использовать XMLR.
        // За дополнительной информацией о чтении файлов XML обращайтесь к приложению "Полезные идеи"
        string s = "xml[0] decorator[0] ";
        s += "type=" + XMLR.xml["xml"][0]["decorator"][0].att("type");
        s += " x=" + XMLR.xml["xml"][0]["decorator"][0].att("x");
        s += " y=" + XMLR.xml["xml"][0]["decorator"][0].att("y");
        s += " scale=" + XMLR.xml["xml"][0]["decorator"][0].att("scale");
        //print(s);

        // Прочитать элементы <Decorator> для всех карт
        Decorators = new List<Decorator>(); // Инициализировать список экземпляров Decorator
        // Извлечь список PT_XMLHashList всех элементов <Decorator> из XML-файла
        PT_XMLHashList xDecos = XMLR.xml["xml"][0]["decorator"];
        Decorator decorator;
        for (int i = 0; i < xDecos.Count; i++)
        {
            // Для каждого элемента <Decorator> в XML
            decorator = new Decorator(); // Создать экземпляр Decorator
            // Скопировать атрибуты из <Decorator> в Decorator
            decorator.Type = xDecos[i].att("type");
            // decorator.Flip получит значение true, если аттрибут flip содержит текст "1"
            decorator.Flip = (xDecos[i].att("flip") == "1");
            // Получить значения float из строковых атрибутов
            decorator.Scale = float.Parse(xDecos[i].att("scale"));
            // Vector3 Loc инициализируется как [0, 0, 0], поэтому нам остаётся только изменить его
            decorator.Loc.x = float.Parse(xDecos[i].att("x"));
            decorator.Loc.y = float.Parse(xDecos[i].att("y"));
            decorator.Loc.z = float.Parse(xDecos[i].att("z"));
            // Добавить decorator в список Decorators
            Decorators.Add(decorator);
        }

        // Прочитать координаты для значков, определяющих достоинство карты
        CardDefinitions = new List<CardDefinition>(); // Инициализировать список карт
        // Извлечь список PT_XMLHashList всех элементов <Card> из XML-файла
        PT_XMLHashList xCardDefinitions = XMLR.xml["xml"][0]["card"];
        for (int i = 0; i < xCardDefinitions.Count; i++)
        {
            // Для каждого элемента <Card> создать экземпляр CardDefinition
            CardDefinition cDef = new CardDefinition();
            // Получить значения атрибута и добавить их в cDef
            cDef.Rank = int.Parse(xCardDefinitions[i].att("rank"));
            // Извлечь список PT_XMLHashList всех элементов <pip> внутри этого элемента <Card>
            PT_XMLHashList xPips = xCardDefinitions[i]["pip"];
            if (xPips != null)
            {
                for (int j = 0; j < xPips.Count; j++)
                {
                    // Обойти все элементы <pip>
                    decorator = new Decorator();
                    // Элементы <pip> в <Card> обрабатываются классом Decorator
                    decorator.Type = "pip";
                    decorator.Flip = (xPips[j].att("flip") == "1");
                    decorator.Loc.x = float.Parse(xPips[j].att("x"));
                    decorator.Loc.y = float.Parse(xPips[j].att("y"));
                    decorator.Loc.z = float.Parse(xPips[j].att("z"));
                    if (xPips[j].HasAtt("scale"))
                    {
                        decorator.Scale = float.Parse(xPips[j].att("scale"));
                    }
                    cDef.Pips.Add(decorator);
                }
            }

            // Карты с картинками (Валет, Дама, Король) имеют атрибут face
            if (xCardDefinitions[i].HasAtt("face"))
            {
                cDef.Face = xCardDefinitions[i].att("face");
            }
            CardDefinitions.Add(cDef);
        }
    }

    // Получает CardDefinition на основе значения достоинства (от 1 до 14 - от туза до короля)
    public CardDefinition GetCardDefinitionByRank(int rank)
    {
        // Поиск во всех определениях CardDefinition
        foreach (CardDefinition cardDefinition in CardDefinitions)
        {
            // Если достоинство совпадает вернуть это определение
            if (cardDefinition.Rank == rank)
            {
                return (cardDefinition);
            }
        }

        return null;
    }

    // Создаёт игровые объекты карт
    public void MakeCards()
    {
        // CardNames будет содержать имена сконструированных карт
        // Каждая масть имеет 14 значений достоинства (например для треф (Clubs): от C1 до C14)
        CardNames = new List<string>();
        string[] letters = new string[] { "C", "D", "H", "S" };
        foreach (string s in letters)
        {
            for (int i = 0; i < 13; i++)
            {
                CardNames.Add(s + (i + 1));
            }
        }

        // Создать список со всеми картами
        Cards = new List<Card>();

        // Обойти все только что созданные имена карт
        for (int i = 0; i < CardNames.Count; i++)
        {
            // Создать карту и добавить её в колоду
            Cards.Add(MakeCard(i));
        }
    }

    private Card MakeCard(int cNum)
    {
        // Создать новый игровой объект с картой
        GameObject cGO = Instantiate(PrefabCard) as GameObject;
        // Настроить transform.parent новой карты в соответствии с точкой привязки
        cGO.transform.parent = DeckAnchor;
        Card card = cGO.GetComponent<Card>(); // Получить компонент Card

        // Эта строка выкладывает карты в аккуратный ряд
        cGO.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);

        // Настроить основные параметры карты
        card.name = CardNames[cNum];
        card.Suit = card.name[0].ToString();
        card.Rank = int.Parse(card.name.Substring(1));
        if (card.Suit == "D" || card.Suit == "H")
        {
            card.ColS = "Red";
            card.Color = Color.red;
        }

        // Получить CardDefinition для этой карты
        card.Definition = GetCardDefinitionByRank(card.Rank);

        AddDecorators(card);
        AddPips(card);
        AddFace(card);
        AddBack(card);

        return card;
    }

    // Следующие скрытые переменные используются вспомогательными методами
    private Sprite _tSprite = null;
    private GameObject _tGameObject = null;
    private SpriteRenderer _tSpriteRenderer = null;

    private void AddDecorators(Card card)
    {
        // Добавить оформление
        foreach (Decorator decorator in Decorators)
        {
            if (decorator.Type == "Suit")
            {
                // Создать экземпляр игрового объекта спрайта
                _tGameObject = Instantiate(PrefabSprite) as GameObject;
                // Получить ссылку на компонент SpriteRenderer
                _tSpriteRenderer = _tGameObject.GetComponent<SpriteRenderer>();
                // Установить спрайт масти
                _tSpriteRenderer.sprite = DictionarySuits[card.Suit];
            }
            else
            {
                _tGameObject = Instantiate(PrefabSprite) as GameObject;
                _tSpriteRenderer = _tGameObject.GetComponent<SpriteRenderer>();
                // Получить спрайт для отображения достоинства
                _tSprite = RankSprites[card.Rank];
                // Установить спрайт достоинства в SpriteRenderer
                _tSpriteRenderer.sprite = _tSprite;
                // Установить цвет, соответствующий масти
                _tSpriteRenderer.color = card.Color;
            }

            // Поместить спрайты над картой
            _tSpriteRenderer.sortingOrder = 1;
            // Сделать спрайт дочерним по отношению к карте
            _tGameObject.transform.SetParent(card.transform);
            // Установить localPosition, как определено в DeckXML
            _tGameObject.transform.localPosition = decorator.Loc;
            // Перевернуть значок, если необходимо
            if (decorator.Flip)
            {
                // Эйлеров поворот на 180° относительно оси z-axis
                _tGameObject.transform.rotation = Quaternion.Euler(0, 0, 180);
            }

            // Установить масштаб, чтобы уменьшить размер спрайта
            if (decorator.Scale != 1)
            {
                _tGameObject.transform.localScale = Vector3.one * decorator.Scale;
            }

            // Дать имя этому игровому объекту для наглядности
            _tGameObject.name = decorator.Type;
            // Добавить этот игровой объект с оформлением в список card.DecoGOs
            card.DecoGOs.Add(_tGameObject);
        }
    }

    private void AddPips(Card card)
    {
        // Для каждого значка в определении...
        foreach (Decorator pip in card.Definition.Pips)
        {
            // ... Создать игровой объект спрайта
            _tGameObject = Instantiate(PrefabSprite) as GameObject;
            // Назначить родителем игровой объект карты
            _tGameObject.transform.SetParent(card.transform);
            // Установить localPosititon, как определено в XML-файле
            _tGameObject.transform.localPosition = pip.Loc;

            // Перевернуть, если необходимо
            if (pip.Flip)
            {
                _tGameObject.transform.rotation = Quaternion.Euler(0, 0, 180);
            }

            // Масштабировать, если необходимо (только для туза)
            if (pip.Scale != 1)
            {
                _tGameObject.transform.localScale = Vector3.one * pip.Scale;
            }

            // Дать имя игровому объекту
            _tGameObject.name = "pip";
            // Получить ссылку на компонент SpriteRenderer
            _tSpriteRenderer = _tGameObject.GetComponent<SpriteRenderer>();
            // Установить спрайт масти
            _tSpriteRenderer.sprite = DictionarySuits[card.Suit];
            // Установить sortingOrder, чтобы значок отображался над Card_Front
            _tSpriteRenderer.sortingOrder = 1;
            // Добавить игровой объект в список значков
            card.PipGOs.Add(_tGameObject);
        }
    }

    private void AddFace(Card card)
    {
        if (card.Definition.Face == "")
        {
            return; // Выйти, если это не карта с картинкой
        }

        _tGameObject = Instantiate(PrefabSprite) as GameObject;
        _tSpriteRenderer = _tGameObject.GetComponent<SpriteRenderer>();
        // Сгенерировать имя и передать его в GetFace()
        _tSprite = GetFace(card.Definition.Face + card.Suit);
        _tSpriteRenderer.sprite = _tSprite;   // Установить этот спрайт в _tSpriteRenderer
        _tSpriteRenderer.sortingOrder = 1;    // Установить sortingOrder
        _tGameObject.transform.SetParent(card.transform);
        _tGameObject.transform.localPosition = Vector3.zero;
        _tGameObject.name = "face";
    }

    // Находит спрайт с картинкой для карты
    private Sprite GetFace(string faceS)
    {
        foreach (Sprite _tSpriteP in FaceSprites)
        {
            // Если найден спрайт с требуемым именем...
            if (_tSpriteP.name == faceS)
            {
                // ... вернуть его
                return _tSpriteP;
            }
        }

        // Если ничего не найдено, вернуть null
        return null;
    }

    private void AddBack(Card card)
    {
        // Добавить рубашку
        // Card_Back будет покрывать всё остальное на карте
        _tGameObject = Instantiate(PrefabSprite) as GameObject;
        _tSpriteRenderer = _tGameObject.GetComponent<SpriteRenderer>();
        _tSpriteRenderer.sprite = CardBack;
        _tGameObject.transform.SetParent(card.transform);
        _tGameObject.transform.localPosition = Vector3.zero;
        // Большее значение sortingOrder, чем у других спрайтов
        _tSpriteRenderer.sortingOrder = 2;
        _tGameObject.name = "back";
        card.Back = _tGameObject;

        // По умолчанию картинкой вверх
        card.FaceUp = StartFaceUp; // Использовать свойство FaceUp карты
    }

    // Перемешивает карты в Deck.Cards
    static public void Shuffle(ref List<Card> oCards)
    {
        // Создать временный список для хранения карт в перемешанном порядке
        List<Card> tCards = new List<Card>();

        int ndx; // Будет хранить индекс перемещаемой карты
        tCards = new List<Card>(); // Инициализировать временный список
        // Повторять, пока не будут перемещены все карты в исходном списке
        while (oCards.Count > 0)
        {
            // Выбрать случайный индекс карть
            ndx = Random.Range(0, oCards.Count);
            // Добавить эту карту во временный список...
            tCards.Add(oCards[ndx]);
            // ... и удалить карту из исходного списка
            oCards.RemoveAt(ndx);
        }

        // Заменить исходный список временным
        oCards = tCards;
        // Т.к. oCards - это параметр-ссылка (ref), оригинальный аргумент, переданный в метод, тоже изменится
    }
}
