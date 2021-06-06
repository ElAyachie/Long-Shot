using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Goal : MonoBehaviour
{
    public static int playerScore;
    public int defenderCount;
    public int[] levels;
    public static List<Defender> defenders;
    public Text tapToStartText;
    public Text bestScoreText;
    public Text scoreText;
    public Text lastScoreText;
    public static bool gameStarted;


    // Start is called before the first frame update
    void Start()
    {
        playerScore = 0;
        defenderCount = 0;
        levels = new int[4] { 0, 5, 10, 20};
        defenders = new List<Defender>();
        scoreText.GetComponent<Text>().enabled = false;
        bestScoreText.text = "Best score: " + PlayerPrefs.GetInt("HighScore");
        lastScoreText.text = "Last score: " + PlayerPrefs.GetInt("LastScore");
        gameStarted = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (gameStarted)
        {
            scoreText.GetComponent<Text>().enabled = true;
            tapToStartText.GetComponent<Text>().enabled = false;
            bestScoreText.GetComponent<Text>().enabled = false;
            lastScoreText.GetComponent<Text>().enabled = false;

        }

    }
    public void AddPoints()
    {

        foreach (int level in levels)
        {
            if (playerScore == level)
            {
                IncreaseDifficulty();
            }
        }
        playerScore++;

        if (playerScore > PlayerPrefs.GetInt("HighScore"))
        {
            PlayerPrefs.SetInt("HighScore", playerScore);
        }
        scoreText.text = "" + playerScore;
        
    }
    public void IncreaseDifficulty()
    {
        if (defenders[defenderCount] != null)
        {
            defenders[defenderCount].GetComponent<Defender>().alive = true;
            defenderCount++;
        }
    }
}
