using UnityEngine;
using UnityEngine.UI;
using System;

[Serializable]
public class NoteData
{
    public float time;
    public float endTime;
    public int lane;
    public bool isLong;
    public bool isSpecial; // FX 여부
    public int measure;
    public string noteType = "normal";
}

public class Note : MonoBehaviour
{
    public NoteData data;
    public float noteTime;
    public float laneX;

    public Note startNote;
    public Note endNote;
    public Note bodyNote;
    public string longNoteGroupID;

    private BeatLineGenerator beatLineGenerator;
    private TimelineScroller timelineScroller;
    public Image img;

    void Awake()
    {
        beatLineGenerator = FindObjectOfType<BeatLineGenerator>();
        timelineScroller = FindObjectOfType<TimelineScroller>();
        img = GetComponent<Image>();
    }

    void Start()
    {
        ApplyLaneColor();
    }

    void Update()
    {
        UpdateNotePosition();
        UpdateLongNoteBody();
        // ------------------------------------------------------;
    }

    //------------------------------------------------------
    // 🎨 색상 규칙
    // 일반노트(Short)  → 라인별 색
    // 롱노트(Start/End/Body) → 일반노트와 **같은 색**
    // FX 노트(isSpecial==true) → 빨강 유지
    //------------------------------------------------------
    public void ApplyLaneColor()
    {
        if (img == null) img = GetComponent<Image>();
        if (data == null) return;

        // FX 노트 → 빨강 고정
        if (data.isSpecial)
        {
            img.color = new Color32(255, 0, 0, 255);
            return;
        }

        // 일반 노트 및 롱노트 Start/End/Body 모두 동일 규칙 적용
        switch (data.lane)
        {
            case 0:
                img.color = new Color32(255, 255, 255, 255); // 파랑
                break;
            case 1:
                img.color = new Color32(36, 129, 224, 255); // 흰색
                break;
            case 2:
                img.color = new Color32(36, 129, 224, 255); // 흰색
                break;
            case 3:
                img.color = new Color32(255, 255, 255, 255); // 파랑
                break;
        }
    }

    //------------------------------------------------------
    // 노트 위치 계산
    //------------------------------------------------------
    void UpdateNotePosition()
    {
        if (beatLineGenerator == null) return;

        float px = beatLineGenerator.pixelsPerSecond;
        float anchor = beatLineGenerator.anchorOffset;

        float y = anchor + (noteTime * px);

        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = new Vector2(laneX, y);
        else
            transform.localPosition = new Vector3(laneX, y, 0f);
    }

    //------------------------------------------------------
    // 롱노트 바디 업데이트
    //------------------------------------------------------
    void UpdateLongNoteBody()
    {
        if (bodyNote == null && startNote == null && endNote == null) return;

        Note body = this;
        if (bodyNote != null) body = bodyNote;

        if (body.startNote == null || body.endNote == null) return;

        RectTransform bodyRT = body.GetComponent<RectTransform>();
        if (bodyRT == null) return;
        BeatLineGenerator beat = beatLineGenerator ?? FindObjectOfType<BeatLineGenerator>();
        if (beat == null) return;

        float px = beat.pixelsPerSecond;
        float anchor = beat.anchorOffset;

        float startY = anchor + (body.startNote.noteTime * px);
        float endY = anchor + (body.endNote.noteTime * px);

        float bottom = Mathf.Min(startY, endY);
        float length = Mathf.Abs(endY - startY);

        bodyRT.anchoredPosition = new Vector2(body.startNote.laneX, bottom);
        bodyRT.sizeDelta = new Vector2(bodyRT.sizeDelta.x, length);

        // 색상 적용 보장 (롱노트 바디도 일반 노트 색상 적용)
        body.ApplyLaneColor();
    }
}