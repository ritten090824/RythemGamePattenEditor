using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 리듬 게임의 노트를 타임라인에 배치하는 에디터 클래스
/// </summary>
public class NotePlacer : MonoBehaviour
{
    [Header("References")]
    /// <summary>타임라인 스크롤러 참조</summary>
    public TimelineScroller timelineScroller;
    /// <summary>비트 라인 생성기 참조</summary>
    public BeatLineGenerator beatLineGenerator;
    /// <summary>캔버스 RectTransform</summary>
    public RectTransform canvasTransform;
    /// <summary>팔레트 영역 (노트 배치 가능 영역)</summary>
    public RectTransform paletteArea;
    /// <summary>일반 노트 프리팹</summary>
    public GameObject notePrefab;
    /// <summary>롱노트 바디 프리팹</summary>
    public GameObject longNoteBodyPrefab;
    /// <summary>FX 노트 프리팹</summary>
    public GameObject fxNotePrefab;
    /// <summary>FX 롱노트 바디 프리팹</summary>
    public GameObject fxLongNoteBodyPrefab;
    /// <summary>오디오 소스 참조</summary>
    public AudioSource audioSource;
    /// <summary>BPM 매니저 참조</summary>
    public BPMManager bpmManager;

    [Header("Lane Settings")]
    /// <summary>일반 노트 레인의 X 좌표 배열</summary>
    public float[] laneXPositions = { -151.5f, -50.5f, 50.5f, 151.5f };
    /// <summary>FX 노트 레인의 X 좌표 배열</summary>
    public float[] fxLaneXPositions = { -101f, 101f };

    [Header("Beat Snap Settings")]
    /// <summary>비트 스냅 임계값 (픽셀)</summary>
    public float beatSnapThreshold = 15f;

    [Header("Global Offsets")]
    /// <summary>Y축 오프셋</summary>
    public float yOffset = 0f;

    /// <summary>현재 일반 롱노트 배치 중인지 여부</summary>
    private bool isPlacingLongNote = false;
    /// <summary>현재 FX 롱노트 배치 중인지 여부</summary>
    private bool isPlacingFxLongNote = false;

    /// <summary>롱노트 시작 시간</summary>
    private float longNoteStartTime;
    /// <summary>롱노트가 배치된 레인 인덱스</summary>
    private int longNoteLane;
    /// <summary>FX 롱노트가 배치된 FX 레인 인덱스</summary>
    private int longFxLane;
    /// <summary>롱노트의 시작 노트 게임오브젝트</summary>
    private GameObject startNote;
    /// <summary>롱노트의 종료 노트 게임오브젝트</summary>
    private GameObject endNote;
    /// <summary>롱노트의 바디(연결선) 게임오브젝트</summary>
    private GameObject longBody;

    // ============================================================
    // 중복 생성 방지
    // ============================================================
    /// <summary>
    /// 해당 레인과 시간에 이미 노트가 존재하는지 확인
    /// </summary>
    /// <param name="lane">레인 인덱스</param>
    /// <param name="snappedTime">비트 스냅된 시간</param>
    /// <param name="isFx">FX 노트 여부</param>
    /// <returns>노트가 이미 존재하면 true</returns>
    private bool IsNoteAlreadyExists(int lane, float snappedTime, bool isFx)
    {
        foreach (var note in FindObjectsOfType<Note>())
        {
            if (note.data == null) continue;

            // FX / 일반 구분
            if (note.data.isSpecial != isFx) continue;

            // lane 비교 (FX는 lane 필드를 fx 인덱스로 사용)
            if (note.data.lane != lane) continue;

            if (!note.data.isLong)
            {
                // 단일 노트의 경우, 시간이 일치하면 중복
                if (Mathf.Approximately(note.data.time, snappedTime)) return true;
            }
            else
            {
                // 롱노트의 시작/끝과 범위 검사
                if (Mathf.Approximately(note.data.time, snappedTime)) return true;
                if (Mathf.Approximately(note.data.endTime, snappedTime)) return true;

                // 롱노트 범위 내에 클릭 위치가 있으면 중복
                float minT = Mathf.Min(note.data.time, note.data.endTime);
                float maxT = Mathf.Max(note.data.time, note.data.endTime);
                if (snappedTime > minT && snappedTime < maxT) return true;
            }
        }
        return false;
    }

