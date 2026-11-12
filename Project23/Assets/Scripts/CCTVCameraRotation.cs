
using UnityEngine;

public class CCTVCameraRotation : MonoBehaviour
{
    [Header("References")]
    public CCTVScreenController screenController;

    [Header("Room Settings")]
    [Range(0, 4)]
    public int roomIndex = 0;

    [Header("Rotation Speeds")]
    public float normalRotationSpeed = 12f;
    public float selectedRotationSpeed = 3f;

    [Header("Rotation Settings")]
    public bool rotateCamera = true;
    public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        if (!rotateCamera)
            return;

        if (screenController == null)
            return;

        // Check whether this room is selected.
        bool isSelected =
            screenController.SelectedCameraIndex == roomIndex;

        // Selected room rotates more slowly.
        float currentSpeed = isSelected
            ? selectedRotationSpeed
            : normalRotationSpeed;

        transform.Rotate(
            rotationAxis.normalized,
            currentSpeed * Time.deltaTime,
            Space.World
        );
    }
}
