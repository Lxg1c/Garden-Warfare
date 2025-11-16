using UnityEngine;
using TMPro;

public class GameTimerUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    void Update()
    {
        if (GameTimer.Instance == null) return;

        double t = GameTimer.Instance.GameTime;

        int minutes = (int)(t / 60);
        int seconds = (int)(t % 60);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}