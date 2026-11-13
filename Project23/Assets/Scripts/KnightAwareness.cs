
using UnityEngine;

public class KnightAwareness : MonoBehaviour
{
    public GameObject warningText;
    public Light knightSpotlight;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            warningText.SetActive(true);
            knightSpotlight.enabled = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            warningText.SetActive(false);
            knightSpotlight.enabled = false;
        }
    }
}
