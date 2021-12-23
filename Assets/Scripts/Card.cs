using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    [Header("Set Dynamically")]
    public string Suit;                 // Масть карты (C, D, H или S)
    public int Rank;                    // Достоинство карты (1-14)
    public Color Color = Color.black;   // Цвет значков
    public string ColS = "Black";       // (Black/Red) - имя цвета

    // Этот список хранит все игровые объекты Decorator
    public List<GameObject> DecoGOs = new List<GameObject>();
    // Этот список хранит все игровые объекты Pip
    public List<GameObject> PipGOs = new List<GameObject>();
    // Список компонентов SpriteRenderer этого и вложенных в него игровых объектов
    public SpriteRenderer[] SpriteRenderers;

    public GameObject Back;             // Игровой объект рубашки карты
    public CardDefinition Definition;   // Извлекается из DeckXML.xml

    public bool FaceUp
    {
        get
        {
            return (!Back.activeSelf);
        }
        set
        {
            Back.SetActive(!value);
        }
    }

    private void Start()
    {
        SetSortOrder(0);   // Обеспечит правильную сортировку карт
    }

    // Если SpriteRenderers не определён, эта функция определит его
    public void PopulateSpriteRenderers()
    {
        // Если SpriteRenderers содержит null или пустой список
        if (SpriteRenderers == null || SpriteRenderers.Length == 0)
        {
            // Получить компоненты SpriteRenderer этого игрового объекта и вложенных в него игровых объектов
            SpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }
    }

    // Инициализирует поле sortingLayerName во всех компонентах SpriteRenderer
    public void SetSortingLayerName(string tSLN)
    {
        PopulateSpriteRenderers();

        foreach (SpriteRenderer tSpriteRenderer in SpriteRenderers)
        {
            tSpriteRenderer.sortingLayerName = tSLN;
        }
    }

    // Инициализирует поле sortingOrder всех компонентов SpriteRenderer
    public void SetSortOrder(int sOrd)
    {
        PopulateSpriteRenderers();

        // Выполнить обход всех элементов в списке SpriteRenderers
        foreach (SpriteRenderer tSpriteRenderer in SpriteRenderers)
        {
            if (tSpriteRenderer.gameObject == this.gameObject)
            {
                // Если компонент принадлежит к текущему игровому объекту, это фон
                tSpriteRenderer.sortingOrder = sOrd; // Установить порядковый номер для сортировки в sOrd
                continue;                            // И перейти к следующей итерации цикла
            }

            // Каждый дочерний игровой объект имеет имя
            // Установить порядковый номер для сортировки, в зависимости от имени
            switch (tSpriteRenderer.gameObject.name)
            {
                case "back":
                    // Установить наибольший порядковый номер для отображения поверх других спрайтов
                    tSpriteRenderer.sortingOrder = sOrd + 2;
                    break;

                case "face": // Если имя "face"
                default:     // или же другое
                    // Установить промежуточный порядковый номер для отображения поверх фона
                    tSpriteRenderer.sortingOrder = sOrd + 1;
                    break;
            }
        }
    }

    // Виртуальные методы могут переопределяться в подклассах определением методов с теми же именами
    virtual public void OnMouseUpAsButton()
    {
        Debug.Log(name);   // По щелчку эта строка выведет имя карты
    }
}

[System.Serializable] // Сериализуемый класс доступен для правки в инспекторе
public class Decorator
{
    // Этот класс хранит информацию из DeckXML о каждом значке на карте
    public string Type;         // Значок, определяющий достоинство карты, имеет type "pip"
    public Vector3 Loc;         // Местоположение спрайта на карте
    public float Scale = 1f;    // Масштаб спрайта
    public bool Flip = false;   // Признак переворода спрайта по вертикали
}

[System.Serializable]
public class CardDefinition
{
    // Этот класс хранит информацию о достоинстве карты
    public string Face;                                    // Спрайт, изображающий лицевую сторону карты
    public int Rank;                                       // Достоинство карты (1-13)
    public List<Decorator> Pips = new List<Decorator>();   // Значки
}
