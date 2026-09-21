using UnityEngine;
using TMPro;

public class PickupPrompt : MonoBehaviour
{
    public TextMeshProUGUI promptText;

    public void ShowPrompt()
    {
        promptText.gameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        promptText.gameObject.SetActive(false);
    }
}