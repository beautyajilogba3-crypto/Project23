using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(GuardPatrol))]
public class GuardDetection : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the player object here.")]
    public Transform player;

    [Tooltip("Drag the player's FPController here (used to check crouch state).")]
    public FPController playerController;

    [Header("Detection Settings")]
    [Tooltip("Guard starts chasing if the player is within this range and not crouched.")]
    public float detectionRange = 8f;

    [Tooltip("Guard gives up the chase if the player gets this much further away than detectionRange.")]
    public float loseRangeMultiplier = 1.5f;

    [Tooltip("Guard catches the player within this range.")]
    public float catchRange = 1.5f;

    [Header("Chase Settings")]
    public float chaseSpeed = 4f;

    [Header("Catch / Game Over")]
    public TMP_Text messageText;
    public string caughtMessage = "You've been caught!";
    public string mainMenuScene = "Main Menu";
    public float catchDelay = 2f;

    [Header("Spotted Message")]
    [Tooltip("Message shown briefly the moment the guard starts chasing.")]
    public string spottedMessage = "You've been seen!";

    [Tooltip("How long the spotted message stays on screen before clearing.")]
    public float spottedMessageDuration = 1.5f;

    [Tooltip("Drag the object holding your Timer script here, so it stops counting once the player is caught.")]
    public Timer timer;

    private GuardPatrol patrol;
    private bool chasing = false;
    private bool caught = false;

    void Awake()
    {
        patrol = GetComponent<GuardPatrol>();
    }

    void Update()
    {
        if (caught || player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool playerCrouched = playerController != null && playerController.IsCrouching;

        if (!playerCrouched && distance <= detectionRange)
        {
            if (!chasing)
            {
                chasing = true;
                patrol.patrolling = false;
                ShowSpottedMessage();
            }

            ChasePlayer();

            if (distance <= catchRange)
            {
                CatchPlayer();
                return;
            }
        }
        else if (chasing && (playerCrouched || distance > detectionRange * loseRangeMultiplier))
        {
            // Lost the player - resume patrolling from current position.
            chasing = false;
            patrol.patrolling = true;
            patrol.ResetPatrolOrigin();
        }
    }

    private void ShowSpottedMessage()
    {
        if (messageText == null)
            return;

        // Reuses the same MessagePump helper Artifactcollector uses, so the
        // message clears itself even though this GuardDetection component
        // keeps running afterward (unlike the artefact, which deactivates).
        MessagePump pump = messageText.GetComponent<MessagePump>();
        if (pump == null)
            pump = messageText.gameObject.AddComponent<MessagePump>();

        pump.ShowThenClear(messageText, spottedMessage, spottedMessageDuration);
    }

    private void ChasePlayer()
    {
        Vector3 destination = player.position;
        destination.y = transform.position.y; // keep guard's own height, ignore player's vertical position

        transform.position = Vector3.MoveTowards(transform.position, destination, chaseSpeed * Time.deltaTime);
    }

    private void CatchPlayer()
    {
        caught = true;
        chasing = false;

        if (timer != null)
            timer.StopTimer();

        if (messageText != null)
            messageText.text = caughtMessage;

        Debug.Log("Player caught! Returning to main menu.");

        Invoke(nameof(GoToMainMenu), catchDelay);
    }

    private void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Optional: visualize detection/catch ranges in the Scene view while designing.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchRange);
    }
}