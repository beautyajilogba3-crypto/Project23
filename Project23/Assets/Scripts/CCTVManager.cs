
using UnityEngine;

public class CCTVManager : MonoBehaviour
{
    public Camera[] roomCameras;

    private int currentCamera = 0;

    void Start()
    {
        ShowCamera(0);
    }

    public void ShowCamera(int index)
    {
        if (roomCameras.Length == 0) return;

        currentCamera = (index + roomCameras.Length)
                        % roomCameras.Length;

        for (int i = 0; i < roomCameras.Length; i++)
        {
            roomCameras[i].enabled = (i == currentCamera);
        }
    }

    public void NextCamera()
    {
        ShowCamera(currentCamera + 1);
    }

    public void PreviousCamera()
    {
        ShowCamera(currentCamera - 1);
    }
}