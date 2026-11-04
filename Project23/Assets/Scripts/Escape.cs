
using UnityEngine;

public class DoorExit : MonoBehaviour
{
    public Timer timer;
    public ArtefactCounter artefactCounter;
    public WinManager winManager;

    private bool exitTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (exitTriggered)
            return;

        FPController player = other.GetComponentInParent<FPController>();

        if (player == null)
            return;

        if (artefactCounter == null || winManager == null || timer == null)
        {
            Debug.LogError("DoorExit is missing an Inspector reference!");
            return;
        }

        if (!timer.timerrunning)
            return;

        if (artefactCounter.ArtefactsCollected >= artefactCounter.totalArtefacts)
        {
            exitTriggered = true;

            timer.StopTimer();
            winManager.ShowVictory();

            Debug.Log("Player escaped with all three artefacts!");
        }
        else
        {
            Debug.Log("You need to collect all three artefacts before escaping!");
        }
    }
}
