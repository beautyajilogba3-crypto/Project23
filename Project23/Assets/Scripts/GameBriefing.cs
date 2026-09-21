using UnityEngine;

public class BriefingPanel : MonoBehaviour
{
    [SerializeField] GameObject briefingPanel;
    [SerializeField] GameObject hud;

    void Start()
    {
        // Show briefing when the game starts
        briefingPanel.SetActive(true);

        // Hide timer and counter
        hud.SetActive(false);

        // Pause the game
        Time.timeScale = 0f;
    }

    public void ContinueGame()
    {
        Debug.Log("CONTINUE WAS CLICKED!");

        // Hide briefing
        briefingPanel.SetActive(false);

        // Show timer and counter
        hud.SetActive(true);

        // Start the game
        Time.timeScale = 1f;
    }
}