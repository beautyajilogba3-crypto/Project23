using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(GuardPatrol))]
public class GuardDetection : MonoBehaviour
{
    private enum GuardState { Patrolling, Chasing, Investigating };

    [Header("References")]
    [Tooltip("Drag the player object here.")]
    public Transform player;

    [Tooltip("Drag the player's FPController here (used to check crouch state).")]
    public FPController playerController;

    [Header("Detection Cone (in front of the guard)")]
    [Tooltip("How far in front of the guard it can spot the player.")]
    public float detectionRange = 8f;

    [Tooltip("Full width of the vision cone in degrees. 90 = 45 degrees either side of where the guard faces.")]
    [Range(10f, 170f)]
    public float detectionAngle = 90f;

    [Tooltip("If true, walls and obstacles block the guard's vision (matches the light being blocked by walls).")]
    public bool requireLineOfSight = true;

    [Tooltip("Which layers block vision. Set this to your Walls layer so artefacts or other guards don't block sight.")]
    public LayerMask obstacleMask = ~0;

    [Tooltip("Height of the guard's eyes, used for the line-of-sight check.")]
    public float eyeHeight = 1.5f;

    [Tooltip("Height on the player the guard aims its line-of-sight check at.")]
    public float playerTargetHeight = 1.0f;

    [Header("Chase Awareness")]
    [Tooltip("Once chasing, guard gives up if the player gets this much further away than detectionRange. " +
             "Ignored if Relentless Once Spotted is true.")]
    public float loseRangeMultiplier = 1.5f;

    [Tooltip("If true, once chasing, distance no longer breaks the chase - only crouching does.")]
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

    [Header("Detection Light (shows the player where the guard can see)")]
    [Tooltip("Optional. Leave empty and a spot light is created automatically as a child of the guard.")]
    public Light detectionLight;

    [Tooltip("Height of the light above the guard's feet (only used for the auto-created light).")]
    public float lightHeight = 1.5f;

    [Tooltip("Downward tilt of the light in degrees, so the beam lands on the floor (only used for the auto-created light).")]
    public float lightPitch = 15f;

    [Tooltip("How far in front of the guard's centre the light sits. Must be bigger than the guard's body radius, " +
             "otherwise the guard's own body blocks the light (only used for the auto-created light).")]
    public float lightForwardOffset = 0.6f;

    [Tooltip("If true, walls block the light. Turn off to test whether shadows are the reason the light isn't showing.")]
    public bool lightCastsShadows = true;

    public Color patrolLightColor = new Color(1f, 0.3f, 0.25f);          // reddish: patrolling
    public Color investigateLightColor = new Color(1f, 0.65f, 0.1f);     // amber: searching
    public Color chaseLightColor = new Color(1f, 0f, 0f);                // pure red: spotted

    public float patrolLightIntensity = 5f;
    public float investigateLightIntensity = 6f;
    public float chaseLightIntensity = 10f;

    [Tooltip("How quickly the light fades between colours.")]
    public float lightTransitionSpeed = 6f;

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
    private NavMeshAgent agent;          // optional - if present, it drives all movement
    private float patrolAgentSpeed;      // agent speed to restore when returning to patrol

    private GuardState state = GuardState.Patrolling;
    private bool caught = false;

    private Vector3 lastKnownPosition;
    private float investigateTimer = 0f;
    private bool investigateArrived = false;

    void Awake()
    {
        patrol = GetComponent<GuardPatrol>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
            patrolAgentSpeed = agent.speed;
        else
            Debug.LogWarning($"{name}: no NavMeshAgent found - chase/investigate will move in straight lines THROUGH WALLS.", this);

        SetupLight();
    }

    void Update()
    {
        if (caught || player == null)
            return;

        float distance = HorizontalDistance(transform.position, player.position);
        bool playerCrouched = playerController != null && playerController.IsCrouching;

        // How the guard can see the player depends on its state:
        // - Patrolling / Investigating: only inside the lit vision cone (and not behind walls),
        //   so "if the light isn't on you, you're not seen".
        // - Chasing: the guard already knows where you are, so it keeps tracking you in a
        //   full circle (or without any distance limit if relentlessOnceSpotted is on).
        bool playerVisible;
        if (state == GuardState.Chasing)
        {
            playerVisible = relentlessOnceSpotted
                ? !playerCrouched
                : !playerCrouched && distance <= detectionRange * loseRangeMultiplier;
        }
        else
        {
            playerVisible = !playerCrouched && IsInDetectionCone(distance) && HasLineOfSight();
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
                    MoveTowards(player.position);

                    if (distance <= catchRange)
                    {
                        CatchPlayer();
                        return;
                    }
                }
                else
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

                if (!investigateArrived)
                {
                    if (HasArrivedAtLastKnown())
                    {
                        investigateArrived = true;
                        StopMoving();
                    }
                    else
                    {
                        MoveTowards(lastKnownPosition);
                    }
                }
                else
                {
                    // Arrived - stand and "look around" before giving up.
                    investigateTimer -= Time.deltaTime;
                    if (investigateTimer <= 0f)
                    {
                        EnterPatrol();
                    }
                }
                break;
        }

