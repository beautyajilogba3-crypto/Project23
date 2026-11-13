
using UnityEngine;

public class KnightPatrol : MonoBehaviour
{
    public float walkingSpeed = 0.8f;
    public float patrolDistance = 2f;
    public float turnSpeed = 180f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool turning = false;
    private bool walkingForward = true;

    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + transform.forward * patrolDistance;
    }

    void Update()
    {
        if (!turning)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                walkingSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, targetPosition) < 0.02f)
            {
                turning = true;
                targetRotation = Quaternion.Euler(
                    0,
                    transform.eulerAngles.y + 180f,
                    0
                );
            }
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );

            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
            {
                transform.rotation = targetRotation;
                turning = false;
                walkingForward = !walkingForward;

                targetPosition = walkingForward
                    ? startPosition + (transform.forward * patrolDistance)
                    : startPosition;
            }
        }
    }
}
