
using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public float timeRemaining = 300f;
    public bool timerrunning = false;

    public TMP_Text timetext;
    public TMP_Text messageText;

    public string mainmenu = "Main Menu";
    public float delay = 2f;
    public GameObject deathParticle;

    public GameOverManager gameOverManager;

    [Tooltip("Message shown when the timer runs out.")]
    public string timeUpMessage =
        "You did not make it out with the artifact piece. GAME OVER";

    private bool timerEnded = false;

    void Start()
    {
        timerrunning = true;

        if (messageText != null)
            messageText.text = "";

        if (timetext != null)
            DisplayTime(timeRemaining);
    }

    void Update()
    {
        if (!timerrunning)
            return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining < 0)
                timeRemaining = 0;

            DisplayTime(timeRemaining);
        }
        else
        {
            timeRemaining = 0;
            timerrunning = false;

            if (!timerEnded)
            {
                timerEnded = true;
                TimeUp();
            }
        }

        if (timetext != null)
        {
            if (timeRemaining <= 10f)
            {
                timetext.color = Color.red;
                timetext.fontSize = 77;
            }
            else
            {
                timetext.color = Color.green;
                timetext.fontSize = 64;
            }
        }
    }

    void DisplayTime(float timeToDisplay)
    {
        timeToDisplay = Mathf.Max(0, timeToDisplay);

        int minutes = Mathf.FloorToInt(timeToDisplay / 60);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60);

        string formattedTime =
            string.Format("{0:00}:{1:00}", minutes, seconds);

        if (timetext != null)
            timetext.text = formattedTime;
    }

    void TimeUp()
    {
        if (messageText != null)
            messageText.text = timeUpMessage;

        if (deathParticle != null)
            deathParticle.SetActive(true);

        timerrunning = false;

        if (gameOverManager != null)
        {
            gameOverManager.ShowGameOver();
        }
        else
        {
            Debug.LogError(
                "GameOverManager is not assigned in the Timer Inspector!"
            );
        }
    }

    public void StopTimer()
    {
        timerrunning = false;
        CancelInvoke(nameof(GoToMainMenu));
    }

    void GoToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainmenu);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