        UpdateDetectionLight();
    }

    // ---------- Vision ----------

    // Is the player within range AND inside the cone in front of the guard?
    private bool IsInDetectionCone(float horizontalDistance)
    {
        if (horizontalDistance > detectionRange)
            return false;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        // Standing right on top of the guard - treat as inside the cone.
        if (toPlayer.sqrMagnitude < 0.0001f)
            return true;

        Vector3 forward = transform.forward;
        forward.y = 0f;

        float angleToPlayer = Vector3.Angle(forward, toPlayer);
        return angleToPlayer <= detectionAngle * 0.5f;
    }

    // Is there a wall or obstacle between the guard's eyes and the player?
    private bool HasLineOfSight()
    {
        if (!requireLineOfSight)
            return true;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * playerTargetHeight;
        Vector3 direction = target - origin;
        float length = direction.magnitude;

        if (length < 0.01f)
            return true;

        if (Physics.Raycast(origin, direction / length, out RaycastHit hit, length, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            Transform hitTransform = hit.transform;

            // Hitting the player (or ourselves) first means nothing is in the way.
            if (hitTransform == player || hitTransform.IsChildOf(player) || hitTransform.IsChildOf(transform))
                return true;

            return false; // a wall/obstacle is blocking the view
        }

        return true;
    }

    // ---------- State transitions ----------

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
        investigateArrived = false;
        // lastKnownPosition already holds the last spot the player was seen.
    }

    private void EnterPatrol()
    {
        state = GuardState.Patrolling;

        // Hand the agent back to the patrol script cleanly.
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = patrolAgentSpeed;
            agent.ResetPath();
            agent.isStopped = false;
        }

        patrol.patrolling = true;
        patrol.ResetPatrolOrigin();
    }

    // ---------- Movement ----------

    private void MoveTowards(Vector3 target)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            // Snap the target onto the NavMesh so an off-mesh player position
            // (e.g. pressed against a wall) still gives a reachable destination.
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                target = hit.position;

            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(target);
            // The agent rotates the guard to face its direction of travel.
        }
        else
        {
            // Fallback for guards without a NavMeshAgent (no wall avoidance).
            patrol.FaceDirection(target - transform.position);

            target.y = transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, target, chaseSpeed * Time.deltaTime);
        }
    }

    private void StopMoving()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    private bool HasArrivedAtLastKnown()
    {
        if (HorizontalDistance(transform.position, lastKnownPosition) <= investigateArriveDistance)
            return true;

        // With a NavMeshAgent the guard may stop short of the raw position
        // (walls, snapped destination), so also trust the agent's own path.
        if (agent != null && agent.isOnNavMesh && agent.hasPath && !agent.pathPending
            && agent.remainingDistance <= Mathf.Max(investigateArriveDistance, agent.stoppingDistance))
            return true;

        return false;
    }

    // Distance ignoring height, so pivot-height differences between guard
    // and player never affect detection, catch, or arrival checks.
    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // ---------- Detection light ----------

    private void SetupLight()
    {
        if (detectionLight == null)
        {
            GameObject lightObject = new GameObject("DetectionLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, lightHeight, lightForwardOffset);
            lightObject.transform.localRotation = Quaternion.Euler(lightPitch, 0f, 0f);

            detectionLight = lightObject.AddComponent<Light>();
            detectionLight.shadows = lightCastsShadows ? LightShadows.Soft : LightShadows.None; // walls block the light, matching line of sight
        }

        detectionLight.type = LightType.Spot;
        detectionLight.color = patrolLightColor;
        detectionLight.intensity = patrolLightIntensity;
    }

    private void UpdateDetectionLight()
    {
        if (detectionLight == null)
            return;

        // Keep the light's size matched to the actual detection values.
        detectionLight.range = detectionRange;
        detectionLight.spotAngle = detectionAngle;

        Color targetColor;
        float targetIntensity;

        switch (state)
        {
            case GuardState.Chasing:
                targetColor = chaseLightColor;
                targetIntensity = chaseLightIntensity;
                break;
            case GuardState.Investigating:
                targetColor = investigateLightColor;
                targetIntensity = investigateLightIntensity;
                break;
            default:
                targetColor = patrolLightColor;
                targetIntensity = patrolLightIntensity;
                break;
        }

        float t = Time.deltaTime * lightTransitionSpeed;
        detectionLight.color = Color.Lerp(detectionLight.color, targetColor, t);
        detectionLight.intensity = Mathf.Lerp(detectionLight.intensity, targetIntensity, t);
    }

    // ---------- Messages / game over ----------

    private void ShowSpottedMessage()
    {
        if (messageText == null)
            return;

        MessagePump pump = messageText.GetComponent<MessagePump>();
        if (pump == null)
            pump = messageText.gameObject.AddComponent<MessagePump>();

        pump.ShowThenClear(messageText, spottedMessage, spottedMessageDuration);
    }

    private void CatchPlayer()
    {
        caught = true;
        StopMoving();

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

    // Shows the vision cone (yellow) and catch range (red) in the Scene view when the guard is selected.
    void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        float half = detectionAngle * 0.5f;

        Gizmos.color = Color.yellow;
        Vector3 leftEdge = Quaternion.AngleAxis(-half, Vector3.up) * forward;
        Vector3 rightEdge = Quaternion.AngleAxis(half, Vector3.up) * forward;
        Gizmos.DrawLine(origin, origin + leftEdge * detectionRange);
        Gizmos.DrawLine(origin, origin + rightEdge * detectionRange);

        // Arc across the front of the cone.
        const int segments = 24;
        Vector3 previous = origin + leftEdge * detectionRange;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-half, half, i / (float)segments);
            Vector3 point = origin + (Quaternion.AngleAxis(angle, Vector3.up) * forward) * detectionRange;
            Gizmos.DrawLine(previous, point);
            previous = point;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, catchRange);
    }
}