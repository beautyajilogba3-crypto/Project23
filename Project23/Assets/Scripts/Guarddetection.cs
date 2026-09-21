using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(GuardPatrol))]
public class GuardDetection : MonoBehaviour
{
    private enum GuardState { Patrolling, Chasing, Investigating }

    [Header("References")]
    [Tooltip("Drag the player object here.")]
    public Transform player;

    [Tooltip("Drag the player's FPController here (used to check crouch state).")]
    public FPController playerController;

    [Header("Detection Settings")]
    [Tooltip("Guard starts chasing if the player is within this range and not crouched.")]
    public float detectionRange = 8f;

    [Tooltip("Guard gives up the chase if the player gets this much further away than detectionRange. Ignored if Relentless Once Spotted is true.")]
    public float loseRangeMultiplier = 1.5f;

    [Tooltip("If true, once the guard has spotted you, distance no longer breaks the chase - only crouching does. " +
             "The guard will chase until it catches you or you crouch out of sight.")]
    public bool relentlessOnceSpotted = false;

    [Tooltip("Guard catches the player within this range.")]
    public float catchRange = 1.5f;

    [Header("Chase Settings")]
    public float chaseSpeed = 4f;

    [Header("Investigate Settings")]
    [Tooltip("After losing the player, the guard walks to their last known position, then waits here before giving up.")]
    public float investigateWaitTime = 3f;

    [Tooltip("How close the guard needs to get to the last known position to start the wait timer.")]
    public float investigateArriveDistance = 0.5f;

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
    private GuardState state = GuardState.Patrolling;
    private bool caught = false;

    private Vector3 lastKnownPosition;
    private float investigateTimer = 0f;

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

        // Once already chasing/investigating, relentlessOnceSpotted ignores
        // distance entirely - only crouching breaks visibility. While still
        // patrolling (not yet spotted), the normal detectionRange applies.
        bool playerVisible;
        if (relentlessOnceSpotted && state != GuardState.Patrolling)
        {
            playerVisible = !playerCrouched;
        }
        else if (state == GuardState.Patrolling)
        {
            playerVisible = !playerCrouched && distance <= detectionRange;
        }
        else
        {
            playerVisible = !playerCrouched && distance <= detectionRange * loseRangeMultiplier;
        }

        switch (state)
        {
            case GuardState.Patrolling:
                if (playerVisible)
                    EnterChase();
                break;

            case GuardState.Chasing:
                if (playerVisible)
                {
                    lastKnownPosition = player.position;
                    ChaseTowards(player.position);

                    if (distance <= catchRange)
                    {
                        CatchPlayer();
                        return;
                    }
                }
                else if (playerCrouched || distance > detectionRange * loseRangeMultiplier)
                {
                    EnterInvestigate();
                }
                break;

            case GuardState.Investigating:
                if (playerVisible)
                {
                    // Spotted again mid-search - resume the chase.
                    EnterChase();
                    break;
                }

                float distToLastKnown = Vector3.Distance(transform.position, lastKnownPosition);

                if (distToLastKnown > investigateArriveDistance)
                {
                    // Still walking to where the player was last seen.
                    ChaseTowards(lastKnownPosition);
                }
                else
                {
                    // Arrived - stand and "look around" for investigateWaitTime before giving up.
                    investigateTimer -= Time.deltaTime;
                    if (investigateTimer <= 0f)
                    {
                        EnterPatrol();
                    }
                }
                break;
        }
    }

    private void EnterChase()
    {
        state = GuardState.Chasing;
        patrol.patrolling = false;
        lastKnownPosition = player.position;
        ShowSpottedMessage();
    }

    private void EnterInvestigate()
    {
        state = GuardState.Investigating;
        investigateTimer = investigateWaitTime;
        // lastKnownPosition already holds the last spot the player was seen.
    }

    private void EnterPatrol()
    {
        state = GuardState.Patrolling;
        patrol.patrolling = true;
        patrol.ResetPatrolOrigin();
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

    private void ChaseTowards(Vector3 target)
    {
        target.y = transform.position.y; // keep guard's own height, ignore target's vertical position
        transform.position = Vector3.MoveTowards(transform.position, target, chaseSpeed * Time.deltaTime);
    }

    private void CatchPlayer()
    {
        caught = true;

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