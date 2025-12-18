using UnityEngine;
using UnityEngine.UI;

public class TimelineScroller : MonoBehaviour
{
    public AudioSource musicSource;
    public RectTransform palette;
    public float pixelsPerSecond = 100f;
    public Slider timelineSlider;
    public bool scrollUp = true;

    private float musicLength = 1f;
    public float startY;
    private bool autoScroll = true;

    [Header("Sync References")]
    public BeatLineGenerator beatLineGenerator;

    public float ScrollOffset
    {
        get
        {
            if (palette == null) return 0f;
            return palette.anchoredPosition.y - startY;
        }
    }

    void Start()
    {
        if (palette != null) startY = palette.anchoredPosition.y;

        if (musicSource != null && musicSource.clip != null) 
            musicLength = musicSource.clip.length;

        if (beatLineGenerator != null)
        {
            //beatLineGenerator.anchorOffset = startY;
            beatLineGenerator.pixelsPerSecond = pixelsPerSecond;
            pixelsPerSecond = beatLineGenerator.pixelsPerSecond;
        }
    }

    public void beatScrolSet()
    {
        if (musicSource != null && musicSource.clip != null) 
            musicLength = musicSource.clip.length;
    }

    void Update()
    {
        if (musicSource == null || palette == null) return;

        if (beatLineGenerator != null)
            pixelsPerSecond = beatLineGenerator.pixelsPerSecond;

        if (musicSource.clip == null) return;

        float px = pixelsPerSecond;
        float newY = startY;

        // 자동 스크롤 중이면
        if (musicSource.isPlaying && autoScroll)
        {
            newY = scrollUp ? startY + (musicSource.time * px)
                            : startY - (musicSource.time * px);

            palette.anchoredPosition = new Vector2(
                palette.anchoredPosition.x, 
                newY
            );

            if (timelineSlider != null)
                timelineSlider.value = musicSource.time / musicLength;
        }
        else
        {
            // 슬라이더 조작 중일 때
            if (timelineSlider != null)
            {
                float progress = timelineSlider.value;
                float offset = (progress * musicLength) * px;

                newY = scrollUp ? startY + offset : startY - offset;

                palette.anchoredPosition = new Vector2(
                    palette.anchoredPosition.x, 
                    newY
                );

                if (!musicSource.isPlaying)
                    musicSource.time = progress * musicLength;
            }
        }
    }

    // 자동 스크롤 ON/OFF
    public void ToggleAutoScroll(bool value)
    {
        autoScroll = value;
    }
}
