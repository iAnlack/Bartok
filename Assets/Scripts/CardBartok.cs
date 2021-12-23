using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// CBState включает состояния игры и состояния to..., описывающие движения
public enum CBState
{
    ToDrawpile,
    Drawpile,
    ToHand,
    Hand,
    ToTarget,
    Target,
    Discard,
    To,
    Idle
}

public class CardBartok : Card
{
    // Статические переменные совместно используются всеми экземплярами CardBartok
    static public string MOVE_EASING = Easing.InOut;
    static public float MOVE_DURATION = 0.5f;
    static public float CARD_HEIGHT = 3.5f;
    static public float CARD_WIDTH = 2f;

    [Header("Set Dynamically: CardBartok")]
    public CBState State = CBState.Drawpile;

    // Поля с информацией, необходимой для перемещения и поворачивания карты
    public List<Vector3> BezierPoints;
    public List<Quaternion> BezierRotations;
    public float TimeStart, TimeDuration;
    public int EventualSortOrder;
    public string EventualSortLayer;

    // По завершении перемещения карты будет вызываться ReportFinishTo.SendMessage()
    public GameObject ReportFinishTo = null;
    [System.NonSerialized]
    public Player CallbackPlayer = null;

    private void Update()
    {
        switch (State)
        {
            case CBState.ToHand:
            case CBState.ToTarget:
            case CBState.ToDrawpile:        
            case CBState.To:
                float u = (Time.time - TimeStart) / TimeDuration;
                float uC = Easing.Ease(u, MOVE_EASING);
                if (u < 0)
                {
                    transform.localPosition = BezierPoints[0];
                    transform.rotation = BezierRotations[0];
                    return;
                }
                else if (u >= 1)
                {
                    uC = 1;
                    //  Перевести из состояния to... в соответсвующее следующее состояние
                    if (State == CBState.ToHand)
                    {
                        State = CBState.Hand;
                    }
                    if (State == CBState.ToTarget)
                    {
                        State = CBState.Target;
                    }
                    if (State == CBState.ToDrawpile)
                    {
                        State = CBState.Drawpile;
                    }
                    if (State == CBState.To)
                    {
                        State = CBState.Idle;
                    }

                    // Переместить в конечное местоположение
                    transform.localPosition = BezierPoints[BezierPoints.Count - 1];
                    transform.rotation = BezierRotations[BezierRotations.Count - 1];

                    // Сбросить TimeStart в 0, чтобы в следующий раз
                    // можно было установить текущее время
                    TimeStart = 0;

                    if (ReportFinishTo != null)
                    {
                        ReportFinishTo.SendMessage("CBCallback", this);
                        ReportFinishTo = null;
                    }
                    else if (CallbackPlayer != null)
                    {
                        // Если имеется ссылка на экземпляр Player
                        // Вызвать метод CBCallback этого экземпляра
                        CallbackPlayer.CBCallback(this);
                        CallbackPlayer = null;
                    }
                    else
                    {
                        // Если ничего вызывать не надо, оставить всё как есть
                    }
                }
                else
                {
                    // Нормальный режим интерполяции (0 <= u < 1)
                    Vector3 pos = Utils.Bezier(uC, BezierPoints);
                    transform.localPosition = pos;
                    Quaternion rotQ = Utils.Bezier(uC, BezierRotations);
                    transform.rotation = rotQ;

                    if (u > 0.5f)
                    {
                        SpriteRenderer sRend = SpriteRenderers[0];
                        if (sRend.sortingOrder !=EventualSortOrder)
                        {
                            // Установить конечный порядок сортировки
                            SetSortOrder(EventualSortOrder);
                        }
                        if (sRend.sortingLayerName != EventualSortLayer)
                        {
                            // Установить конечный слой сортировки
                            SetSortingLayerName(EventualSortLayer);
                        }
                    }
                }

                break;
        }
    }

    // MoveTo запускает перемещение карты в новое местоположение с заданным поворотом
    public void MoveTo(Vector3 ePos, Quaternion eRot)
    {
        // Создать новые списки для интерполяции.
        // Траектории перемещения и поворота определяются двумя точками каждая
        BezierPoints = new List<Vector3>();
        BezierPoints.Add(transform.localPosition);   // Текущее местоположение
        BezierPoints.Add(ePos);                      // Новое местоположение

        BezierRotations = new List<Quaternion>();    
        BezierRotations.Add(transform.rotation);     // Текущий угол поворота
        BezierRotations.Add(eRot);                   // Новый угол поворота

        if (TimeStart == 0)
        {
            TimeStart = Time.time;
        }
        // TimeDuration всегда получает одно и то же значение, но потом это можно исправить
        TimeDuration = MOVE_DURATION;
        State = CBState.To;
    }

    public void MoveTo(Vector3 ePos)
    {
        MoveTo(ePos, Quaternion.identity);
    }

    // Этот метод определяет рекацию карты на щелчок мышью
    public override void OnMouseUpAsButton()
    {
        // Вызвать метод CardClicked объекта-одиночки Bartok
        Bartok.S.CardClicked(this);
        // Также вызвать версию этого метода в базовом классе (Card.cs)
        base.OnMouseUpAsButton();
    }
}
