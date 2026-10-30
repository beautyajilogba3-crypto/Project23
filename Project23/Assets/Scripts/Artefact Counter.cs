using System;
using UnityEngine;
using TMPro;

public class ArtefactCounter : MonoBehaviour
{
    // Fired once, when the final artifact is collected. Guards listen for this to enter lockdown.
    public static event Action OnAllArtefactsCollected;

    public TMP_Text artefactText;

    private int artefactsCollected = 0;
    public int totalArtefacts = 3;

    private void Start()
    {
        UpdateText();
    }

    public void CollectArtefact()
    {
        if (artefactsCollected >= totalArtefacts)
            return;

        artefactsCollected++;
        UpdateText();

        if (artefactsCollected >= totalArtefacts)
            OnAllArtefactsCollected?.Invoke();
    }

    private void UpdateText()
    {
        artefactText.text = artefactsCollected + "/" + totalArtefacts;
    }
}