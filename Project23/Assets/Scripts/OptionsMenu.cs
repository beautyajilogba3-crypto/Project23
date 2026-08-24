using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    public Slider volumeSlider;

    void Start()
    {
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;

        volumeSlider.value = AudioListener.volume;
    }

    public void ChangeVolume()
    {
        AudioListener.volume = volumeSlider.value;
    }
}