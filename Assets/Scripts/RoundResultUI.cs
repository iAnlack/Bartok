using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoundResultUI : MonoBehaviour
{
    private Text _text;

    private void Awake()
    {
        _text = GetComponent<Text>();
        _text.text = "";
    }

    private void Update()
    {
        if (Bartok.S.Phase != TurnPhase.GameOver)
        {
            _text.text = "";
            return;
        }

        // В эту точку мы попадём только когда игра завершилась
        Player player = Bartok.CURRENT_PLAYER;
        if (player == null || player.Type == PlayerType.Human)
        {
            _text.text = "";
        }
        else
        {
            _text.text = "Player " + (player.PlayerNum) + " won";
        }
    }
}
