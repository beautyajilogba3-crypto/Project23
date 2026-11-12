
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CCTVMonitorInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public FPController playerController;
    public CCTVScreenController cctvScreen;
    public Collider monitorCollider;

    [Header("UI Prompts")]
    public TMP_Text interactionPrompt;
    public TMP_Text controlsPrompt;

    [Header("Interaction Settings")]
    public float interactionDistance = 3f;

    private bool isUsingMonitor = false;

    void Start()
    {
        // Interaction prompt
        if (interactionPrompt != null)
        {
            interactionPrompt.text = "PRESS C TO ACCESS CCTV";
            interactionPrompt.gameObject.SetActive(false);
        }

        // CCTV controls guide
        if (controlsPrompt != null)
        {
            controlsPrompt.text =
                "CCTV CONTROL GUIDE\n\n" +
                "CAMERA SELECTION\n" +
                "A - Lobby\n" +
                "B - Egyptian Room\n" +
                "C - Medieval Room\n" +
                "D - Greek Room\n" +
                "E - Corridor\n\n" +
                "TAB - Return to Overview\n" +
                "ESC - Exit CCTV System";

            controlsPrompt.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        // While using CCTV
        if (isUsingMonitor)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ExitMonitor();
            }

            return;
        }

        // Show or hide interaction prompt
        UpdateInteractionPrompt();

        // Press C to access CCTV
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            TryAccessMonitor();
        }
    }

    bool IsLookingAtMonitor()
    {
        if (playerCamera == null || monitorCollider == null)
            return false;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        return monitorCollider.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance
        );
    }

    void UpdateInteractionPrompt()
    {
        if (interactionPrompt == null)
            return;

        bool canInteract = IsLookingAtMonitor();

        interactionPrompt.gameObject.SetActive(canInteract);
    }

    void TryAccessMonitor()
    {
        if (IsLookingAtMonitor())
        {
            EnterMonitor();
        }
    }

    void EnterMonitor()
    {
        if (cctvScreen == null || playerController == null)
        {
            Debug.LogWarning(
                "CCTV Interaction: Missing script references."
            );
            return;
        }

        isUsingMonitor = true;

        // Hide interaction prompt
        if (interactionPrompt != null)
            interactionPrompt.gameObject.SetActive(false);

        // Show CCTV controls
        if (controlsPrompt != null)
            controlsPrompt.gameObject.SetActive(true);

        // Activate CCTV controls
        cctvScreen.SetMonitorActive(true);

        // Disable player movement and mouse-look
        playerController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ExitMonitor()
    {
        isUsingMonitor = false;

        // Return CCTV to overview
        if (cctvScreen != null)
            cctvScreen.SetMonitorActive(false);

        // Hide controls guide
        if (controlsPrompt != null)
            controlsPrompt.gameObject.SetActive(false);

        // Restore player movement
        if (playerController != null)
            playerController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        if (isUsingMonitor)
        {
            isUsingMonitor = false;

            if (cctvScreen != null)
                cctvScreen.SetMonitorActive(false);

            if (interactionPrompt != null)
                interactionPrompt.gameObject.SetActive(false);

            if (controlsPrompt != null)
                controlsPrompt.gameObject.SetActive(false);

            if (playerController != null)
                playerController.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}