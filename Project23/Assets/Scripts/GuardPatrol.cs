using UnityEngine;
using UnityEngine.AI;

public class GuardPatrol : MonoBehaviour
{
    [Header("Walk Settings")]
    public float walkDistance = 5f;    // how far it walks before turning around
    public float walkSpeed = 2f;       // how fast it moves

    [Tooltip("How close the guard needs to get to a patrol point before turning around.")]
    public float arriveDistance = 0.5f;

    [Tooltip("Set to false by GuardDetection while chasing/investigating so patrol doesn't fight it.")]
    public bool patrolling = true;

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
    }

    void Start()
    {
        startPos = transform.position;
        targetPos = startPos + Vector3.right * walkDistance;

        if (agent == null)
        {
            Debug.LogWarning($"{name}: no NavMeshAgent found - guard will walk in straight lines THROUGH WALLS. Add a NavMeshAgent component.", this);
        }
        else if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name}: NavMeshAgent is not on a NavMesh - falling back to straight-line movement (through walls). Bake a NavMesh and make sure the guard starts on it.", this);
        }

        // Snap points onto the NavMesh so they're always reachable.
        if (agent != null)
        {
            startPos = Snap(startPos);
            targetPos = Snap(targetPos);
        }
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

    private Vector3 Snap(Vector3 point)
    {
        return NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : point;
    }

    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}