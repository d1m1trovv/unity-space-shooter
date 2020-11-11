using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Sprite[] lives;

    public Image livesImageDisplay;

    public int score;

    public Text scoreText;

    [SerializeField]
    private GameObject _title;

    private void Update()
    {
        
    }

    public void UpdateLives(int currentLives)
    {
        livesImageDisplay.sprite = lives[currentLives];
    }

    public void UpdateScore()
    {
        score += 10;

        scoreText.text = "Score: " + score;
    }

    public void ShowTitleScreen()
    {
        _title.SetActive(true);
    }

    public void HideTitleScreen()
    {
        _title.SetActive(false);
        scoreText.text = "Score: ";
    }
}
