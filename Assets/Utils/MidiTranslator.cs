using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiParser;
using System.IO;
using System.Runtime.InteropServices;

public struct Note
{
    public int idx;
    public int track;
    public int note;
    public int startTime;
    public int length;
    public int InitialVelocity;
    public int FinalVelocity;
    public float tickPerSecond;
}

public struct TempoChange
{
    public float tickPerSecond;
    public int startTime;
    public TempoChange(float f, int i)
    {
        tickPerSecond = f;
        startTime = i;
    }
}

public class MidiTranslator : MonoBehaviour
{
    static int bpm;
    static List<Note>[] tracks;
    static MidiFile file = null;
    static string filename = "The Bass Part.mid";
    static int exitCount = 3;
    static List<TempoChange> tempoChanges;
    static int noteMax;
    static int noteMin;
    public static string[] noteNames = new string[12]
    {
        "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
    };
    public static string[] noteOctave = new string[11]
    {
        "-1", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"
    };
    public static string NoteToString(int note)
    {
        if (note < 0 || note > 127)
        {
            Debug.Log("Not a 4-Octave Note");
            return "Invalid";
        }
        return noteNames[note % 12] + noteOctave[note / 12];
    }
    public static void Dump()
    {
        var file = new MidiFile("Midi/" + filename);
        string text = "[General]" + System.Environment.NewLine
            + "Format=" + file.Format + System.Environment.NewLine
            + "TracksCount=" + file.TracksCount + System.Environment.NewLine
            + "TicksPerQuarterNote=" + file.TicksPerQuarterNote + System.Environment.NewLine
             + System.Environment.NewLine;
        for (int i = 0; i < file.Tracks.Length; ++i)
        {
            var t = file.Tracks[i];
            text += "[Track" + t.Index + "]" + System.Environment.NewLine;
            text += "MidiEventsCount=" + t.MidiEvents.Count + System.Environment.NewLine;
            text += "TextEventsCount=" + t.TextEvents.Count + System.Environment.NewLine;
            text += System.Environment.NewLine;
            for (int j = 0; j < t.MidiEvents.Count; ++j)
            {
                MidiEvent me = t.MidiEvents[j];
                text += "[MidiEvent" + j + "]" + System.Environment.NewLine;
                text += "Time=" + me.Time + System.Environment.NewLine;
                text += "Type=" + me.MidiEventType + System.Environment.NewLine;
                if (me.MidiEventType != MidiEventType.NoteOn && me.MidiEventType != MidiEventType.NoteOff) Debug.Log(j + " : " + me.MidiEventType);
                if (me.MidiEventType == MidiEventType.MetaEvent) text += "MetaEventType=" + me.MetaEventType + System.Environment.NewLine;
                text += "Channel=" + me.Channel + System.Environment.NewLine;
                text += "Note=" + me.Note + System.Environment.NewLine;
                text += "Velocity=" + me.Velocity + System.Environment.NewLine;
                if (me.MidiEventType == MidiEventType.ControlChange) text += "ControlChangeType=" + me.ControlChangeType + System.Environment.NewLine;
                text += System.Environment.NewLine;
            }
            for (int j = 0; j < t.TextEvents.Count; ++j)
            {
                TextEvent te = t.TextEvents[j];
                text += "[TextEvent" + j + "]" + System.Environment.NewLine;
                text += "Time=" + te.Time + System.Environment.NewLine;
                text += "Type=" + te.TextEventType + System.Environment.NewLine;
                text += "Value=" + te.Value + System.Environment.NewLine;
                text += System.Environment.NewLine;
            }
            text += System.Environment.NewLine;
            text += System.Environment.NewLine;
            text += System.Environment.NewLine;
        }
        File.WriteAllText("nnn.txt", text);
    }
    public static void MakeNotes(int min = int.MinValue, int max = int.MaxValue)
    {
        noteMax = int.MinValue;
        noteMin = int.MaxValue;
        if (max > 0 && min > 0)
        {
            noteMax = max;
            noteMin = min;
        }
        var file = new MidiFile("Midi/" + filename + ".mid");
        string text = "[General]" + System.Environment.NewLine
            + "Format=" + file.Format + System.Environment.NewLine
            + "TracksCount=" + file.TracksCount + System.Environment.NewLine
            + "TicksPerQuarterNote=" + file.TicksPerQuarterNote + System.Environment.NewLine
             + System.Environment.NewLine;
        tracks = new List<Note>[file.Tracks.Length];
        tempoChanges = new List<TempoChange>();
        for (int i = 0; i < tracks.Length; ++i) tracks[i] = new List<Note>();
        ParseTracks(file, min, max);
        File.WriteAllText("Dump/General.txt", text);
        Debug.Log("MaxNote: " + noteMax + " MinNote: " + noteMin);
    }

