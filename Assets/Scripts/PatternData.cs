using System;
using System.Collections.Generic;

[Serializable]
public class PatternData
{
    public float bpm;
    public string audioPath; // 음원 파일 경로
    public int beat;
    public List<NoteData> notes = new List<NoteData>();
}
[Serializable]
public class GamePatternData
{
    public float bpm;
    public int beat;
    public List<NoteData> notes;
}