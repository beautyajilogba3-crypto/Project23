using UnityEngine;

public class FloatingArrow : MonoBehaviour
{
    public float floatHeight = 8f;
    public float floatSpeed = 2f;

    private RectTransform rectTransform;
    private Vector2 startPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.unscaledTime * floatSpeed) * floatHeight;
        rectTransform.anchoredPosition =
            startPosition + new Vector2(0f, offset);
    }
}
