
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

    void Awake()
    {
        patrol = GetComponent<GuardPatrol>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
            patrolAgentSpeed = agent.speed;

        SetupLight();
        SetupVisionCone();
        SetupAudio();
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
                    investigateTimer -= Time.deltaTime;
                    if (investigateTimer <= 0f)
                    {
                        EnterPatrol();
                    }
                }
                break;
        }

        UpdateDetectionLight();
        UpdateVisionCone();
    }

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

    private void EnterChase()
    {
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

    private void MoveTowards(Vector3 target)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
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

    private bool HasArrivedAtLastKnown()
    {
        if (HorizontalDistance(transform.position, lastKnownPosition) <= investigateArriveDistance)
            return true;

        if (agent != null && agent.isOnNavMesh && agent.hasPath && !agent.pathPending
            && agent.remainingDistance <= Mathf.Max(investigateArriveDistance, agent.stoppingDistance))
            return true;

        return false;
    }

    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

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
        if (state == GuardState.Chasing) activeColor = chaseLightColor;
        else if (state == GuardState.Investigating) activeColor = investigateLightColor;

        visionCone.Configure(detectionRange, detectionAngle, activeColor, currentConeOpacity, coneHeight, obstacleMask, player);
    }

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