    // ============================================================
    // Export (노트 데이터 추출)
    // ============================================================
    /// <summary>
    /// 현재 배치된 모든 노트를 NoteData 리스트로 추출
    /// </summary>
    /// <returns>노트 데이터 리스트</returns>
    public List<NoteData> ExportNotes()
    {
        List<NoteData> result = new List<NoteData>();
        foreach (var note in FindObjectsOfType<Note>())
        {
            if (note.data == null) continue;
            if (note.data.isLong)
            {
                // startNote가 null인 (바디가 아닌 실제 데이터 하나만) 항목만 저장
                if (note.startNote == null)
                    result.Add(note.data);
            }
            else result.Add(note.data);
        }
        return result;
    }

    // ============================================================
    // Import (노트 데이터 불러오기)
    // ============================================================
    /// <summary>
    /// 저장된 노트 데이터를 타임라인에 배치
    /// </summary>
    /// <param name="notes">배치할 노트 데이터 리스트</param>
    public void ImportNotes(List<NoteData> notes)
{
    if (timelineScroller == null || beatLineGenerator == null) return;
    if (notes == null || notes.Count == 0) return;

    // -----------------------------------------------------------
    // ⭐ 1. 패턴 전체의 최소 시간 (Pivot 기준) 계산
    // -----------------------------------------------------------
    float minTime = float.MaxValue;
    
    foreach (var d in notes)
    {
        // 롱노트의 경우, time과 endTime 중 더 빠른 값(더 작은 값)을 기준으로 삼습니다.
        float currentMin = d.isLong ? Mathf.Min(d.time, d.endTime) : d.time;
        if (currentMin < minTime)
            minTime = currentMin;
    }
    
    // 유효성 검사 및 보정 여부 결정
    // 최소 시간이 0보다 크고, 최대값이 아닌 경우에만 보정합니다.
    if (minTime > 0f && minTime != float.MaxValue)
    {
        // -----------------------------------------------------------
        // ⭐ 2. 노트 데이터 시간 보정 (Offset 적용)
        // -----------------------------------------------------------
        foreach (var d in notes)
        {
            d.time -= minTime;
            if (d.isLong)
            {
                d.endTime -= minTime;
            }
        }
    }

    // -----------------------------------------------------------
    // 3. 노트 오브젝트 생성 및 배치 (기존 User 코드)
    // -----------------------------------------------------------

    RectTransform parent = timelineScroller.palette;

    // 비트라인 설정 값 가져오기
    float anchor = beatLineGenerator.anchorOffset; // Time 0 Y position
    float px = beatLineGenerator.pixelsPerSecond;

    foreach (var d in notes)
    {
        bool isFx = d.isSpecial;
        GameObject prefab = isFx ? fxNotePrefab : notePrefab;
        GameObject bodyPrefab = isFx ? fxLongNoteBodyPrefab : longNoteBodyPrefab;

        // 레인의 X 좌표 설정
        float xPos = isFx ? fxLaneXPositions[Mathf.Clamp(d.lane, 0, fxLaneXPositions.Length - 1)]
                             : laneXPositions[Mathf.Clamp(d.lane, 0, laneXPositions.Length - 1)];

        if (!d.isLong)
        {
            // 단일 노트 생성
            GameObject n = Instantiate(prefab, parent);
            if (isFx) n.transform.SetAsFirstSibling();

            Note note = n.GetComponent<Note>();
            note.data = d;
            note.noteTime = d.time; // d.time은 이제 보정된 시간 (첫 노트는 0.0초)
            note.laneX = xPos;

            n.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, anchor + d.time * px);
        }
        else
        {
            // 롱노트 생성
            string gid = System.Guid.NewGuid().ToString();
            float startY = anchor + d.time * px;
            float endY = anchor + d.endTime * px;
            float bottomY = Mathf.Min(startY, endY);
            float height = Mathf.Abs(endY - startY);

            // 롱노트 바디(연결선) 생성
            GameObject body = Instantiate(bodyPrefab, parent);
            if (isFx) body.transform.SetAsFirstSibling();
            RectTransform bRT = body.GetComponent<RectTransform>();
            bRT.pivot = new Vector2(0.5f, 0f);
            bRT.anchoredPosition = new Vector2(xPos, bottomY);
            bRT.sizeDelta = new Vector2(bRT.sizeDelta.x, height);

            Note bodyNote = body.GetComponent<Note>();
            bodyNote.data = d;
            bodyNote.noteTime = d.time;
            bodyNote.laneX = xPos;
            bodyNote.longNoteGroupID = gid;

            // 롱노트 시작점 생성
            GameObject sObj = Instantiate(prefab, parent);
            if (isFx) sObj.transform.SetAsFirstSibling();
            sObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, startY);

            Note sNote = sObj.GetComponent<Note>();
            // 롱노트 시작점 데이터 생성 (보정된 d.time 사용)
            sNote.data = new NoteData { time = d.time, endTime = d.endTime, lane = d.lane, isLong = true, isSpecial = d.isSpecial };
            sNote.noteTime = d.time;
            sNote.laneX = xPos;
            sNote.longNoteGroupID = gid;

            // 롱노트 종료점 생성
            GameObject eObj = Instantiate(prefab, parent);
            if (isFx) eObj.transform.SetAsFirstSibling();
            eObj.SetActive(true);
            eObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, endY);

