using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BPMManager : MonoBehaviour
{
    public static BPMManager Instance;
    public BeatLineGenerator beatLineGenerator;
    public TMP_InputField bpmInput;
    public float? bpm = null;

    void Awake() { Instance = this; }

    void Start()
    {
        if (bpmInput != null)
        {
            bpmInput.onEndEdit.AddListener(OnBPMChanged);
            if (bpm.HasValue) bpmInput.text = bpm.Value.ToString();
        }
    }

    private void OnBPMChanged(string value)
    {
        if (float.TryParse(value, out float v))
        {
            bpm = v;
            if (beatLineGenerator != null) beatLineGenerator.SetBeatLine();
        }
        else
        {
            if (bpmInput != null && bpm.HasValue) bpmInput.text = bpm.Value.ToString();
        }
    }

    public void SetBPM(float newBpm)
    {
        bpm = newBpm;
        if (bpmInput != null) bpmInput.text = bpm.ToString();
        if (beatLineGenerator != null) beatLineGenerator.SetBeatLine();
    }
}
