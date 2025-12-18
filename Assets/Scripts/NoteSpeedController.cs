using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NoteSpeedController : MonoBehaviour
{
    public BeatLineGenerator beatLineGenerator;
    public TimelineScroller timelineScroller;
    public Slider speedSlider;
    public TMP_Text speedValueText;

    public float minSpeed = 0.1f;
    public float maxSpeed = 20f;
    public float initialSpeed = 1f;

    void Start()
    {
        if (speedSlider == null) return;

        speedSlider.minValue = minSpeed;
        speedSlider.maxValue = maxSpeed;
        speedSlider.wholeNumbers = false;
        float init = Mathf.Clamp(initialSpeed, minSpeed, maxSpeed);
        speedSlider.SetValueWithoutNotify(init);
        ApplySpeed(init);
        speedSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    void OnSliderChanged(float raw)
    {
        float stepped = Mathf.Round(raw * 10f) / 10f;
        speedSlider.SetValueWithoutNotify(stepped);
        ApplySpeed(stepped);
    }

    void ApplySpeed(float multiplier)
{
    if (beatLineGenerator != null)
        beatLineGenerator.SetSpeedMultiplier(multiplier);

    if (timelineScroller != null)
        timelineScroller.pixelsPerSecond = beatLineGenerator.pixelsPerSecond;

    if (speedValueText != null)
        speedValueText.text = $"x{multiplier:0.0}";
}

}
