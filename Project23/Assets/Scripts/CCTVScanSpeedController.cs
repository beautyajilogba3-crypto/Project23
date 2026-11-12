
using UnityEngine;

public class CCTVScanSpeedController : MonoBehaviour
{
    [Header("CCTV Screen Controller")]
    public CCTVScreenController screenController;

    [Header("Room Animators")]
    public Animator[] roomAnimators = new Animator[5];

    [Header("Animation Speeds")]
    public float normalSpeed = 1f;
    public float selectedRoomSpeed = 0.25f;

    void Update()
    {
        if (screenController == null)
            return;

        int selectedIndex =
            screenController.SelectedCameraIndex;

        for (int i = 0; i < roomAnimators.Length; i++)
        {
            if (roomAnimators[i] == null)
                continue;

            if (i == selectedIndex)
            {
                roomAnimators[i].speed = selectedRoomSpeed;
            }
            else
            {
                roomAnimators[i].speed = normalSpeed;
            }
        }
    }

    void OnDisable()
    {
        if (roomAnimators == null)
            return;

        foreach (Animator animator in roomAnimators)
        {
            if (animator != null)
                animator.speed = normalSpeed;
        }
    }
}