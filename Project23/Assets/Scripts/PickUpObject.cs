using UnityEngine;

public class PickUpObject : MonoBehaviour
{
    [SerializeField] private Vector3 holdLocalOffset = Vector3.zero;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void PickUp(Transform holdPoint)
    {
        if (rb == null)
            return;

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.SetParent(holdPoint);
        transform.localPosition = holdLocalOffset;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        if (rb == null)
            return;

        transform.SetParent(null);
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    public void Throw(Vector3 impulse)
    {
        if (rb == null)
            return;

        transform.SetParent(null);
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(impulse, ForceMode.Impulse);
    }
}
