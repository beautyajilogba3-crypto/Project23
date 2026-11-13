
using UnityEngine;

public class WinManager : MonoBehaviour
{
    public GameObject victoryPanel;
    public AudioSource victoryAudio;

    private bool hasWon = false;

    void Start()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }

    public void ShowVictory()
    {
        if (hasWon) return;

        hasWon = true;

        // Stop the lockdown alarm
        GuardDetection.StopLockdownAlarm();

        // Play victory sound
        if (victoryAudio != null)
        {
            victoryAudio.Play();
        }

        // Show victory panel
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            victoryPanel.transform.SetAsLastSibling();
        }

        Debug.Log("PLAYER WON!");
    }
}
