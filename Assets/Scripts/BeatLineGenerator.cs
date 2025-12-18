using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class BeatLineGenerator : MonoBehaviour
{
    [Header("References")]
    public RectTransform content;
    public GameObject linePrefab;
    public TMP_Dropdown divisionDropdown;
    public RectTransform viewport;

    [Header("Settings")]
    public float pixelsPerSecond = 100f;   // 초기값 (기본 간격)
    public int totalBeats = 193;
    public float anchorOffset = 200f;

    private float secPerBeat;
    private int beatDivision = 1;

    // ❗ 기본 간격 저장(배속의 기준)
    private float basePixelsPerSecond;

    // 배속
    private float speedMultiplier = 1f;

    [Header("Optimization")]
    public int extraBuffer = 5;
    private Queue<GameObject> linePool = new Queue<GameObject>();
    private List<GameObject> activeLines = new List<GameObject>();

    public int CurrentDivision => beatDivision;
    public float SecPerBeat => secPerBeat;

    void Awake()
    {
        basePixelsPerSecond = pixelsPerSecond;   // 처음 간격 저장
    }

    void Start()
    {
        List<string> options = new List<string>
        {
            "1/1","1/2","1/4","1/8","1/12","1/16","1/24","1/32"
        };

        divisionDropdown.ClearOptions();
        divisionDropdown.AddOptions(options);
        divisionDropdown.onValueChanged.AddListener(OnDivisionChanged);

        SetBeatLine();
    }

    // ======================================================
    //  BPM 변경 시 라인 갱신
    // ======================================================
    public void SetBeatLine()
    {
        if (BPMManager.Instance != null && BPMManager.Instance.bpm.HasValue)
        {
            secPerBeat = 60f / BPMManager.Instance.bpm.Value;
            RefreshLines();
        }
    }

    // ======================================================
    // ⭐ 외부에서 불러온 패턴의 division(beat) 적용하는 함수
    // ======================================================
    public void SetDivision(int div)
    {
        int[] divisions = { 1, 2, 4, 8, 12, 16, 24, 32 };

        int index = System.Array.IndexOf(divisions, div);
        if (index < 0) index = 0;

        beatDivision = divisions[index];

        // UI 반영
        if (divisionDropdown != null)
            divisionDropdown.value = index;

        RefreshLines();
    }

    // ======================================================
    // ⭐ 배속(노트/비트라인 간격 증가량 조절)
    // ======================================================
    public void SetSpeedMultiplier(float multiplier)
    {
        if (multiplier <= 0f) multiplier = 1f;

        speedMultiplier = multiplier;

        float expandFactor = 0.5f; // 조절용 (0.3~0.6 권장)
        float mul = 1f + ((multiplier - 1f) * expandFactor);

        pixelsPerSecond = basePixelsPerSecond * mul;

        RefreshLines();
    }

    // ======================================================
    // divisionDropdown 에서 직접 변경할 때
    // ======================================================
    void OnDivisionChanged(int index)
    {
        int[] divisions = { 1, 2, 4, 8, 12, 16, 24, 32 };
        beatDivision = divisions[index];

        RefreshLines();
    }

    // ======================================================
    void RefreshLines()
    {
        foreach (var line in activeLines)
            ReturnLine(line);

        activeLines.Clear();
    }

    void Update()
    {
        UpdateVisibleLines();
    }

    // ======================================================
    // 스크롤 시 보이는 라인만 생성 (최적화)
    // ======================================================
    void UpdateVisibleLines()
    {
        if (secPerBeat <= 0f) return;

        float step = (secPerBeat / beatDivision) * pixelsPerSecond;

        float totalHeight = anchorOffset + (totalBeats * secPerBeat * pixelsPerSecond);
        content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);

        if (viewport == null)
        {
            for (int i = 0; i < totalBeats * beatDivision; i++)
            {
                float yPos = anchorOffset + (i * step);
                CreateLine(i, yPos);
            }
            return;
        }

        Vector3[] corners = new Vector3[4];
        viewport.GetWorldCorners(corners);

        Vector2 localMin = content.InverseTransformPoint(corners[0]);
        Vector2 localMax = content.InverseTransformPoint(corners[1]);

        float minY = localMin.y - extraBuffer * step;
        float maxY = localMax.y + extraBuffer * step;

        int startIndex = Mathf.Max(0, Mathf.FloorToInt((minY - anchorOffset) / step));
        int endIndex = Mathf.Min(totalBeats * beatDivision, Mathf.CeilToInt((maxY - anchorOffset) / step));

        foreach (var line in activeLines)
            ReturnLine(line);

        activeLines.Clear();

        for (int i = startIndex; i < endIndex; i++)
        {
            float yPos = anchorOffset + (i * step);
            CreateLine(i, yPos);
        }
    }

    // ======================================================
    private void CreateLine(int index, float y)
    {
        GameObject line = GetLine();
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.SetParent(content, false);
        rt.anchoredPosition = new Vector2(0, y);

        Image img = line.GetComponent<Image>();

        if (index % (4 * beatDivision) == 0)
            img.color = Color.white;
        else if (index % beatDivision == 0)
            img.color = new Color(1, 1, 1, 0.6f);
        else
            img.color = new Color(1, 1, 1, 0.3f);

        activeLines.Add(line);
    }

    // ======================================================
    GameObject GetLine()
    {
        if (linePool.Count > 0)
        {
            GameObject line = linePool.Dequeue();
            line.SetActive(true);
            return line;
        }
        return GameObject.Instantiate(linePrefab);
    }

    void ReturnLine(GameObject line)
    {
        line.SetActive(false);
        linePool.Enqueue(line);
    }
}
