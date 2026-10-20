using UnityEngine;
using UnityEngine.AI;

public class GuardPatrol : MonoBehaviour
{
    [Header("Patrol Route")]
    [Tooltip("Optional. Drag an empty GameObject here (e.g. placed at the back of the room) to set exactly " +
             "where the guard patrols to. Overrides Walk Distance and the direction option below.")]
    public Transform patrolEndPoint;

    [Tooltip("How far it walks before turning around (ignored if Patrol End Point is set).")]
    public float walkDistance = 5f;

    [Tooltip("Off = patrol toward world-right (the old behaviour). " +
             "On = patrol in the direction the guard is facing in the editor, so you can aim the route by rotating the guard.")]
    public bool patrolAlongGuardForward = false;

    [Header("Walk Settings")]
    public float walkSpeed = 2f;       // how fast it moves

    [Tooltip("How fast the guard turns to face its direction of travel (degrees/second). " +
             "Raise this if guards slide sideways while turning around.")]
    public float turnSpeed = 360f;

    [Tooltip("How close the guard needs to get to a patrol point before turning around.")]
    public float arriveDistance = 0.5f;

    [Tooltip("Set to false by GuardDetection while chasing/investigating so patrol doesn't fight it.")]
    public bool patrolling = true;

    private const float SnapRadius = 2f;

    // The two patrol points are fixed once at Start and never move, so after a
    // chase the guard always returns to its original route (pathing around walls).
    private Vector3 startPos;
    private Vector3 targetPos;
    private bool movingForward = true;

    private NavMeshAgent agent;
    private bool destinationSet = false;
    private bool wasPatrolling = true;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            // The agent turns the guard to face where it is walking, so the guard
            // (and any model on it) always walks forwards.
            agent.updateRotation = true;
            agent.angularSpeed = turnSpeed;
        }
    }

    void Start()
    {
        startPos = transform.position;
        targetPos = ComputeFarPoint(startPos);

        if (agent == null)
        {
            Debug.LogWarning($"{name}: no NavMeshAgent found - guard will walk in straight lines THROUGH WALLS. Add a NavMeshAgent component.", this);
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name}: NavMeshAgent is not on a NavMesh - falling back to straight-line movement (through walls). " +
                             "Bake a NavMesh and make sure the guard starts on it.", this);
            return;
        }

        ValidateRoute();

        // Snap points onto the NavMesh so they're always reachable.
        startPos = Snap(startPos);
        targetPos = Snap(targetPos);
    }

    void Update()
    {
        // Detection is in control - don't touch the agent or transform.
        if (!patrolling)
        {
            wasPatrolling = false;
            return;
        }

        // Just resumed patrol: re-issue the destination.
        if (!wasPatrolling)
        {
            wasPatrolling = true;
            destinationSet = false;
        }

        Vector3 destination = movingForward ? targetPos : startPos;

        if (agent != null && agent.isOnNavMesh)
        {
            if (!destinationSet)
            {
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.SetDestination(destination);
                destinationSet = true;
                return;
            }

            bool closeEnough = HorizontalDistance(transform.position, destination) <= arriveDistance;
            bool pathFinished = agent.hasPath && !agent.pathPending
                                && agent.remainingDistance <= agent.stoppingDistance + 0.1f;

            if (closeEnough || pathFinished)
            {
                movingForward = !movingForward;
                destinationSet = false;
            }
        }
        else
        {
            // Fallback for guards without a NavMeshAgent (straight line, no wall avoidance).
            FaceDirection(destination - transform.position);
            transform.position = Vector3.MoveTowards(transform.position, destination, walkSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, destination) < 0.05f)
                movingForward = !movingForward;
        }
    }

    // Called by GuardDetection when handing control back. The patrol route itself
    // is NOT moved - this just makes the guard re-target its current patrol point.
    public void ResetPatrolOrigin()
    {
        destinationSet = false;
    }

    // Smoothly turns the guard (flat, no tilting) toward a direction.
    // Only needed for the no-agent fallback - an agent rotates itself.
    public void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    // ---------- Route setup ----------

    // Where the far end of the patrol is, given where the guard starts.
    private Vector3 ComputeFarPoint(Vector3 from)
    {
        if (patrolEndPoint != null)
            return patrolEndPoint.position;

        Vector3 direction = patrolAlongGuardForward ? transform.forward : Vector3.right;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.right;

        return from + direction.normalized * walkDistance;
    }

    // Warns (in the Console) if the route can't actually be walked, which is the usual
    // problem after moving a guard somewhere new.
    private void ValidateRoute()
    {
        if (!NavMesh.SamplePosition(targetPos, out NavMeshHit farHit, SnapRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: the far patrol point is off the NavMesh (more than {SnapRadius}m from walkable floor). " +
                             "Move the guard, shorten Walk Distance, tick Patrol Along Guard Forward and rotate it, " +
                             "or assign a Patrol End Point.", this);
            return;
        }

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(startPos, farHit.position, NavMesh.AllAreas, path) && path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning($"{name}: no complete walkable path from the guard to its far patrol point " +
                             "(a wall may be in the way, or the point is in a different room). " +
                             "Move the guard or assign a Patrol End Point on the same side of the wall.", this);
        }
    }

    private Vector3 Snap(Vector3 point)
    {
        return NavMesh.SamplePosition(point, out NavMeshHit hit, SnapRadius, NavMesh.AllAreas) ? hit.position : point;
    }

    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // Select a guard in the Scene view to preview its patrol route - even before pressing Play.
    // Cyan line = route. The far end is green if it's on the NavMesh, red if it isn't.
    void OnDrawGizmosSelected()
    {
        Vector3 a, b;

        if (Application.isPlaying)
        {
            a = startPos;
            b = targetPos;
        }
        else
        {
            a = transform.position;
            b = ComputeFarPoint(a);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawWireSphere(a, 0.3f);

        bool farPointWalkable = NavMesh.SamplePosition(b, out NavMeshHit hit, SnapRadius, NavMesh.AllAreas);
        Gizmos.color = farPointWalkable ? Color.green : Color.red;
        Gizmos.DrawWireSphere(b, 0.3f);
    }
}