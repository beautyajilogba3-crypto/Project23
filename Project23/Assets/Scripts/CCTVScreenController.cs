
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class CCTVScreenController : MonoBehaviour
{
    [Header("Screen Layouts")]
    public GameObject overviewLayout;
    public GameObject selectedCameraLayout;

    [Header("Main Selected Feed")]
    public RawImage selectedFeed;
    public TMP_Text selectedRoomLabel;

    [Header("Four Small Preview Feeds")]
    public RawImage[] previewFeeds = new RawImage[4];

    [Header("Room Render Textures")]
    public RenderTexture[] roomFeeds = new RenderTexture[5];

    [Header("Selected Label Orientation")]
    public bool correctMirroredLabelScale = true;

    private readonly string[] roomNames =
    {
        "Lobby",
        "Egyptian Room",
        "Medieval Room",
        "Greek Room",
        "Corridor"
    };

    // Controls keyboard access, NOT the actual monitor display.
    private bool monitorActive = false;

    // The currently selected camera.
    // -1 means no individual camera is selected.
    public int SelectedCameraIndex { get; private set; } = -1;

    public bool IsMonitorInUse => monitorActive;

    void Start()
    {
        // The monitor displays its overview immediately.
        ShowOverview();
    }

    void Update()
    {
        // Camera controls only work while the player is using CCTV.
        if (!monitorActive || Keyboard.current == null)
            return;

        if (Keyboard.current.aKey.wasPressedThisFrame)
            ShowSelectedCamera(0);

        if (Keyboard.current.bKey.wasPressedThisFrame)
            ShowSelectedCamera(1);

        if (Keyboard.current.cKey.wasPressedThisFrame)
            ShowSelectedCamera(2);

        if (Keyboard.current.dKey.wasPressedThisFrame)
            ShowSelectedCamera(3);

        if (Keyboard.current.eKey.wasPressedThisFrame)
            ShowSelectedCamera(4);

        if (Keyboard.current.tabKey.wasPressedThisFrame)
            ShowOverview();
    }

    // Enables or disables player control of the CCTV interface.
    // The monitor and camera feeds remain visible either way.
    public void SetMonitorActive(bool active)
    {
        monitorActive = active;

        if (!active)
        {
            ShowOverview();
        }
    }

    public void ShowOverview()
    {
        SelectedCameraIndex = -1;

        if (overviewLayout != null)
            overviewLayout.SetActive(true);

        if (selectedCameraLayout != null)
            selectedCameraLayout.SetActive(false);
    }

    public void ShowSelectedCamera(int selectedIndex)
    {
        if (roomFeeds == null ||
            selectedIndex < 0 ||
            selectedIndex >= roomFeeds.Length ||
            selectedIndex >= roomNames.Length)
        {
            return;
        }

        if (selectedFeed == null || selectedRoomLabel == null)
        {
            Debug.LogWarning(
                "CCTV: Selected Feed or Room Label is not assigned."
            );
            return;
        }

        if (selectedIndex >= roomFeeds.Length ||
            roomFeeds[selectedIndex] == null)
        {
            Debug.LogWarning(
                "CCTV: Selected room has no Render Texture assigned."
            );
            return;
        }

        SelectedCameraIndex = selectedIndex;

        if (overviewLayout != null)
            overviewLayout.SetActive(false);

        if (selectedCameraLayout != null)
            selectedCameraLayout.SetActive(true);

        // Display the selected room's live feed.
        selectedFeed.texture = roomFeeds[selectedIndex];

        // Display the correct room name.
        selectedRoomLabel.text = roomNames[selectedIndex];

        // Correct a negative scale directly on the label.
        if (correctMirroredLabelScale)
        {
            RectTransform labelTransform =
                selectedRoomLabel.rectTransform;

            Vector3 scale = labelTransform.localScale;

            labelTransform.localScale = new Vector3(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z)
            );
        }

        // Display the remaining four rooms as previews.
        int previewIndex = 0;

        for (int i = 0; i < roomFeeds.Length; i++)
        {
            if (i == selectedIndex)
                continue;

            if (previewIndex >= previewFeeds.Length)
                break;

            if (previewFeeds[previewIndex] != null)
            {
                previewFeeds[previewIndex].texture = roomFeeds[i];
            }

            previewIndex++;
        }
    }
}
