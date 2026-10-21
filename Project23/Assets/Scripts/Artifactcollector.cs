
using TMPro;
using UnityEngine;
using System.Collections;

public class Artifactcollector : MonoBehaviour
{
    public ArtefactCounter artefactCounter;
    public TMP_Text messageText;

    public string pickupMessage = "Artifact fragment collected";
    public bool Collected { get; private set; } = false;
    public float messageDuration = 2f;

    [Header("Pickup Sound")]
    public AudioSource audioSource;
    public AudioClip pickupSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    public void Collect()
    {
        if (Collected)
            return;

        Collected = true;

        // Play the pickup sound
        if (pickupSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(pickupSound, soundVolume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(
                    pickupSound,
                    Camera.main != null
                        ? Camera.main.transform.position
                        : transform.position,
                    soundVolume
                );
            }
        }

        if (artefactCounter != null)
        {
            artefactCounter.CollectArtefact();
        }
        else
        {
            Debug.LogWarning(
                $"{name}: No ArtefactCounter assigned!", this);
        }

        ShowMessage();

        gameObject.SetActive(false);
    }

    private void ShowMessage()
    {
        if (messageText == null)
            return;

        MessagePump pump = messageText.GetComponent<MessagePump>();

        if (pump == null)
            pump = messageText.gameObject.AddComponent<MessagePump>();

        pump.ShowThenClear(
            messageText,
            pickupMessage,
            messageDuration
        );
    }
}

public class MessagePump : MonoBehaviour
{
    private Coroutine activeRoutine;

    public void ShowThenClear(
        TMP_Text text,
        string message,
        float duration)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(
            ShowThenClearRoutine(text, message, duration)
        );
    }

    private IEnumerator ShowThenClearRoutine(
        TMP_Text text,
        string message,
        float duration)
    {
        text.text = message;

        yield return new WaitForSeconds(duration);

        if (text != null && text.text == message)
            text.text = "";

        activeRoutine = null;
    }
}
