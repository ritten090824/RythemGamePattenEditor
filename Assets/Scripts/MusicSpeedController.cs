using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicSpeedController : MonoBehaviour
{
    [Header("References")]
    public AudioSource audioSource;
    public Slider speedSlider;
    public TMP_Text speedLabel;

    // 고정된 속도 선택값
    private float[] availableSpeeds = 
        { 0.50f, 0.60f, 0.70f, 0.75f, 0.80f, 0.90f,  1.00f, 1.10f, 1.20f,  1.25f, 1.30f, 1.40f, 1.50f, 1.60f, 1.70f, 1.75f, 1.80f, 1.90f, 2.00f };

    void Start()
    {
        if (audioSource == null)
        {
            Debug.LogError("MusicSpeedController: AudioSource 참조 필요!");
            return;
        }

        // 슬라이더 설정
        speedSlider.minValue = 0;
        speedSlider.maxValue = availableSpeeds.Length - 1;
        speedSlider.wholeNumbers = true;
        speedSlider.onValueChanged.AddListener(OnSpeedChanged);

        // 초기값 1.0 설정
        int defaultIndex = System.Array.IndexOf(availableSpeeds, 1.0f);
        speedSlider.value = defaultIndex >= 0 ? defaultIndex : 4;
        ApplySpeed(availableSpeeds[(int)speedSlider.value]);
    }

    void OnSpeedChanged(float sliderIndex)
    {
        int index = Mathf.RoundToInt(sliderIndex);
        float selectedSpeed = availableSpeeds[index];
        ApplySpeed(selectedSpeed);
    }

    private void ApplySpeed(float speed)
    {
        audioSource.pitch = speed;

        if (speedLabel != null)
            speedLabel.text = $"x{speed:0.00}";
    }
}
