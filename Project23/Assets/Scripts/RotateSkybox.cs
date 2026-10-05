using UnityEngine;

public class RotateSkybox : MonoBehaviour
{
    public float startingRotation = 0f;
    public float rotationSpeed = 0.05f;

    void Start()
    {
        RenderSettings.skybox.SetFloat("_Rotation", startingRotation);
    }


    void Update()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * rotationSpeed);
    }
}