using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    public float timeRemaining = 60f;
    public bool timerrunning = false;
    public TMP_Text timetext;   //this displays how many seconds are left
    public TMP_Text messageText;         //text ui to display when time is up

    public string mainmenu = "Main Menu";
    public float delay = 2f;        //transition delay between time up message and reutrn to main emnu

    [Tooltip("Message shown when the timer runs out (different from the guard-catch message).")]
    public string timeUpMessage = "You did not make it out with the artifact piece.               GAME OVER";

    private bool timerEnded = false;

    void Start()                        //timer is now running, clear the time-up-message text ui
    {
        timerrunning = true;

        if (messageText != null)
            messageText.text = "";
    }

    void Update()
    {
        if (timerrunning)
        {
            if (timeRemaining > 0)            //decrease time val by 1 if its not 0 yet
            {
                timeRemaining -= Time.deltaTime;    //delta time as the change in time

                DisplayTime(timeRemaining);
            }
            else
            {
                timeRemaining = 0;             //otherwise it has hit 0, update bool flag
                timerrunning = false;

                if (!timerEnded)              //call time-up function
                {
                    timerEnded = true;
                    TimeUp();
                }
            }

            if (timeRemaining <= 10f)         //time display getts bigger and turns red when its 10 sec till 0 sec

            {
                timetext.color = Color.red;
<<<<<<< Updated upstream
                timetext.fontSize = 85;
=======
<<<<<<< HEAD
                timetext.fontSize = 50;
=======
                timetext.fontSize = 85;
>>>>>>> 07dbc6e0bdeff4fd42c3f3c3396434b784485947
>>>>>>> Stashed changes
            }
            else
            {
                timetext.color = Color.darkGreen;
<<<<<<< Updated upstream
                timetext.fontSize = 85;
=======
<<<<<<< HEAD
                timetext.fontSize = 43;
=======
                timetext.fontSize = 85;
>>>>>>> 07dbc6e0bdeff4fd42c3f3c3396434b784485947
>>>>>>> Stashed changes
            }
        }
    }

    void DisplayTime(float timeToDisplay)              //function that update the text ui that displays time left. update occurs every second
    {
        timeToDisplay += 1;

        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (timetext != null)
            timetext.text = formattedTime;
        else
            Debug.Log(formattedTime);
    }

    void TimeUp()                   //time up fucntion
    {
        if (messageText != null)
            messageText.text = timeUpMessage;

        Debug.Log("Time's up! Now returning to main menu");

        Invoke(nameof(GoToMainMenu), delay);       //call delay and then loadscene to go back to main menu
    }

    void GoToMainMenu()
    {
        SceneManager.LoadScene(mainmenu);

        Cursor.lockState = CursorLockMode.None;          //after teleporting back to the menu, the cursor was gone and nothing could
        Cursor.visible = true;                     //be clicked. these 2 lines resolve that issue
    }

    public void StopTimer()                    //stop timer if player escaped with obj
    {
        timerrunning = false;
        CancelInvoke(nameof(GoToMainMenu));
    }
}