using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;   // Подключает механизм запросов LINQ, о котором рассказывается ниже

// Игрок может быть человеком или ИИ
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
    public List<CardBartok> Hand;   // Карты в руках игрока

    // Добавляет карту в руки
    public CardBartok AddCard(CardBartok addedCard)
    {
        if (Hand == null)
        {
            Hand = new List<CardBartok>();
        }

        // Добавить карту
        Hand.Add(addedCard);

        // Если это человек, отсортировать карты по достоинству с помощью LINQ
        if (Type == PlayerType.Human)
        {
            CardBartok[] cards = Hand.ToArray();

            // Это вызов LINQ
            cards = cards.OrderBy(card => card.Rank).ToArray();

            Hand = new List<CardBartok>(cards);

            // Примечание: LINQ выполняет операции довольно медленно (затрачивая
            // по несколько мс), но т.к. мы делаем это один раз за раунд - норм
        }

        addedCard.SetSortingLayerName("10");   // Перенести перемещаемую карту в верхний слой
        addedCard.EventualSortLayer = HandSlotDef.LayerName;

        FanHand();
        return addedCard;
    }

    // Удаляет карту из рук
    public CardBartok RemoveCard(CardBartok removedCard)
    {
        // Если список Hand пуст или не содержит карты removedCard, вернуть null
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
        // startRotation - угол поворота первой карты относительно оси Z
        float startRotation = 0;
        startRotation = HandSlotDef.Rotation;
        if (Hand.Count > 1)
        {
            startRotation += Bartok.S.HandFanDegrees * (Hand.Count - 1) / 2;
        }

        // Переместить все карты в новые позиции
        Vector3 position;
        float rotation;
        Quaternion rotationQ;
        for (int i = 0; i < Hand.Count; i++)
        {
            rotation = startRotation - Bartok.S.HandFanDegrees * i;
            rotationQ = Quaternion.Euler(0, 0, rotation);

            position = Vector3.up * CardBartok.CARD_HEIGHT / 2f;
            position = rotationQ * position;

            // Прибавить координаты позиции руки игрока (внизу в центре веера карт)
            position += HandSlotDef.Position;
            position.z = -0.5f * i;

            // Если это не начальная раздача, начать перемещение карты немедленно
            if (Bartok.S.Phase != TurnPhase.Idle)
            {
                Hand[i].TimeStart = 0;
            }

            // Установить локальную позицию и поворот i-ой карты в руках
            Hand[i].MoveTo(position, rotationQ);   // Сообщить карте, что она
                                                   // должна начать интерполяцию
            Hand[i].State = CBState.ToHand;
            // Закончив перемещение, карта запишет в поле State значение CBState.Hand

            /*
            Hand[i].transform.localPosition = position;
            Hand[i].transform.rotation = rotationQ;
            Hand[i].State = CBState.Hand;
            */

            Hand[i].FaceUp = (Type == PlayerType.Human);

            // Установить SortOrder карт, чтобы обеспечить правильное перекрытие
            Hand[i].EventualSortOrder = i * 4;
            //Hand[i].SetSortOrder(i * 4);
        }
    }

    // Фунция TakeTurn реализует ИИ для игроков, управляемых компьютером
    public void TakeTurn()
    {
        Utils.tr("Player.TakeTurn");

        // Ничего не делать для игрока-человека
        if (Type == PlayerType.Human)
        {
            return;
        }

        Bartok.S.Phase = TurnPhase.Waiting;

        CardBartok cardBartok;

        // Если этим игроком управляет компьютер, нужно выбрать карту для хода
        // Найти допустимые ходы
        List<CardBartok> validCards = new List<CardBartok>();
        foreach (CardBartok tCardBartok in Hand)
        {
            if (Bartok.S.ValidPlay(tCardBartok))
            {
                validCards.Add(tCardBartok);
            }
        }

        // Если допустимых ходов нет
        if (validCards.Count == 0)
        {
            // ... взять карту
            cardBartok = AddCard(Bartok.S.Draw());
            cardBartok.CallbackPlayer = this;
            return;
        }

        // Итак, у нас есть одна или несколько карт, которыми можно сыграть
        // теперь нужно выбрать одну из них
        cardBartok = validCards[Random.Range(0, validCards.Count)];
        RemoveCard(cardBartok);
        Bartok.S.MoveToTarget(cardBartok);
        cardBartok.CallbackPlayer = this;
    }

    public void CBCallback(CardBartok cardBartok)
    {
        Utils.tr("Player.CBCallback()", cardBartok.name, "Player " + PlayerNum);
        // Карта завершила перемещение, передать право хода
        Bartok.S.PassTurn();
    }
}
