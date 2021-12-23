using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// CBState �������� ��������� ���� � ��������� to..., ����������� ��������
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
    // ����������� ���������� ��������� ������������ ����� ������������ CardBartok
    static public string MOVE_EASING = Easing.InOut;
    static public float MOVE_DURATION = 0.5f;
    static public float CARD_HEIGHT = 3.5f;
    static public float CARD_WIDTH = 2f;

    [Header("Set Dynamically: CardBartok")]
    public CBState State = CBState.Drawpile;

    // ���� � �����������, ����������� ��� ����������� � ������������� �����
    public List<Vector3> BezierPoints;
    public List<Quaternion> BezierRotations;
    public float TimeStart, TimeDuration;
    public int EventualSortOrder;
    public string EventualSortLayer;

    // �� ���������� ����������� ����� ����� ���������� ReportFinishTo.SendMessage()
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
                    //  ��������� �� ��������� to... � �������������� ��������� ���������
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

                    // ����������� � �������� ��������������
                    transform.localPosition = BezierPoints[BezierPoints.Count - 1];
                    transform.rotation = BezierRotations[BezierRotations.Count - 1];

                    // �������� TimeStart � 0, ����� � ��������� ���
                    // ����� ���� ���������� ������� �����
                    TimeStart = 0;

                    if (ReportFinishTo != null)
                    {
                        ReportFinishTo.SendMessage("CBCallback", this);
                        ReportFinishTo = null;
                    }
                    else if (CallbackPlayer != null)
                    {
                        // ���� ������� ������ �� ��������� Player
                        // ������� ����� CBCallback ����� ����������
                        CallbackPlayer.CBCallback(this);
                        CallbackPlayer = null;
                    }
                    else
                    {
                        // ���� ������ �������� �� ����, �������� �� ��� ����
                    }
                }
                else
                {
                    // ���������� ����� ������������ (0 <= u < 1)
                    Vector3 pos = Utils.Bezier(uC, BezierPoints);
                    transform.localPosition = pos;
                    Quaternion rotQ = Utils.Bezier(uC, BezierRotations);
                    transform.rotation = rotQ;

                    if (u > 0.5f)
                    {
                        SpriteRenderer sRend = SpriteRenderers[0];
                        if (sRend.sortingOrder !=EventualSortOrder)
                        {
                            // ���������� �������� ������� ����������
                            SetSortOrder(EventualSortOrder);
                        }
                        if (sRend.sortingLayerName != EventualSortLayer)
                        {
                            // ���������� �������� ���� ����������
                            SetSortingLayerName(EventualSortLayer);
                        }
                    }
                }

                break;
        }
    }

    // MoveTo ��������� ����������� ����� � ����� �������������� � �������� ���������
    public void MoveTo(Vector3 ePos, Quaternion eRot)
    {
        // ������� ����� ������ ��� ������������.
        // ���������� ����������� � �������� ������������ ����� ������� ������
        BezierPoints = new List<Vector3>();
        BezierPoints.Add(transform.localPosition);   // ������� ��������������
        BezierPoints.Add(ePos);                      // ����� ��������������

        BezierRotations = new List<Quaternion>();    
        BezierRotations.Add(transform.rotation);     // ������� ���� ��������
        BezierRotations.Add(eRot);                   // ����� ���� ��������

        if (TimeStart == 0)
        {
            TimeStart = Time.time;
        }
        // TimeDuration ������ �������� ���� � �� �� ��������, �� ����� ��� ����� ���������
        TimeDuration = MOVE_DURATION;
        State = CBState.To;
    }

    public void MoveTo(Vector3 ePos)
    {
        MoveTo(ePos, Quaternion.identity);
    }

    // ���� ����� ���������� ������� ����� �� ������ �����
    public override void OnMouseUpAsButton()
    {
        // ������� ����� CardClicked �������-�������� Bartok
        Bartok.S.CardClicked(this);
        // ����� ������� ������ ����� ������ � ������� ������ (Card.cs)
        base.OnMouseUpAsButton();
    }
}
