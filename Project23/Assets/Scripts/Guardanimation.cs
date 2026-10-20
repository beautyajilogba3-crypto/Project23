using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Plays idle / walk / run on the guard's model, blended by how fast the guard is really moving.
// No Animator Controller needed - just drag clips into the slots below.
// Attach this to the same object as GuardPatrol (the guard root, not the model).
public class GuardAnimation : MonoBehaviour
{
    [Header("Animation Clips (drag them in from your project)")]
    public AnimationClip idleClip;
    public AnimationClip walkClip;

    [Tooltip("Optional. Used while chasing. If empty, the walk clip is simply played faster.")]
    public AnimationClip runClip;

    [Tooltip("How quickly the animation blends when the guard speeds up or slows down.")]
    public float blendSmoothing = 8f;

    [Tooltip("Prints what the script found on Start, and the guard's speed once a second. Turn off when it works.")]
    public bool debugLog = true;
    private float debugTimer = 0f;

    private Animator animator;
    private NavMeshAgent agent;

    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable[] players;
    private AnimationClip[] clips;

    private float walkSpeedReference = 2f;   // speed the walk clip matches (read from GuardPatrol)
    private float runSpeedReference = 4f;    // speed the run clip matches (read from GuardDetection)

    private bool ready = false;
    private float smoothedSpeed = 0f;
    private Vector3 lastPosition;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            Debug.LogWarning($"{name}: no Animator found on the guard or its children. Is the model a child of this object?", this);
            enabled = false;
            return;
        }

        if (idleClip == null || walkClip == null)
        {
            Debug.LogWarning($"{name}: assign at least an Idle Clip and a Walk Clip on GuardAnimation.", this);
            enabled = false;
            return;
        }

        // The NavMeshAgent moves the guard, not the animation.
        animator.applyRootMotion = false;

        GuardPatrol patrol = GetComponent<GuardPatrol>();
        if (patrol != null)
            walkSpeedReference = patrol.walkSpeed;

        GuardDetection detection = GetComponent<GuardDetection>();
        if (detection != null)
            runSpeedReference = detection.chaseSpeed;

        BuildGraph();
        LogDiagnostics();

        lastPosition = transform.position;
        ready = true;
    }

    // Reports what the script found and warns about the usual reasons clips don't play.
    private void LogDiagnostics()
    {
        if (debugLog)
        {
            Debug.Log($"{name}: GuardAnimation running. Animator on '{animator.gameObject.name}', " +
                      $"avatar = {(animator.avatar != null ? animator.avatar.name : "NONE")}, " +
                      $"humanoid rig = {animator.isHuman}, animator enabled = {animator.enabled}, " +
                      $"idle = {idleClip.name}, walk = {walkClip.name}, run = {(runClip != null ? runClip.name : "none")}", this);
        }

        if (animator.avatar == null)
            Debug.LogWarning($"{name}: the model's Animator has no Avatar. Set the Rig tab on the model to Humanoid or Generic and click Apply.", this);

        if (!animator.enabled)
            Debug.LogWarning($"{name}: the model's Animator component is disabled. Tick it on.", this);

        foreach (AnimationClip clip in clips)
        {
            if (clip.isHumanMotion != animator.isHuman)
            {
                Debug.LogWarning($"{name}: RIG MISMATCH - clip '{clip.name}' is {(clip.isHumanMotion ? "Humanoid" : "Generic")} " +
                                 $"but the model is {(animator.isHuman ? "Humanoid" : "Generic")}. Use clips with the same rig type as the model, " +
                                 "or change the model's Rig tab to match, otherwise the clip does nothing.", this);
            }

            if (clip.length < 0.1f)
                Debug.LogWarning($"{name}: clip '{clip.name}' is only {clip.length:0.00}s long - it may be a single pose, not a real animation.", this);
        }
    }

    private void BuildGraph()
    {
        bool hasRun = runClip != null;

        clips = hasRun ? new[] { idleClip, walkClip, runClip } : new[] { idleClip, walkClip };
        players = new AnimationClipPlayable[clips.Length];

        graph = PlayableGraph.Create("GuardAnimation_" + name);
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "GuardAnimationOutput", animator);
        mixer = AnimationMixerPlayable.Create(graph, clips.Length);

        for (int i = 0; i < clips.Length; i++)
        {
            players[i] = AnimationClipPlayable.Create(graph, clips[i]);
            graph.Connect(players[i], 0, mixer, i);
        }

        mixer.SetInputWeight(0, 1f); // start on idle
        output.SetSourcePlayable(mixer);
        graph.Play();
    }

    void Update()
    {
        if (!ready)
            return;

        // How fast is the guard actually moving?
        float speed;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            speed = agent.velocity.magnitude;
        }
        else
        {
            speed = (transform.position - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        }
        lastPosition = transform.position;

        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, Time.deltaTime * blendSmoothing);

        ApplyWeights(smoothedSpeed);
        LoopClips();

        if (debugLog)
        {
            debugTimer += Time.deltaTime;
            if (debugTimer >= 1f)
            {
                debugTimer = 0f;
                Debug.Log($"{name}: speed {speed:0.00} (smoothed {smoothedSpeed:0.00})", this);
            }
        }
    }

    // Idle -> walk -> run, blended smoothly by speed.
    private void ApplyWeights(float speed)
    {
        bool hasRun = runClip != null;

        float walkRef = Mathf.Max(0.01f, walkSpeedReference);
        float runRef = Mathf.Max(walkRef + 0.01f, runSpeedReference);

        float idleWeight = 0f, walkWeight = 0f, runWeight = 0f;

        if (speed <= 0.05f)
        {
            idleWeight = 1f;
        }
        else if (speed < walkRef)
        {
            float t = speed / walkRef;
            idleWeight = 1f - t;
            walkWeight = t;
        }
        else if (hasRun && speed < runRef)
        {
            float t = (speed - walkRef) / (runRef - walkRef);
            walkWeight = 1f - t;
            runWeight = t;
        }
        else if (hasRun)
        {
            runWeight = 1f;
        }
        else
        {
            walkWeight = 1f;
        }

        mixer.SetInputWeight(0, idleWeight);
        mixer.SetInputWeight(1, walkWeight);
        if (hasRun)
            mixer.SetInputWeight(2, runWeight);

        // With no run clip, play the walk clip faster while chasing so it still looks quicker.
        if (!hasRun)
            players[1].SetSpeed(Mathf.Clamp(speed / walkRef, 1f, 2f));
    }

    // Keeps every clip looping even if "Loop Time" isn't ticked on the clip itself.
    private void LoopClips()
    {
        for (int i = 0; i < players.Length; i++)
        {
            double length = clips[i].length;
            if (length > 0.0 && players[i].GetTime() >= length)
                players[i].SetTime(players[i].GetTime() % length);
        }
    }

    void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();
    }
}