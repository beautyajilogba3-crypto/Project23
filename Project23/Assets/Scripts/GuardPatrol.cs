using UnityEngine;

public class GuardPatrol : MonoBehaviour
{
    [Header("Walk Settings")]
    public float walkDistance = 5f;    //how far it walks before turning around
    public float walkSpeed = 2f;       //how fast it moves

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool movingForward = true;

    void Start()
    {
        startPos = transform.position;
        targetPos = startPos + Vector3.right * walkDistance;   
    }

    void Update()
    {
        Vector3 destination = movingForward ? targetPos : startPos;

        transform.position = Vector3.MoveTowards(transform.position, destination, walkSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, destination) < 0.05f)
        {
            movingForward = !movingForward;
        }
    }
}
