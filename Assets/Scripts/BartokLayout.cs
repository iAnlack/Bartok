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
    public List<int> HiddenBy = new List<int>();   // �� ������������ � Bartok
    public float Rotation;                         // ������� � ����������� �� ������
    public string Type = "slot";
    public Vector2 Stagger;
    public int Player;                             // ���������� ����� ������
    public Vector3 Position;                       // ����������� �� ������ x, y � multiplier 
}

public class BartokLayout : MonoBehaviour
{
    [Header("Set Dynamically")]
    public PT_XMLReader XMLR;        // ��� ��, ��� Deck, ����� PT_XMLReader
    public PT_XMLHashtable XML;      // ������������ ��� ��������� ������� � XML
    public Vector2 Multiplier;       // �������� � ���������
    // ������ �� SlotDef
    public List<SlotDef> SlotDefs;   // ������ SlotDef ��� �������
    public SlotDef DrawPile;
    public SlotDef DiscardPile;
    public SlotDef Target;

    // ���� ����� ���������� ��� ������ �� ����� BartokLayoutXML.xml
    public void ReadLayout(string xmlText)
    {
        XMLR = new PT_XMLReader();
        XMLR.Parse(xmlText);        // ��������� XML
        XML = XMLR.xml["xml"][0];   // � ���������� XML ��� ��������� ������� � XML

        // ��������� ���������, ������������ ���������� ����� �������
        Multiplier.x = float.Parse(XML["multiplier"][0].att("x"));
        Multiplier.y = float.Parse(XML["multiplier"][0].att("y"));

        // ��������� �����
        SlotDef tSlotDef;
        // slotsX ������������ ��� ��������� �������� � ��������� <slot>
        PT_XMLHashList slotsX = XML["slot"];

        for (int i = 0; i < slotsX.Count; i++)
        {
            tSlotDef = new SlotDef(); // ������� ����� ��������� SlotDef
            if (slotsX[i].HasAtt("type"))
            {
                // ���� <slot> ����� ������� type, ��������� ���
                tSlotDef.Type = slotsX[i].att("type");
            }
            else
            {
                // ����� ���������� ��� ��� "slot" - ��� ��������� ����� � ����
                tSlotDef.Type = "slot";
            }

            // ������������� ��������� �������� � �������� ��������
            tSlotDef.X = float.Parse(slotsX[i].att("x"));
            tSlotDef.Y = float.Parse(slotsX[i].att("y"));
            tSlotDef.Position = new Vector3(tSlotDef.X * Multiplier.x, tSlotDef.Y * Multiplier.y, 0);

            // ���� ����������
            tSlotDef.LayerID = int.Parse(slotsX[i].att("layer"));
            tSlotDef.LayerName = tSlotDef.LayerID.ToString();

            // ��������� �������������� ��������, �������� �� ��� �����
            switch (tSlotDef.Type)
            {
                case "slot":
                    // ������������ ����� � ����� "slot"
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