            Note eNote = eObj.GetComponent<Note>();
            // 롱노트 종료점 데이터 생성 (보정된 d.endTime 사용)
            eNote.data = new NoteData { time = d.endTime, endTime = d.endTime, lane = d.lane, isLong = true, isSpecial = d.isSpecial };
            eNote.noteTime = d.endTime;
            eNote.laneX = xPos;
            eNote.longNoteGroupID = gid;

            // 롱노트의 각 부분을 서로 연결
            sNote.endNote = eNote;
            sNote.bodyNote = bodyNote;
            eNote.startNote = sNote;
            bodyNote.startNote = sNote;
            bodyNote.endNote = eNote;
        }
    }
}

    // ============================================================
    // Update (입력 처리)
    // ============================================================
    /// <summary>
    /// 매 프레임마다 마우스 입력을 처리하고 노트를 배치
    /// </summary>
    void Update()
    {
        if (timelineScroller == null || beatLineGenerator == null) return;
        if (audioSource == null) return;

        // 캔버스 설정 가져오기
        Canvas parentCanvas = canvasTransform != null ? canvasTransform.GetComponentInParent<Canvas>() : null;
        Camera cam = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera) ? parentCanvas.worldCamera : null;

        RectTransform parent = timelineScroller.palette;
        if (parent == null) return;

        // ====================================================================
        // 마우스 좌클릭 → 노트 생성 시작
        // ====================================================================
        if (Input.GetMouseButtonDown(0))
        {
            // paletteArea 밖이면 무시
            if (paletteArea != null && !RectTransformUtility.RectangleContainsScreenPoint(paletteArea, Input.mousePosition, cam))
                return;

            // 마우스 위치를 팔레트 로컬 좌표로 변환
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Input.mousePosition, cam, out Vector2 localPos);

            int lane = GetNearestLane(localPos.x);
            float? snappedTime = GetNearestBeatTime(localPos.y);
            if (snappedTime == null) return;

            float anchor = beatLineGenerator.anchorOffset;
            float px = beatLineGenerator.pixelsPerSecond;
            float snappedY = anchor + (snappedTime.Value * px);

            // 특수 키 확인
            bool isAlt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (isAlt)
            {
                // Alt 키: FX 노트 생성
                int fxLane = (localPos.x < 0) ? 0 : 1;
                float fxX = fxLaneXPositions[fxLane];

                if (isShift)
                {
                    // Shift + Alt: FX 롱노트 시작
                    if (IsNoteAlreadyExists(fxLane, snappedTime.Value, true)) return;
                    StartLongNote(true, snappedTime.Value, fxX, parent, snappedY, fxLane);
                }
                else
                {
                    // Alt: FX 단일 노트 생성
                    if (IsNoteAlreadyExists(fxLane, snappedTime.Value, true)) return;
                    CreateSingleNote(true, fxX, snappedY, snappedTime.Value, parent, fxLane);
                }
            }
            else
            {
                // 일반 노트 생성
                float laneX = laneXPositions[lane];

                if (isShift)
                {
                    // Shift: 롱노트 시작
                    if (IsNoteAlreadyExists(lane, snappedTime.Value, false)) return;
                    StartLongNote(false, snappedTime.Value, laneX, parent, snappedY, lane);
                }
                else
                {
                    // 일반: 단일 노트 생성
                    if (IsNoteAlreadyExists(lane, snappedTime.Value, false)) return;
                    CreateSingleNote(false, laneX, snappedY, snappedTime.Value, parent, lane);
                }
            }
        }

        // ====================================================================
        // 롱노트 드래그 중 - 길이 조정
        // ====================================================================
        if ((isPlacingLongNote || isPlacingFxLongNote) && startNote != null && longBody != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Input.mousePosition, cam, out Vector2 localPos);
            float? snappedTime = GetNearestBeatTime(localPos.y);
            if (snappedTime != null && snappedTime.Value > longNoteStartTime)
            {
                float anchor = beatLineGenerator.anchorOffset;
                float px = beatLineGenerator.pixelsPerSecond;

                float startY = anchor + longNoteStartTime * px;
                float endY = anchor + snappedTime.Value * px;
                float laneX = isPlacingFxLongNote ? fxLaneXPositions[longFxLane] : laneXPositions[longNoteLane];

                // 종료 노트 위치 업데이트
                endNote.SetActive(true);
                endNote.GetComponent<RectTransform>().anchoredPosition = new Vector2(laneX, endY);

                // 롱노트 바디 길이 업데이트
                RectTransform bodyRT = longBody.GetComponent<RectTransform>();
                float bottom = Mathf.Min(startY, endY);
                float height = Mathf.Abs(endY - startY);
                bodyRT.anchoredPosition = new Vector2(laneX, bottom);
                bodyRT.sizeDelta = new Vector2(bodyRT.sizeDelta.x, height);

                // 노트 데이터 업데이트
                Note sNote = startNote.GetComponent<Note>();
                Note eNote = endNote.GetComponent<Note>();
                Note bNote = longBody.GetComponent<Note>();

                sNote.data.endTime = snappedTime.Value;
                bNote.data.endTime = snappedTime.Value;

                eNote.data = new NoteData
                {
                    time = snappedTime.Value,
                    endTime = snappedTime.Value,
                    lane = sNote.data.lane,
                    isLong = true,
                    isSpecial = sNote.data.isSpecial
                };
                eNote.noteTime = snappedTime.Value;
                eNote.laneX = laneX;
            }
        }

        // ====================================================================
        // 마우스 좌클릭 해제 → 롱노트 확정 (드래그 없으면 단일 노트로 변환)
        // ====================================================================
        if (Input.GetMouseButtonUp(0) && (isPlacingLongNote || isPlacingFxLongNote))
        {
            float minLengthPx = 1f; // 최소 길이(픽셀) - 이 이상이어야 롱노트로 인정
            bool valid = false;

            if (endNote != null && longBody != null)
            {
                RectTransform bRT = longBody.GetComponent<RectTransform>();
                if (bRT.sizeDelta.y >= minLengthPx) valid = true;
            }

            if (!valid)
            {
                // 드래그가 충분하지 않으면 롱노트 오브젝트를 제거하고 단일 노트로 변환
                float anchor = beatLineGenerator.anchorOffset;
                float px = beatLineGenerator.pixelsPerSecond;
                float snappedY = anchor + (longNoteStartTime * px);

                bool isFx = isPlacingFxLongNote;
                float x = isFx ? fxLaneXPositions[longFxLane] : laneXPositions[longNoteLane];
                int laneIndex = isFx ? longFxLane : longNoteLane;

                if (startNote != null) Destroy(startNote);
                if (endNote != null) Destroy(endNote);
                if (longBody != null) Destroy(longBody);

                CreateSingleNote(isFx, x, snappedY, longNoteStartTime, parent, laneIndex);
            }

            // 롱노트 배치 상태 초기화
            isPlacingLongNote = false;
            isPlacingFxLongNote = false;
            startNote = null;
            endNote = null;
            longBody = null;
        }

        // ====================================================================
        // 마우스 우클릭 → 노트 삭제
        // ====================================================================
        if (Input.GetMouseButtonDown(1))
        {
            PointerEventData pd = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pd, results);

            foreach (var r in results)
            {
                Note note = r.gameObject.GetComponent<Note>();
                if (note != null)
                {
                    if (!string.IsNullOrEmpty(note.longNoteGroupID))
                    {
                        // 롱노트 전체 그룹 삭제
                        foreach (var n in FindObjectsOfType<Note>())
                        {
                            if (n.longNoteGroupID == note.longNoteGroupID) Destroy(n.gameObject);
                        }
                    }
                    else
                    {
                        // 단일 노트 삭제
                        Destroy(note.gameObject);
                    }

                    break;
                }
            }
        }
    }

    /// <summary>
    /// 단일 노트를 생성하고 팔레트에 배치
    /// </summary>
    /// <param name="isFx">FX 노트 여부</param>
    /// <param name="x">노트의 X 좌표</param>
    /// <param name="snappedY">비트 스냅된 Y 좌표</param>
    /// <param name="snappedTime">비트 스냅된 시간</param>
    /// <param name="parent">배치될 부모 RectTransform</param>
    /// <param name="lane">레인 인덱스</param>
    private void CreateSingleNote(bool isFx, float x, float snappedY, float snappedTime, RectTransform parent, int lane = 0)
    {
        GameObject prefab = isFx ? fxNotePrefab : notePrefab;
        GameObject obj = Instantiate(prefab, parent);
        if (isFx) obj.transform.SetAsFirstSibling();

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, snappedY);

        Note n = obj.GetComponent<Note>();
        n.data = new NoteData { time = snappedTime, lane = lane, isSpecial = isFx };
        n.noteTime = snappedTime;
        n.laneX = x;
    }

    /// <summary>
    /// 롱노트 배치를 시작 (시작 노트, 종료 노트, 바디 생성)
    /// </summary>
    /// <param name="isFx">FX 노트 여부</param>
    /// <param name="startTime">롱노트 시작 시간</param>
    /// <param name="x">노트의 X 좌표</param>
    /// <param name="parent">배치될 부모 RectTransform</param>
    /// <param name="snappedY">비트 스냅된 Y 좌표</param>
    /// <param name="lane">레인 인덱스</param>
    private void StartLongNote(bool isFx, float startTime, float x, RectTransform parent, float snappedY, int lane = 0)
    {
        string gid = System.Guid.NewGuid().ToString();
        longNoteStartTime = startTime;
        longNoteLane = lane;
        longFxLane = lane;
        isPlacingLongNote = !isFx;
        isPlacingFxLongNote = isFx;

        GameObject notePrefabToUse = isFx ? fxNotePrefab : notePrefab;
        GameObject bodyPrefabToUse = isFx ? fxLongNoteBodyPrefab : longNoteBodyPrefab;

        // 롱노트 시작점 생성
        startNote = Instantiate(notePrefabToUse, parent);
        if (isFx) startNote.transform.SetAsFirstSibling();
        startNote.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, snappedY);

        Note sNote = startNote.GetComponent<Note>();
        sNote.data = new NoteData { time = startTime, endTime = startTime, lane = lane, isLong = true, isSpecial = isFx };
        sNote.noteTime = startTime;
        sNote.laneX = x;
        sNote.longNoteGroupID = gid;

        // 롱노트 종료점 생성 (처음엔 보이지 않음)
        endNote = Instantiate(notePrefabToUse, parent);
        if (isFx) endNote.transform.SetAsFirstSibling();
        endNote.SetActive(false);
        Note eNote = endNote.GetComponent<Note>();
        eNote.longNoteGroupID = gid;

        // 롱노트 바디(연결선) 생성
        longBody = Instantiate(bodyPrefabToUse, parent);
        if (isFx) longBody.transform.SetAsFirstSibling();
        RectTransform bRT = longBody.GetComponent<RectTransform>();
        bRT.pivot = new Vector2(0.5f, 0f);
        bRT.anchoredPosition = startNote.GetComponent<RectTransform>().anchoredPosition;
        bRT.sizeDelta = new Vector2(bRT.sizeDelta.x, 0); // 처음엔 높이 0
        longBody.SetActive(true);

        Note bNote = longBody.GetComponent<Note>();
        bNote.data = sNote.data;
        bNote.noteTime = startTime;
        bNote.laneX = x;
        bNote.longNoteGroupID = gid;

        // 각 노트 부분들을 서로 연결
        sNote.bodyNote = bNote;
        sNote.endNote = eNote;
        bNote.startNote = sNote;
        bNote.endNote = eNote;
        eNote.startNote = sNote;
        eNote.bodyNote = bNote;
    }

    /// <summary>
    /// 마우스 X 위치에서 가장 가까운 레인 인덱스 반환
    /// </summary>
    /// <param name="mouseX">마우스의 X 좌표</param>
    /// <returns>가장 가까운 레인의 인덱스</returns>
    int GetNearestLane(float mouseX)
    {
        int lane = 0;
        float min = Mathf.Abs(mouseX - laneXPositions[0]);
        for (int i = 1; i < laneXPositions.Length; i++)
        {
            float dist = Mathf.Abs(mouseX - laneXPositions[i]);
            if (dist < min)
            {
                min = dist;
                lane = i;
            }
        }
        return lane;
    }

    /// <summary>
    /// Y 좌표(팔레트 기준)에서 비트 스냅된 시간 반환
    /// </summary>
    /// <param name="yPalette">팔레트의 Y 좌표</param>
    /// <returns>비트 스냅된 시간 (또는 null)</returns>
    float? GetNearestBeatTime(float yPalette)
    {
        if (bpmManager == null || !bpmManager.bpm.HasValue || bpmManager.bpm.Value <= 0f)
            return null;

        float bpm = bpmManager.bpm.Value;
        float secPerBeat = 60f / bpm; // 한 비트당 시간(초)
        float pxPerSec = beatLineGenerator.pixelsPerSecond;
        int division = Mathf.Max(1, beatLineGenerator.CurrentDivision); // 비트 분할 (1/4, 1/8, 1/16 등)

        // 클릭 위치(팔레트 좌표) → 시간으로 변환
        float timeByPos = (yPalette - beatLineGenerator.anchorOffset) / pxPerSec;

        // 스냅 간격 계산 및 반올림
        float snapStep = secPerBeat / division;
        float snappedTime = Mathf.Round(timeByPos / snapStep) * snapStep;

        return snappedTime;
    }
}