    static void ParseTracks(MidiFile file, int min, int max)
    {
        for (int i = 0; i < file.Tracks.Length; ++i)
        {
            var t = file.Tracks[i];
            string text = "[Track" + t.Index + "]" + System.Environment.NewLine;
            text += "MidiEventsCount=" + t.MidiEvents.Count + System.Environment.NewLine;
            text += "TextEventsCount=" + t.TextEvents.Count + System.Environment.NewLine;
            text += System.Environment.NewLine;
            Dictionary<int, Stack<MidiEvent>> map = new Dictionary<int, Stack<MidiEvent>>();
            int noteNum = 0;
            for (int j = 0; j < t.MidiEvents.Count; ++j)
            {
                MidiEvent me = t.MidiEvents[j];
                Stack<MidiEvent> stack;
                if ((me.MidiEventType == MidiEventType.NoteOn
                    || me.MidiEventType == MidiEventType.NoteOff
                    ) && (me.Note < min || me.Note > max))
                    continue;
                if (me.MidiEventType == MidiEventType.NoteOn)
                {
                    if (map.TryGetValue(me.Note, out stack))
                    {
                        stack.Push(me);
                        Debug.Log("Duplicate events");
                    }
                    else
                    {
                        stack = new Stack<MidiEvent>();
                        stack.Push(me);
                        map.Add(me.Note, stack);
                    }
                }
                else if (me.MidiEventType == MidiEventType.NoteOff)
                {
                    if (map.TryGetValue(me.Note, out stack))
                    {
                        Note n = new Note();
                        var on = stack.Pop();
                        if (stack.Count == 0) map.Remove(me.Note);
                        text += "[Note" + noteNum + "]" + System.Environment.NewLine; n.idx = noteNum;
                        n.track = i;
                        text += "Note=" + NoteToString(on.Note) + " (" + on.Note + ")" + System.Environment.NewLine; n.note = on.Note;
                        if (on.Note > noteMax) noteMax = on.Note;
                        if (on.Note < noteMin) noteMin = on.Note;
                        text += "StartTime=" + on.Time + System.Environment.NewLine; n.startTime = on.Time;
                        int length = me.Time - on.Time;
                        text += "Length=" + length + System.Environment.NewLine; n.length = length;
                        text += "InitialVelocity=" + on.Velocity + System.Environment.NewLine; n.InitialVelocity = on.Velocity;
                        text += "FinalVelocity=" + me.Velocity + System.Environment.NewLine + System.Environment.NewLine; n.FinalVelocity = me.Velocity;
                        tracks[i].Add(n);
                        ++noteNum;
                    }
                    else
                    {
                        Debug.Log("Error");
                    }
                }
                else
                {
                    text += "[MidiEvent" + j + "]" + System.Environment.NewLine;
                    text += "Time=" + me.Time + System.Environment.NewLine;
                    text += "Type=" + me.MidiEventType + System.Environment.NewLine;
                    if (me.MidiEventType == MidiEventType.MetaEvent)
                        text += "MetaEventType=" + me.MetaEventType + System.Environment.NewLine;
                    if (me.MetaEventType == MetaEventType.Tempo)
                    {
                        text += "Tempo=" + me.Arg1 + System.Environment.NewLine;
                        text += "BeatsMinute=" + me.Arg2 + System.Environment.NewLine;
                        bpm = me.Arg2;
                        float tickPerSecond = 60f / (bpm * file.TicksPerQuarterNote);
                        tempoChanges.Add(new TempoChange(tickPerSecond, me.Time));
                        print("Sets Ticks/Sec to " + tickPerSecond + " at time " + me.Time);
                    }
                    else
                    {
                        text += "Channel=" + me.Channel + System.Environment.NewLine;
                        text += "Note=" + me.Note + System.Environment.NewLine;
                        text += "Velocity=" + me.Velocity + System.Environment.NewLine;
                    }
                    if (me.MidiEventType == MidiEventType.ControlChange) text += "ControlChangeType=" + me.ControlChangeType + System.Environment.NewLine;
                    text += System.Environment.NewLine;
                }
            }
            for (int j = 0; j < t.TextEvents.Count; ++j)
            {
                TextEvent te = t.TextEvents[j];
                text += "[TextEvent" + j + "]" + System.Environment.NewLine;
                text += "Time=" + te.Time + System.Environment.NewLine;
                text += "Type=" + te.TextEventType + System.Environment.NewLine;
                text += "Value=" + te.Value + System.Environment.NewLine;
                text += System.Environment.NewLine;
            }
            text += System.Environment.NewLine;
            text += System.Environment.NewLine;
            text += System.Environment.NewLine;
            File.WriteAllText("Dump/Track" + i + ".txt", text);
        }
    }

