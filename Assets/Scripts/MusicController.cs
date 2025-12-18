using UnityEngine;
using UnityEngine.UI;
using SFB;
using System.IO;
using TMPro;

public class MusicController : MonoBehaviour
{
    public TimelineScroller timelineScroller;
    public Button playPauseButton;
    public Button loadButton;
    public Slider timelineSlider;
    public TMP_Text trackNameText;
    public AudioSource audioSource;
    public string currentAudioPath;
    private bool isPlaying = false;

    void Start()
    {
        if (playPauseButton != null) playPauseButton.onClick.AddListener(TogglePlayPause);
        if (loadButton != null) loadButton.onClick.AddListener(LoadAudioFile);
        if (timelineSlider != null) timelineSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void Update()
    {
        if (isPlaying && audioSource != null && audioSource.clip != null && timelineSlider != null)
            timelineSlider.value = audioSource.time / audioSource.clip.length;
    }

    void TogglePlayPause()
    {
        if (audioSource == null || audioSource.clip == null) return;
        if (isPlaying) { audioSource.Pause(); SetPlayButtonIcon("▶"); }
        else { audioSource.Play(); SetPlayButtonIcon("■"); }
        isPlaying = !isPlaying;
    }

    void SetPlayButtonIcon(string s)
    {
        if (playPauseButton == null) return;
        var txt = playPauseButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        if (txt != null) txt.text = s;
    }

    private void OnSliderValueChanged(float value)
    {
        if (audioSource != null && audioSource.clip != null)
            audioSource.time = value * audioSource.clip.length;
    }

    void LoadAudioFile()
    {
        var extensions = new[] { new ExtensionFilter("Audio Files", "mp3", "wav", "ogg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Audio File", "", extensions, false);
        if (paths != null && paths.Length > 0 && File.Exists(paths[0]))
        {
            StartCoroutine(LoadAudio(paths[0]));
        }
    }

    public System.Collections.IEnumerator LoadAudio(string path)
    {
        currentAudioPath = path;
        var url = "file://" + path;
        using (var www = new WWW(url))
        {
            yield return www;
            audioSource.clip = www.GetAudioClip();
            if (trackNameText != null) trackNameText.text = Path.GetFileName(path);
            if (timelineScroller != null) timelineScroller.beatScrolSet();
        }
    }
}
