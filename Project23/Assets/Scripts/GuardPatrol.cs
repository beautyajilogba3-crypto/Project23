using UnityEngine;

public class GuardPatrol : MonoBehaviour
{
    [Header("Walk Settings (used if Waypoints is empty)")]
    public float walkDistance = 5f;    // how far it walks before turning around
    public float walkSpeed = 2f;       // how fast it moves

    [Header("Waypoints (optional)")]
    [Tooltip("Leave empty for now if your teammate hasn't built the map yet. " +
             "Once waypoints exist, drag them in and the guard will patrol between them instead.")]
    public Transform[] waypoints;

    [Tooltip("Set to false externally (e.g. by GuardDetection) to pause patrolling during a chase.")]
    public bool patrolling = true;

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool movingForward = true;
    private int currentWaypointIndex = 0;

    void Start()
    {
        startPos = transform.position;
        targetPos = startPos + Vector3.right * walkDistance;
    }

    void Update()
    {
        if (!patrolling)
            return;

        if (waypoints != null && waypoints.Length > 0)
        {
            PatrolWaypoints();
        }
        else
        {
            PatrolBackAndForth();
        }
    }

    private void PatrolBackAndForth()
    {
        Vector3 destination = movingForward ? targetPos : startPos;

        transform.position = Vector3.MoveTowards(transform.position, destination, walkSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, destination) < 0.05f)
        {
            movingForward = !movingForward;
        }
    }

    private void PatrolWaypoints()
    {
        Transform destination = waypoints[currentWaypointIndex];
        if (destination == null)
            return;

        transform.position = Vector3.MoveTowards(transform.position, destination.position, walkSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, destination.position) < 0.05f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    // Called by GuardDetection when it wants the guard to resume patrolling
    // from wherever it currently is, rather than snapping back to its old path.
    public void ResetPatrolOrigin()
    {
        startPos = transform.position;
        targetPos = startPos + Vector3.right * walkDistance;
    }
}