    public static void PrepareTrack(int t)
    {
        tracks[t].Sort((a, b) => a.startTime.CompareTo(b.startTime));
        for (int h = 0; h < tracks[t].Count; ++h)
        {
            Note n = tracks[t][h];
            for (int i = 0; i < tempoChanges.Count; ++i)
            {
                if (n.startTime >= tempoChanges[i].startTime)
                {
                    n.tickPerSecond = tempoChanges[i].tickPerSecond;
                    tracks[t][h] = n;
                    break;
                }
            }
        }
    }

    static int curExit = 0;
    public static void TranslateCheng()
    {
        filename = "Cheng";
        MakeNotes();
        string text = "[General]\nExit=3\nDelay=3\n\n[TempoChanges]\n";
        tempoChanges.Sort((a, b) => a.startTime.CompareTo(b.startTime));
        foreach (var tc in tempoChanges)
            text += tc.startTime + "=" + tc.tickPerSecond + "\n";
        text += "\n\n";
        PrepareTrack(7);
        text += ";开头\n";
        text += InExitOrder(7, 17310, 19230, showLeft: true);
        text += CombineIntoLongPress(7, 20190, 22590, 6);
        text += InExitOrder(7, 23070, 27870);
        text += CombineIntoLongPress(7, 28830, 30750, 4);
        text += "\n;结尾\n";
        File.WriteAllText("Assets/Music/Resources/" + filename + "/00.txt", text);
    }

    public static string InExitOrder(int track, float startMidiTime, float endMidiTime, PanelType panel = PanelType.Left, bool showLeft = false)
    {
        string text = "";
        int i = GetIndex(track, startMidiTime);
        if (showLeft)
        {
            Note n = tracks[track][i];
            text += "[" + (n.startTime * n.tickPerSecond - 1.5f) + "]\nType=ShowLeftPanel\n\n";
        }
        for (; ; ++i)
        {
            Note n = tracks[track][i];
            text += "[" + n.startTime + "]\n";
            text += "TickPerSecond=" + n.tickPerSecond + "\n";
            text += "Type=FallingBlock\n";
            text += "Panel=" + panel + "\n";
            text += "Exit=" + curExit + "\n";
            ++curExit;
            if (curExit >= exitCount) curExit = 0;
            if (!Utils.noteToFile.ContainsKey(n.note)) Debug.Log("Note " + n.startTime + " has no sound!");
            text += "Note=" + n.note + "\n";
            if (n.startTime == endMidiTime) break;
        }
        return text + "\n";
    }

    public static string CombineIntoLongPress(int track, float startMidiTime, float endMidiTime, int length, PanelType panel = PanelType.Left)
    {
        string text = "";
        int st = GetIndex(track, startMidiTime);
        float tps = tracks[track][st].tickPerSecond;
        List<int> noteVals = new List<int>() { tracks[track][st].note };
        List<float> delays = new List<float>();
        for (int i = st + 1; ; ++i)
        {
            Note n = tracks[track][i];
            if (n.tickPerSecond != tps) Debug.LogError("TickPerSecond Mismatch!");
            noteVals.Add(n.note);
            if (!Utils.noteToFile.ContainsKey(n.note)) Debug.Log("Note " + n.startTime + " has no sound!");
            delays.Add(n.startTime - tracks[track][i - 1].startTime);
            if (n.startTime == endMidiTime) break;
        }

        text += "[" + startMidiTime + "]\n";
        text += "TickPerSecond=" + tps + "\n";
        text += "Type=LongFallingBlock\n";
        text += "Panel=" + panel + "\n";
        text += "Exit=" + curExit + "\n";
        ++curExit;
        if (curExit >= exitCount) curExit = 0;
        text += "Length=" + length + "\n";
        text += "Note=";
        for (int i = 0; i < noteVals.Count; ++i) text += noteVals[i] + (i == noteVals.Count - 1 ? "\n" : ",");
        text += "Delays=";
        for (int i = 0; i < delays.Count; ++i) text += "m" + delays[i] + (i == delays.Count - 1 ? "\n" : ",");
        return text;
    }

    static int GetIndex(int track, float midiTime)
    {
        for (int i = 0; i < tracks[track].Count; ++i)
        {
            if (tracks[track][i].startTime == midiTime) return i;
        }
        return -1;
    }

    private void Start()
    {
        TranslateCheng();
    }
}
