using UnityEngine;

public class Artifactcollector : MonoBehaviour
{
    [Tooltip("Drag the GameObject holding your ArtefactCounter script here.")]
    public ArtefactCounter artefactCounter;

    public bool Collected { get; private set; } = false;

    public void Collect()
    {
        if (Collected)
            return;

        Collected = true;

        if (artefactCounter != null)
        {
            artefactCounter.CollectArtefact();
        }
        else
        {
            Debug.LogWarning($"{name}: No ArtefactCounter assigned!", this);
        }


        gameObject.SetActive(false);
    }
}
