using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
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
        if (Bartok.CURRENT_PLAYER == null)
        {
            return;
        }

        if (Bartok.CURRENT_PLAYER.Type == PlayerType.Human)
        {
            _text.text = "You won!";
        }
        else
        {
            _text.text = "Game Over";
        }
    }
}
