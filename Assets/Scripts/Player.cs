using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;   // ���������� �������� �������� LINQ, � ������� �������������� ����

// ����� ����� ���� ��������� ��� ��
public enum PlayerType
{
    Human,
    AI
}

[System.Serializable]
public class Player
{
    public PlayerType Type = PlayerType.AI;
    public int PlayerNum;
    public SlotDef HandSlotDef;
    public List<CardBartok> Hand;   // ����� � ����� ������

    // ��������� ����� � ����
    public CardBartok AddCard(CardBartok addedCard)
    {
        if (Hand == null)
        {
            Hand = new List<CardBartok>();
        }

        // �������� �����
        Hand.Add(addedCard);

        // ���� ��� �������, ������������� ����� �� ����������� � ������� LINQ
        if (Type == PlayerType.Human)
        {
            CardBartok[] cards = Hand.ToArray();

            // ��� ����� LINQ
            cards = cards.OrderBy(card => card.Rank).ToArray();

            Hand = new List<CardBartok>(cards);

            // ����������: LINQ ��������� �������� �������� �������� (����������
            // �� ��������� ��), �� �.�. �� ������ ��� ���� ��� �� ����� - ����
        }

        addedCard.SetSortingLayerName("10");   // ��������� ������������ ����� � ������� ����
        addedCard.EventualSortLayer = HandSlotDef.LayerName;

        FanHand();
        return addedCard;
    }

    // ������� ����� �� ���
    public CardBartok RemoveCard(CardBartok removedCard)
    {
        // ���� ������ Hand ���� ��� �� �������� ����� removedCard, ������� null
        if (Hand == null || !Hand.Contains(removedCard))
        {
            return null;
        }

        Hand.Remove(removedCard);
        FanHand();
        return removedCard;
    }

    public void FanHand()
    {
        // startRotation - ���� �������� ������ ����� ������������ ��� Z
        float startRotation = 0;
        startRotation = HandSlotDef.Rotation;
        if (Hand.Count > 1)
        {
            startRotation += Bartok.S.HandFanDegrees * (Hand.Count - 1) / 2;
        }

        // ����������� ��� ����� � ����� �������
        Vector3 position;
        float rotation;
        Quaternion rotationQ;
        for (int i = 0; i < Hand.Count; i++)
        {
            rotation = startRotation - Bartok.S.HandFanDegrees * i;
            rotationQ = Quaternion.Euler(0, 0, rotation);

            position = Vector3.up * CardBartok.CARD_HEIGHT / 2f;
            position = rotationQ * position;

            // ��������� ���������� ������� ���� ������ (����� � ������ ����� ����)
            position += HandSlotDef.Position;
            position.z = -0.5f * i;

            // ���� ��� �� ��������� �������, ������ ����������� ����� ����������
            if (Bartok.S.Phase != TurnPhase.Idle)
            {
                Hand[i].TimeStart = 0;
            }

            // ���������� ��������� ������� � ������� i-�� ����� � �����
            Hand[i].MoveTo(position, rotationQ);   // �������� �����, ��� ���
                                                   // ������ ������ ������������
            Hand[i].State = CBState.ToHand;
            // �������� �����������, ����� ������� � ���� State �������� CBState.Hand

            /*
            Hand[i].transform.localPosition = position;
            Hand[i].transform.rotation = rotationQ;
            Hand[i].State = CBState.Hand;
            */

            Hand[i].FaceUp = (Type == PlayerType.Human);

            // ���������� SortOrder ����, ����� ���������� ���������� ����������
            Hand[i].EventualSortOrder = i * 4;
            //Hand[i].SetSortOrder(i * 4);
        }
    }

    // ������ TakeTurn ��������� �� ��� �������, ����������� �����������
    public void TakeTurn()
    {
        Utils.tr("Player.TakeTurn");

        // ������ �� ������ ��� ������-��������
        if (Type == PlayerType.Human)
        {
            return;
        }

        Bartok.S.Phase = TurnPhase.Waiting;

        CardBartok cardBartok;

        // ���� ���� ������� ��������� ���������, ����� ������� ����� ��� ����
        // ����� ���������� ����
        List<CardBartok> validCards = new List<CardBartok>();
        foreach (CardBartok tCardBartok in Hand)
        {
            if (Bartok.S.ValidPlay(tCardBartok))
            {
                validCards.Add(tCardBartok);
            }
        }

        // ���� ���������� ����� ���
        if (validCards.Count == 0)
        {
            // ... ����� �����
            cardBartok = AddCard(Bartok.S.Draw());
            cardBartok.CallbackPlayer = this;
            return;
        }

        // ����, � ��� ���� ���� ��� ��������� ����, �������� ����� �������
        // ������ ����� ������� ���� �� ���
        cardBartok = validCards[Random.Range(0, validCards.Count)];
        RemoveCard(cardBartok);
        Bartok.S.MoveToTarget(cardBartok);
        cardBartok.CallbackPlayer = this;
    }

    public void CBCallback(CardBartok cardBartok)
    {
        Utils.tr("Player.CBCallback()", cardBartok.name, "Player " + PlayerNum);
        // ����� ��������� �����������, �������� ����� ����
        Bartok.S.PassTurn();
    }
}
