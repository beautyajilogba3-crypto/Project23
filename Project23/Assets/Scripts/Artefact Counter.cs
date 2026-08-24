using UnityEngine;
using TMPro;

public class ArtefactCounter : MonoBehaviour
{
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
    }

    private void UpdateText()
    {
        artefactText.text = artefactsCollected + "/" + totalArtefacts;
    }
}