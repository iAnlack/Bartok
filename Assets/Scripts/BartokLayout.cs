using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SlotDef
{
    public float X;
    public float Y;
    public bool FaceUp = false;
    public string LayerName = "Default";
    public int LayerID = 0;
    public int ID;
    public List<int> HiddenBy = new List<int>();   // Не используется в Bartok
    public float Rotation;                         // Поворот в зависимости от игрока
    public string Type = "slot";
    public Vector2 Stagger;
    public int Player;                             // Порядковый номер игрока
    public Vector3 Position;                       // Вычисляется на основе x, y и multiplier 
}

public class BartokLayout : MonoBehaviour
{
    [Header("Set Dynamically")]
    public PT_XMLReader XMLR;        // Так же, как Deck, имеет PT_XMLReader
    public PT_XMLHashtable XML;      // Используется для ускорения доступа к XML
    public Vector2 Multiplier;       // Смещение в раскладке
    // Ссылки на SlotDef
    public List<SlotDef> SlotDefs;   // Список SlotDef для игроков
    public SlotDef DrawPile;
    public SlotDef DiscardPile;
    public SlotDef Target;

    // Этот метод вызывается для чтения из файла BartokLayoutXML.xml
    public void ReadLayout(string xmlText)
    {
        XMLR = new PT_XMLReader();
        XMLR.Parse(xmlText);        // Загрузить XML
        XML = XMLR.xml["xml"][0];   // и определить XML для ускорения доступа к XML

        // Прочитать множители, определяющие расстояние между картами
        Multiplier.x = float.Parse(XML["multiplier"][0].att("x"));
        Multiplier.y = float.Parse(XML["multiplier"][0].att("y"));

        // Прочитать слоты
        SlotDef tSlotDef;
        // slotsX используется для ускорения длоступа к элементам <slot>
        PT_XMLHashList slotsX = XML["slot"];

        for (int i = 0; i < slotsX.Count; i++)
        {
            tSlotDef = new SlotDef(); // Создать новый экземпляр SlotDef
            if (slotsX[i].HasAtt("type"))
            {
                // Если <slot> имеет атрибут type, прочитать его
                tSlotDef.Type = slotsX[i].att("type");
            }
            else
            {
                // Иначе определить тип как "slot" - это отдельная карта в ряду
                tSlotDef.Type = "slot";
            }

            // Преобразовать некоторые атрибуты в числовые значения
            tSlotDef.X = float.Parse(slotsX[i].att("x"));
            tSlotDef.Y = float.Parse(slotsX[i].att("y"));
            tSlotDef.Position = new Vector3(tSlotDef.X * Multiplier.x, tSlotDef.Y * Multiplier.y, 0);

            // Слои сортировки
            tSlotDef.LayerID = int.Parse(slotsX[i].att("layer"));
            tSlotDef.LayerName = tSlotDef.LayerID.ToString();

            // Прочитать дополнительные атрибуты, опираясь на тип слота
            switch (tSlotDef.Type)
            {
                case "slot":
                    // Игнорировать слоты с типом "slot"
                    break;

                case "drawpile":
                    tSlotDef.Stagger.x = float.Parse(slotsX[i].att("xstagger"));
                    DrawPile = tSlotDef;
                    break;

                case "discardpile":
                    DiscardPile = tSlotDef;
                    break;

                case "target":
                    Target = tSlotDef;
                    break;

                case "hand":
                    tSlotDef.Player = int.Parse(slotsX[i].att("player"));
                    tSlotDef.Rotation = float.Parse(slotsX[i].att("rot"));
                    SlotDefs.Add(tSlotDef);
                    break;
            }
        }
    }
}
