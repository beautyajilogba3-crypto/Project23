using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

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
    public float detectionRange = 16f;

    [Tooltip("Full width of the vision cone in degrees. 90 = 45 degrees either side of where the guard faces.")]
    [Range(10f, 170f)]
    public float detectionAngle = 90f;

    [Tooltip("If true, walls and obstacles block the guard's vision.")]
    public bool requireLineOfSight = true;

    [Tooltip("Which layers block vision.")]
    public LayerMask obstacleMask = ~0;

    [Tooltip("Height of the guard's eyes, used for the line-of-sight check.")]
    public float eyeHeight = 1.5f;

    [Tooltip("Height on the player the guard aims its line-of-sight check at.")]
    public float playerTargetHeight = 1.0f;

    [Header("Chase Awareness")]
    public float loseRangeMultiplier = 1.5f;
    public bool relentlessOnceSpotted = false;
    public float catchRange = 1.5f;

    [Header("Chase Settings")]
    public float chaseSpeed = 4f;

    [Header("Catch When Blocked (player standing on objects)")]
    [Tooltip("How far from the player the guard may look for the nearest walkable floor when the player is off the NavMesh (e.g. on a crate). Make it a bit bigger than your largest object's half-width.")]
    public float chaseSnapRadius = 8f;

    [Tooltip("If the guard can't get any closer (the player is on a crate, table, etc.) it can still catch the player from this far away.")]
    public float blockedCatchRange = 3.5f;

    [Tooltip("How long the guard must be stuck at the edge before the catch happens, so the player gets a brief moment of tension.")]
    public float blockedCatchDelay = 0.4f;

    [Tooltip("The guard counts as 'stuck' when it moves slower than this (metres per second) while chasing.")]
    public float blockedSpeedThreshold = 0.6f;

    [Tooltip("Players higher than this above the guard can't be caught (e.g. on a balcony). Raise it if your objects are tall.")]
    public float maxCatchHeightDifference = 3f;

    [Header("Investigate Settings")]
    public float investigateWaitTime = 3f;
    public float investigateArriveDistance = 0.5f;

    [Header("Detection Light")]
    public Light detectionLight;
    public float lightHeight = 1.5f;
    public float lightPitch = 15f;
    public float lightForwardOffset = 0.6f;
    public bool lightCastsShadows = true;

    public Color patrolLightColor = new Color(1f, 0.3f, 0.25f);
    public Color investigateLightColor = new Color(1f, 0.65f, 0.1f);
    public Color chaseLightColor = new Color(1f, 0f, 0f);

    public float patrolLightIntensity = 5f;
    public float investigateLightIntensity = 6f;
    public float chaseLightIntensity = 10f;
    public float lightTransitionSpeed = 6f;
    public bool useSpotLight = true;

    [Header("Vision Cone Visual (visible beam on the floor)")]
    public bool showVisionCone = true;
    public Material visionConeMaterial;

    [Range(0f, 1f)]
    public float coneOpacity = 0.35f;

    [Range(0f, 1f)]
    public float chaseConeOpacity = 0.6f;

    [Tooltip("Vertical height/thickness of the 3D cone volume.")]
    public float coneHeight = 2f;

    public int coneRayCount = 40;
    public float coneFloorHeight = 0.05f;
    public float coneWallCheckHeight = 1f;

    [Header("Detection Sound")]
    public AudioClip detectedSound;
    [Range(0f, 1f)] public float detectedVolume = 1f;
    [Range(0f, 1f)] public float detectedSoundSpatialBlend = 0f;
    public float detectedSoundCooldown = 0.5f;

    [Header("Catch / Game Over")]
    public TMP_Text messageText;
    public string caughtMessage = "You've been caught!";
    public GameOverManager gameOverManager;

    [Header("Spotted Message")]
    public string spottedMessage = "You've been seen!";
    public float spottedMessageDuration = 1.5f;
    public Timer timer;

    [Header("Investigate Safety")]
    [Tooltip("Guards search a random spot this close to where the player was last seen, so two guards don't try to stand on the exact same point and block each other.")]
    public float investigateSpread = 1.5f;

    [Tooltip("If the guard hasn't reached its search spot after this many seconds, it stops walking and starts its wait anyway.")]
    public float investigateMaxTravelTime = 8f;

    [Tooltip("If the guard is blocked and barely moving for this many seconds, it counts as arrived.")]
    public float investigateStuckTime = 1f;

    [Header("Lockdown (triggered by collecting the final artifact)")]
    [Tooltip("Chase speed used during lockdown (normal chase speed is the Chase Speed field above).")]
    public float lockdownChaseSpeed = 4.159f;

    [Tooltip("Patrol walk speed used during lockdown (replaces GuardPatrol's Walk Speed).")]
    public float lockdownPatrolSpeed = 3.75f;

    [Tooltip("Colour of the light and vision cone during lockdown.")]
    public Color lockdownColor = new Color(0.6f, 0.1f, 1f);

    [Tooltip("Light intensity while in lockdown and not chasing.")]
    public float lockdownLightIntensity = 8f;

    [Tooltip("Alarm clip. Remember to credit the source in your submission. Only the first guard with a clip plays it, so the alarm doesn't stack.")]
    public AudioClip alarmSound;
    [Range(0f, 1f)] public float alarmVolume = 1f;

    [Tooltip("Loop the alarm clip so it keeps sounding. Needed for a siren that plays continuously.")]
    public bool alarmLoops = true;

    [Tooltip("Seconds of alarm before it is cut off. 0 = never stops (until the player is caught).")]
    public float alarmDuration = 0f;

    [Tooltip("Optional. A second AudioSource used only for the alarm. If left empty, one is created automatically.")]
    public AudioSource alarmSource;

    [Tooltip("Message shown on screen when lockdown starts.")]
    public string lockdownMessage = "LOCKDOWN!";
    public float lockdownMessageDuration = 2f;

    [Tooltip("Colour of the LOCKDOWN text. Other messages go back to their normal colour afterwards.")]
    public Color lockdownTextColor = new Color(0.7f, 0.3f, 1f);

    [Tooltip("Font size multiplier for the LOCKDOWN text. 1.4 = 40% bigger.")]
    public float lockdownFontScale = 1.4f;

    private GuardPatrol patrol;
    private NavMeshAgent agent;
    private float patrolAgentSpeed;

    private GuardState state = GuardState.Patrolling;
    private bool caught = false;

    private Vector3 lastKnownPosition;
    private float investigateTimer = 0f;
    private bool investigateArrived = false;

    private VisionConeVisual visionCone;
    private float currentConeOpacity;
    private AudioSource audioSource;
    private float lastDetectedSoundTime = -999f;

    private Vector3 investigateTarget;
    private float investigateTravelTimer;
    private float investigateStuckTimer;

    private float blockedTimer = 0f;
    private Vector3 lastGuardPosition;

    private bool lockdown = false;
    private static AudioSource activeAlarmSource; // so any guard can silence the alarm when the player is caught
    private static float lastLockdownMessageTime = -999f;
    private static float lastAlarmTime = -999f; // shared so several guards don't stack several alarms

    void Awake()
    {
        patrol = GetComponent<GuardPatrol>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
            patrolAgentSpeed = agent.speed;

        SetupLight();
        SetupVisionCone();
        SetupAudio();
        SetupAlarmAudio();
    }

    void OnEnable()
    {
        ArtefactCounter.OnAllArtefactsCollected += EnterLockdown;
    }

    void OnDisable()
    {
        ArtefactCounter.OnAllArtefactsCollected -= EnterLockdown;
    }

    void Update()
    {
        if (caught || player == null)
            return;

        float distance = HorizontalDistance(transform.position, player.position);
        bool playerCrouched = playerController != null && playerController.IsCrouching;

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

                    if (distance <= catchRange || IsBlockedNextToPlayer(distance))
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
                    EnterChase();
                    break;
                }

                if (!investigateArrived)
                {
                    investigateTravelTimer += Time.deltaTime;

                    if (HasArrivedAtInvestigateTarget() || investigateTravelTimer >= investigateMaxTravelTime)
                    {
                        investigateArrived = true;
                        StopMoving();
                    }
                    else
                    {
                        MoveTowards(investigateTarget);
                    }
                }
                else
                {
                    investigateTimer -= Time.deltaTime;
                    if (investigateTimer <= 0f)
                        EnterPatrol();
                }
                break;
        }

        UpdateDetectionLight();
        UpdateVisionCone();
    }

    // ---------- Lockdown ----------

    private void EnterLockdown()
    {
        if (lockdown)
            return;

        lockdown = true;
        Debug.Log($"{name}: LOCKDOWN started (alarm clip: {(alarmSound != null ? alarmSound.name : "NONE")}, message text: {(messageText != null ? "assigned" : "NOT assigned")})", this);

        // Speed buff using fixed values. patrolAgentSpeed is what EnterPatrol restores,
        // so patrol stays buffed after a chase too.
        chaseSpeed = lockdownChaseSpeed;
        patrolAgentSpeed = lockdownPatrolSpeed;
        if (patrol != null)
            patrol.walkSpeed = lockdownPatrolSpeed;

        // If already patrolling, apply the new speed right away.
        if (state == GuardState.Patrolling && agent != null && agent.isOnNavMesh)
            agent.speed = lockdownPatrolSpeed;

        // Alarm: played once across all guards, by the first guard that has a clip.
        if (alarmSound != null && Time.time - lastAlarmTime > 0.1f)
        {
            lastAlarmTime = Time.time;
            StartCoroutine(PlayAlarmBriefly());
        }

        // Message: shown once across all guards, by the first guard that has Message Text assigned.
        if (messageText != null && Time.time - lastLockdownMessageTime > 0.1f)
        {
            lastLockdownMessageTime = Time.time;
            ShowLockdownMessage();
        }
    }

    private IEnumerator PlayAlarmBriefly()
    {
        if (alarmSource == null)
        {
            Debug.LogWarning($"{name}: alarm could not play - no AudioSource available.", this);
            yield break;
        }

        Debug.Log($"{name}: playing alarm (volume {alarmVolume}, loop {alarmLoops})", this);
        alarmSource.clip = alarmSound;
        alarmSource.volume = alarmVolume;
        alarmSource.loop = alarmLoops;
        alarmSource.Play();
        activeAlarmSource = alarmSource;

        // 0 (or less) = keep playing until the player is caught.
        if (alarmDuration <= 0f)
            yield break;

        yield return new WaitForSeconds(alarmDuration);

        if (alarmSource != null && alarmSource.clip == alarmSound)
            alarmSource.Stop();
    }

    private void ShowLockdownMessage()
    {
        if (messageText == null)
            return;

        MessagePump pump = messageText.GetComponent<MessagePump>();
        if (pump == null)
            pump = messageText.gameObject.AddComponent<MessagePump>();

        // Never shorter than half a second, so a 0 in the Inspector can't make the message invisible.
        float duration = Mathf.Max(lockdownMessageDuration, 0.5f);

        pump.ShowThenClear(messageText, lockdownMessage, duration);
        StartCoroutine(LockdownTextStyleRoutine(duration));
    }

    // Makes the shared message text purple and bigger while LOCKDOWN is showing,
    // then restores the original style so other messages look like they used to.
    private IEnumerator LockdownTextStyleRoutine(float duration)
    {
        TMP_Text text = messageText;
        Color originalColor = text.color;
        float originalSize = text.fontSize;

        text.color = lockdownTextColor;
        text.fontSize = originalSize * lockdownFontScale;

        float end = Time.time + duration;
        while (Time.time < end && text != null && text.text == lockdownMessage)
            yield return null;

        if (text != null)
        {
            text.color = originalColor;
            text.fontSize = originalSize;
        }
    }

    // ---------- Detection ----------

    private bool IsInDetectionCone(float horizontalDistance)
    {
        if (horizontalDistance > detectionRange)
            return false;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f)
            return true;

        Vector3 forward = transform.forward;
        forward.y = 0f;

        float angleToPlayer = Vector3.Angle(forward, toPlayer);
        return angleToPlayer <= detectionAngle * 0.5f;
    }

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
            if (hitTransform == player || hitTransform.IsChildOf(player) || hitTransform.IsChildOf(transform))
                return true;

            return false;
        }

        return true;
    }

    // ---------- State changes ----------

    // True when the guard has run as far as it can toward the player but still can't reach
    // them (e.g. the player is standing on a crate the NavMesh doesn't cover).
    private bool IsBlockedNextToPlayer(float horizontalDistance)
    {
        // Measure how far the guard has actually moved this frame. This is more reliable than
        // the agent's path flags, because the guard re-requests a path every frame while
        // chasing and those flags keep flickering.
        float movedThisFrame = HorizontalDistance(transform.position, lastGuardPosition);
        float actualSpeed = Time.deltaTime > 0f ? movedThisFrame / Time.deltaTime : 0f;
        lastGuardPosition = transform.position;

        bool blocked = horizontalDistance <= blockedCatchRange
                       && Mathf.Abs(player.position.y - transform.position.y) <= maxCatchHeightDifference
                       && actualSpeed < blockedSpeedThreshold;

        blockedTimer = blocked ? blockedTimer + Time.deltaTime : 0f;
        return blockedTimer >= blockedCatchDelay;
    }

    private void EnterChase()
    {
        blockedTimer = 0f;
        lastGuardPosition = transform.position;
        state = GuardState.Chasing;
        patrol.patrolling = false;
        lastKnownPosition = player.position;
        PlayDetectedSound();
        ShowSpottedMessage();
    }

    private void EnterInvestigate()
    {
        state = GuardState.Investigating;
        investigateTimer = investigateWaitTime;
        investigateArrived = false;
        investigateTravelTimer = 0f;
        investigateStuckTimer = 0f;

        // Pick a search spot near where the player was last seen, snapped onto the NavMesh,
        // so it's always reachable and two guards don't choose the identical point.
        Vector2 offset = Random.insideUnitCircle * investigateSpread;
        Vector3 candidate = lastKnownPosition + new Vector3(offset.x, 0f, offset.y);

        investigateTarget = lastKnownPosition;
        if (NavMesh.SamplePosition(candidate, out NavMeshHit candidateHit, 3f, NavMesh.AllAreas))
            investigateTarget = candidateHit.position;
        else if (NavMesh.SamplePosition(lastKnownPosition, out NavMeshHit lastHit, 3f, NavMesh.AllAreas))
            investigateTarget = lastHit.position;
    }

    private void EnterPatrol()
    {
        state = GuardState.Patrolling;

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
            // Snap the target to the nearest walkable spot. When the player is standing on an object
            // the NavMesh doesn't cover, this is the floor right at the object's edge, so the guard
            // walks right up to it instead of stopping somewhere far away.
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, chaseSnapRadius, NavMesh.AllAreas))
                target = hit.position;

            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(target);
        }
        else
        {
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

    private bool HasArrivedAtInvestigateTarget()
    {
        if (HorizontalDistance(transform.position, investigateTarget) <= investigateArriveDistance)
            return true;

        if (agent != null && agent.isOnNavMesh && !agent.pathPending)
        {
            // The agent finished its path (it can stop short of the exact point).
            if (agent.hasPath && agent.remainingDistance <= Mathf.Max(investigateArriveDistance, agent.stoppingDistance))
                return true;

            // Blocked or unreachable: barely moving for a while counts as arrived,
            // so a guard can never stand there forever.
            if (agent.velocity.sqrMagnitude < 0.01f)
            {
                investigateStuckTimer += Time.deltaTime;
                if (investigateStuckTimer >= investigateStuckTime)
                    return true;
            }
            else
            {
                investigateStuckTimer = 0f;
            }
        }

        return false;
    }

    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // ---------- Light ----------

    private void SetupLight()
    {
        if (detectionLight == null)
        {
            GameObject lightObject = new GameObject("DetectionLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, lightHeight, lightForwardOffset);
            lightObject.transform.localRotation = Quaternion.Euler(lightPitch, 0f, 0f);

            detectionLight = lightObject.AddComponent<Light>();
            detectionLight.shadows = lightCastsShadows ? LightShadows.Soft : LightShadows.None;
        }

        detectionLight.type = LightType.Spot;
        detectionLight.color = patrolLightColor;
        detectionLight.intensity = patrolLightIntensity;
        detectionLight.enabled = useSpotLight;
    }

    private void UpdateDetectionLight()
    {
        if (detectionLight == null)
            return;

        detectionLight.range = detectionRange;
        detectionLight.spotAngle = detectionAngle;

        Color targetColor;
        float targetIntensity;

        if (lockdown)
        {
            targetColor = lockdownColor;
            targetIntensity = (state == GuardState.Chasing) ? chaseLightIntensity : lockdownLightIntensity;
        }
        else
        {
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
        }

        float t = Time.deltaTime * lightTransitionSpeed;
        detectionLight.color = Color.Lerp(detectionLight.color, targetColor, t);
        detectionLight.intensity = Mathf.Lerp(detectionLight.intensity, targetIntensity, t);
    }

    // ---------- Vision cone ----------

    private void SetupVisionCone()
    {
        if (!showVisionCone)
            return;

        GameObject coneObj = new GameObject("VisionConeVisual");
        coneObj.transform.SetParent(transform, false);
        visionCone = coneObj.AddComponent<VisionConeVisual>();

        Material mat = visionConeMaterial;
        if (mat == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("UI/Default");
            mat = new Material(shader);
        }

        visionCone.Initialize(mat, coneRayCount, coneFloorHeight, coneHeight, coneWallCheckHeight);
        currentConeOpacity = coneOpacity;
    }

    private void UpdateVisionCone()
    {
        if (visionCone == null)
            return;

        visionCone.gameObject.SetActive(showVisionCone);
        if (!showVisionCone)
            return;

        float targetOpacity = (state == GuardState.Chasing) ? chaseConeOpacity : coneOpacity;
        currentConeOpacity = Mathf.MoveTowards(currentConeOpacity, targetOpacity, Time.deltaTime * 3f);

        Color activeColor = patrolLightColor;
        if (lockdown) activeColor = lockdownColor;
        else if (state == GuardState.Chasing) activeColor = chaseLightColor;
        else if (state == GuardState.Investigating) activeColor = investigateLightColor;

        visionCone.Configure(detectionRange, detectionAngle, activeColor, currentConeOpacity, coneHeight, obstacleMask, player);
    }

    // ---------- Audio ----------

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && detectedSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.spatialBlend = detectedSoundSpatialBlend;
            audioSource.playOnAwake = false;
        }
    }

    private void SetupAlarmAudio()
    {
        // The alarm needs its own AudioSource so stopping it never cuts off the "spotted" sound.
        if (alarmSound == null)
            return;

        if (alarmSource == null || alarmSource == audioSource)
            alarmSource = gameObject.AddComponent<AudioSource>();

        alarmSource.playOnAwake = false;
        alarmSource.loop = false;
        alarmSource.spatialBlend = 0f; // 2D so the player hears it clearly wherever they are
    }

    private void PlayDetectedSound()
    {
        if (detectedSound == null || audioSource == null)
            return;

        if (Time.time - lastDetectedSoundTime < detectedSoundCooldown)
            return;

        audioSource.volume = detectedVolume;
        audioSource.spatialBlend = detectedSoundSpatialBlend;
        audioSource.PlayOneShot(detectedSound);
        lastDetectedSoundTime = Time.time;
    }

    // ---------- Messages ----------

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

        // Silence the siren so it doesn't blare over the Game Over screen.
        if (activeAlarmSource != null)
            activeAlarmSource.Stop();

        if (timer != null)
            timer.StopTimer();

        if (messageText != null)
            messageText.text = caughtMessage;

        Debug.Log("Player caught! Showing Game Over panel.");

        if (gameOverManager != null)
        {
            gameOverManager.ShowGameOver();
        }
        else
        {
            Debug.LogError("Game Over Manager is not assigned on GuardDetection!", this);
        }
    }

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