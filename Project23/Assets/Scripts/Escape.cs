using UnityEngine;

public class DoorExit : MonoBehaviour
{
    public Timer timer;              //drag your Timer object here in the inspector

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FPController player = other.GetComponent<FPController>();

            if (player != null && player.IsHoldingObject)
            {
                timer.StopTimer();
                Debug.Log("Escaped with item, timer stopped!");
            }
        }
    }
}