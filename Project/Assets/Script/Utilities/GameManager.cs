using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Text accuracyText;
    public Text shotsFiredText;
    private int totalShots = 0;
    private int hits = 0;

    private void Awake()
    {
        // Ensure singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void IncreaseHitCount()
    {
        hits++;
        UpdateDisplayedText();
    }

    public void IncreaseShotCount()
    {
        totalShots++;
        UpdateDisplayedText();
    }

    private void UpdateDisplayedText()
    {
        if (totalShots > 0)
        {
            float accuracy = (float)hits / totalShots * 100f;
            accuracyText.text = $"Accuracy: {accuracy:F2}%";
            shotsFiredText.text = $"Shots fired: {totalShots}";
        }
    }
}
