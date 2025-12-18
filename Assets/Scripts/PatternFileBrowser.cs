using UnityEngine;
using UnityEngine.UI;
using SFB;
using System.Collections.Generic;
using TMPro;

public class PatternFileBrowser : MonoBehaviour
{
    [Header("UI References")]
    public Button saveButton;
    public Button loadButton;
    public Button exportButton;
    public Text statusText;

    [Header("References")]
    public BPMManager bpmManager;
    public NotePlacer notePlacer;
    public MusicController musicController;

    private string lastSavePath = "";

    private void Awake()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButton);

        if (loadButton != null)
            loadButton.onClick.AddListener(OnLoadButton);

        if (exportButton != null)
            exportButton.onClick.AddListener(OnExportButton);
    }

    // ===== SAVE (EDITOR) =====
    public void OnSaveButton()
    {
        PatternData pattern = new PatternData
        {
            bpm = bpmManager != null && bpmManager.bpm.HasValue ? bpmManager.bpm.Value : 120f,
            audioPath = musicController != null ? musicController.currentAudioPath : "",
            beat = bpmManager.beatLineGenerator != null ? bpmManager.beatLineGenerator.CurrentDivision : 1,   // ★ 비트 저장
            notes = notePlacer != null ? notePlacer.ExportNotes() : new List<NoteData>()
        };

        SaveEditorPattern(pattern);
    }

    // ===== LOAD (EDITOR) =====
    public void OnLoadButton()
    {
        LoadPattern(OnPatternLoaded);
    }

    // ===== EXPORT (GAME) =====
    public void OnExportButton()
    {
        if (notePlacer == null || bpmManager == null)
        {
            if (statusText != null) statusText.text = "내보내기 실패: 구성 요소 누락!";
            return;
        }

        GamePatternData gamePattern = new GamePatternData
        {
            bpm = bpmManager.bpm.HasValue ? bpmManager.bpm.Value : 120f,
            beat = bpmManager.beatLineGenerator.CurrentDivision,   // ★ 비트 저장
            notes = notePlacer.ExportNotes()
        };

        var extensions = new[] { new ExtensionFilter("In-game Pattern Files", "rgp") };
        string path = StandaloneFileBrowser.SaveFilePanel("인게임 패턴 저장", "", "pattern.rgp", extensions);

        if (!string.IsNullOrEmpty(path))
        {
            PatternData editorPattern = new PatternData
            {
                bpm = gamePattern.bpm,
                beat = gamePattern.beat,
                notes = gamePattern.notes,
                audioPath = ""
            };

            PatternFileEncoder.SavePatternFile(path, editorPattern);

            if (statusText != null) statusText.text = $"인게임 패턴 저장 완료: {path}";
        }
    }

    public void SaveEditorPattern(PatternData pattern)
    {
        string path = lastSavePath;

        if (string.IsNullOrEmpty(path))
        {
            var extensions = new[] { new ExtensionFilter("Editor Pattern Files", "rgped") };
            path = StandaloneFileBrowser.SaveFilePanel("패턴 저장", "", "pattern.rgped", extensions);
        }

        if (!string.IsNullOrEmpty(path))
        {
            PatternFileEncoder.SavePatternFile(path, pattern);
            lastSavePath = path;

            if (statusText != null) statusText.text = $"저장 완료: {path}";
        }
    }

    public void LoadPattern(System.Action<PatternData> onLoaded)
    {
        var extensions = new[] { new ExtensionFilter("Editor Pattern Files", "rgped") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("패턴 불러오기", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string path = paths[0];
            PatternData data = PatternFileEncoder.LoadPatternFile(path);

            if (data != null)
            {
                onLoaded?.Invoke(data);

                lastSavePath = path;
                if (statusText != null) statusText.text = $"불러오기 성공: {path}";
            }
            else
            {
                if (statusText != null) statusText.text = "불러오기 실패!";
            }
        }
    }

    // ==== LOAD APPLY ====
    private void OnPatternLoaded(PatternData data)
    {
        // BPM 반영
        if (BPMManager.Instance != null)
        {
            BPMManager.Instance.bpm = data.bpm;
            BPMManager.Instance.beatLineGenerator.SetBeatLine();

            if (BPMManager.Instance.bpmInput != null)
            {
                BPMManager.Instance.bpmInput.text = data.bpm.ToString("F2");
                BPMManager.Instance.bpmInput.ForceLabelUpdate();
            }
        }

        // ★ 비트(division) 반영
        if (bpmManager.beatLineGenerator != null)
            bpmManager.beatLineGenerator.SetDivision(data.beat);

        // 오디오 불러오기
        if (!string.IsNullOrEmpty(data.audioPath))
        {
            if (musicController != null)
                musicController.StartCoroutine(musicController.LoadAudio(data.audioPath));
        }

        // 노트 삭제 + 불러오기
        if (notePlacer != null)
        {
            foreach (var x in FindObjectsOfType<Note>())
                Destroy(x.gameObject);

            notePlacer.ImportNotes(data.notes);
        }

        // 비트 라인 재생성
        if (bpmManager.beatLineGenerator != null)
        {
            bpmManager.beatLineGenerator.gameObject.SetActive(true);
            bpmManager.beatLineGenerator.SetBeatLine();
        }

        if (statusText != null)
            statusText.text = $"불러오기 완료 (BPM={data.bpm}, Beat={data.beat}, 노트={data.notes.Count})";
    }
}
