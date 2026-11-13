
using UnityEngine;

public class WinManager : MonoBehaviour
{
    public GameObject victoryPanel;

    private bool hasWon = false;

    void Start()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    public void ShowVictory()
    {
        if (hasWon)
            return;

        hasWon = true;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        victoryPanel.SetActive(true);
        victoryPanel.transform.SetAsLastSibling();

        Debug.Log("PLAYER WON!");
    }
}
