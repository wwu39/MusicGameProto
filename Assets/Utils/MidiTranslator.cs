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
    public float startTimeInSec;
}

public struct TempoChange
{
    public int startTime;
    public int tempo;
    public float timeLast;
    public TempoChange(int t, int i)
    {
        tempo = t;
        startTime = i;
        timeLast = 0;
    }
}

public class MidiTranslator : MonoBehaviour
{
    static int bpm;
    static List<Note>[] tracks;
    static string filename = "The Bass Part.mid";
    static int exitCount = 3;
    static List<TempoChange> tempoChanges;
    static int noteMax;
    static int noteMin;
    static int ticksPerQuarterNote;
    static float TickPerSecond(int bpm) => 60f / (bpm * ticksPerQuarterNote);

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
        ticksPerQuarterNote = file.TicksPerQuarterNote;
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
                        text += "BeatsMinute=" + me.Arg2 + System.Environment.NewLine;
                        bpm = me.Arg2;
                        tempoChanges.Add(new TempoChange(bpm, me.Time));
                        print("Sets Ticks/Sec to " + TickPerSecond(bpm) + " at time " + me.Time);
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
        tracks[t].RemoveAll(n => !Utils.noteToFile.ContainsKey(n.note));
        for (int h = 0; h < tracks[t].Count; ++h)
        {
            Note n = tracks[t][h];
            int tempoIdx = 0;
            for (int i = tempoChanges.Count - 1; i >= 0; --i)
            {
                if (n.startTime >= tempoChanges[i].startTime)
                {
                    tempoIdx = i;
                    break;
                }
            }
            float timeSum = 0;
            for (int i = 0; i < tempoIdx; ++i)
                timeSum += tempoChanges[i].timeLast;
            n.startTimeInSec = (n.startTime - tempoChanges[tempoIdx].startTime) * TickPerSecond(tempoChanges[tempoIdx].tempo) + timeSum;
            tracks[t][h] = n;
        }
        curTrack = t;
    }

    static int curExit = 0;
    static int curTrack;
    static PanelType curPanel;
    public static void TranslateCheng()
    {
        filename = "Cheng";
        MakeNotes();
        exitCount = 3;
        string text = "[General]\nExit=3\nDelay=0\n\n[TempoChanges]\n";
        tempoChanges.Sort((a, b) => a.startTime.CompareTo(b.startTime));

        for (int i = 0; i < tempoChanges.Count; ++i)
        {
            var tc = tempoChanges[i];
            tc.timeLast = i == tempoChanges.Count - 1 ? -1 : (tempoChanges[i + 1].startTime - tempoChanges[i].startTime) * TickPerSecond(tempoChanges[i].tempo);
            tempoChanges[i] = tc;
            text += tc.startTime + "=" + tc.tempo + "," + tc.timeLast + "\n";
        }
        text += "\n\n";

        // curTrack=7
        curPanel = PanelType.Left;
        PrepareTrack(7);
        text += ";Track 7: Melody 开头\n";

        Note n = tracks[7][GetIndex(17310)];
        text += "[" + (n.startTimeInSec - 1.5f) + "]\nType=ShowLeftPanel\n\n";

        text += InExitOrder(17310, 19230);
        text += SingleExit(20190, 20670);
        text += CombineIntoLongPress(21150, 22590, 6);
        text += InExitOrder(23070, 27870);
        text += SingleExit(28830, 29309, 3);
        text += SingleExit(29789, 30269, 3);
        text += InExitOrder(30750, 34589);
        text += CombineIntoLongPress(35549, 37470, 8);
        text += InExitOrder(37949, 43230);
        text += CombineIntoLongPress(44190, 45869, 6);
        text += CombineIntoLongPress(46110, 47070, 6);
        text += InExitOrder(48030, 52829);
        text += CombineIntoVerticalMove(53789, 55229, 3);
        text += InExitOrder(55709, 63390);
        text += CombineIntoVerticalMove(65310, 65789, 2);
        text += CombineIntoLongPress(67230, 68669, 6);
        text += Single(69150);
        text += CombineIntoLongPress(71069, 72509, 6);
        text += Single(72990);
        text += SingleExit(74910, 76350, 2);
        text += InExitOrder(76830, 78271);

        text += "\n;告一段落\n\n";

        text += InExitOrder(124829, 126750);
        text += SingleExit(127710, 128190);
        text += InExitOrder(128670, 135389);
        text += CombineIntoLongPress(136349, 137789, 6);
        text += InExitOrder(138270, 142109);
        text += SingleExit(143069, 143549, 2);
        text += CombineIntoLongPress(144029, 145469, 6);
        text += CombineIntoLongPress(145949, 147389, 6);
        text += InExitOrder(147869, 150749);
        text += CombineIntoLongPress(151709, 153149, 6);
        text += CombineIntoLongPress(153629, 154589, 4);
        text += InExitOrder(155549, 160349);
        text += CombineIntoLongPress(161309, 162749, 6);
        text += InExitOrder(163229, 173309);
        text += CombineIntoLongPress(174749, 176189, 6);
        text += Single(176669);
        text += CombineIntoLongPress(178591, 180031, 6);
        text += Single(180509);
        text += CombineIntoLongPress(182429, 183869, 6);
        text += InExitOrder(184349, 194909);
        text += CombineIntoLongPress(195869, 197309, 6);
        text += InExitOrder(197790, 207869);
        text += CombineIntoLongPress(209310, 210749, 6);
        text += Single(210749);
        text += CombineIntoLongPress(213153, 214589, 6);
        text += Single(215071);
        text += CombineIntoVerticalMove(216990, 217469, 2);
        text += SingleExit(217957, 218905, 4);
        text += InExitOrder(219398, 228259);

        text += "\n;Track 7: Melody 结尾\n\n\n";
        n = tracks[7][GetIndex(228259)];
        text += "[" + (n.startTimeInSec + 6) + "]\nType=GameOver\n\n";
        File.WriteAllText("Assets/Music/Resources/Default/" + filename + "/00.txt", text);

        // curTrack=8
        text = "";
        curPanel = PanelType.Right;
        PrepareTrack(8);
        text += ";Track 8: Piano 开头\n";

        n = tracks[8][GetIndex(86444)];
        text += "[" + (n.startTimeInSec - 1.5f) + "]\nType=ShowRightPanel\n\n";
        text += InExitOrder(86444, 123890, 240);

        n = tracks[8][GetIndex(123890)];
        text += "[" + (n.startTimeInSec + 4f) + "]\nType=HideRightPanel\n\n";
        text += ";Track 8: Piano 结尾\n\n";
        File.WriteAllText("Assets/Music/Resources/Default/" + filename + "/Track 8.txt", text);
    }

    public static void TranslateCheng_Mobile()
    {
        filename = "Cheng";
        MakeNotes();
        exitCount = 4;
        string text = "[General]\nExit=4\nDelay=0\n\n[TempoChanges]\n";
        tempoChanges.Sort((a, b) => a.startTime.CompareTo(b.startTime));

        for (int i = 0; i < tempoChanges.Count; ++i)
        {
            var tc = tempoChanges[i];
            tc.timeLast = i == tempoChanges.Count - 1 ? -1 : (tempoChanges[i + 1].startTime - tempoChanges[i].startTime) * TickPerSecond(tempoChanges[i].tempo);
            tempoChanges[i] = tc;
            text += tc.startTime + "=" + tc.tempo + "," + tc.timeLast + "\n";
        }
        text += "\n\n";

        // curTrack=7
        curPanel = PanelType.Left;
        PrepareTrack(7);
        text += ";Track 7: Melody 开头\n";

        Note n = tracks[7][GetIndex(17310)];
        text += "[" + (n.startTimeInSec - 1.5f) + "]\nType=ShowLeftPanel\n\n";

        text += InExitOrder(17310, 19230);
        text += SingleExit(20190, 20670);
        text += CombineIntoLongPress(21150, 22590, 6);
        text += InExitOrder(23070, 27870);
        text += SingleExit(28830, 29309, 3);
        text += SingleExit(29789, 30269, 3);
        text += InExitOrder(30750, 34589);
        text += CombineIntoLongPress(35549, 37470, 8);
        text += InExitOrder(37949, 43230);
        text += CombineIntoLongPress(44190, 45869, 6);
        text += CombineIntoLongPress(46110, 47070, 6);
        text += InExitOrder(48030, 52829);
        text += SingleExit(53789, 55229, 3);
        text += InExitOrder(55709, 63390);
        text += SingleExit(65310, 65789, 2);
        text += CombineIntoLongPress(67230, 68669, 6);
        text += Single(69150);
        text += CombineIntoLongPress(71069, 72509, 6);
        text += Single(72990);
        text += SingleExit(74910, 76350, 2);
        text += InExitOrder(76830, 78271);

        text += "\n;告一段落\n\n";

        text += InExitOrder(124829, 126750);
        text += SingleExit(127710, 128190);
        text += InExitOrder(128670, 135389);
        text += CombineIntoLongPress(136349, 137789, 6);
        text += InExitOrder(138270, 142109);
        text += SingleExit(143069, 143549, 2);
        text += CombineIntoLongPress(144029, 145469, 6);
        text += CombineIntoLongPress(145949, 147389, 6);
        text += InExitOrder(147869, 150749);
        text += CombineIntoLongPress(151709, 153149, 6);
        text += CombineIntoLongPress(153629, 154589, 4);
        text += InExitOrder(155549, 160349);
        text += CombineIntoLongPress(161309, 162749, 6);
        text += InExitOrder(163229, 173309);
        text += CombineIntoLongPress(174749, 176189, 6);
        text += Single(176669);
        text += CombineIntoLongPress(178591, 180031, 6);
        text += Single(180509);
        text += CombineIntoLongPress(182429, 183869, 6);
        text += InExitOrder(184349, 194909);
        text += CombineIntoLongPress(195869, 197309, 6);
        text += InExitOrder(197790, 207869);
        text += CombineIntoLongPress(209310, 210749, 6);
        text += Single(210749);
        text += CombineIntoLongPress(213153, 214589, 6);
        text += Single(215071);
        text += SingleExit(216990, 217469, 2);
        text += SingleExit(217957, 218905, 4);
        text += InExitOrder(219398, 228259);

        text += "\n;Track 7: Melody 结尾\n\n\n";
        n = tracks[7][GetIndex(228259)];
        text += "[" + (n.startTimeInSec + 6) + "]\nType=GameOver\n\n";
        File.WriteAllText("Assets/Music/Resources/Mobile/" + filename + "/00.txt", text);

        // curTrack=8
        text = "";
        curPanel = PanelType.Right;
        PrepareTrack(8);
        text += ";Track 8: Piano 开头\n";

        n = tracks[8][GetIndex(86444)];
        text += "[" + (n.startTimeInSec - 1.5f) + "]\nType=ShowRightPanel\n\n";
        text += InExitOrder(86444, 123890, 240);

        n = tracks[8][GetIndex(123890)];
        text += "[" + (n.startTimeInSec + 4f) + "]\nType=HideRightPanel\n\n";
        text += ";Track 8: Piano 结尾\n\n";
        File.WriteAllText("Assets/Music/Resources/Mobile/" + filename + "/Track 8.txt", text);
    }

    public static string InExitOrder(float startMidiTime, float endMidiTime)
    {
        string text = "";
        int i = GetIndex(startMidiTime);
        for (; ; ++i)
        {
            Note n = tracks[curTrack][i];
            text += "[" + n.startTimeInSec + "]\n";
            text += "StartTick=" + n.startTime + "\n";
            text += "Type=FallingBlock\n";
            text += "Panel=" + curPanel + "\n";
            text += "Exit=" + curExit + "\n";
            ++curExit;
            if (curExit >= exitCount) curExit = 0;
            if (!Utils.noteToFile.ContainsKey(n.note)) Debug.Log("Note " + n.startTimeInSec + " has no sound!");
            text += "Note=" + n.note + "\n";
            if (n.startTime == endMidiTime) break;
        }
        return text + "\n";
    }

    public static string SingleExit(float startMidiTime, float endMidiTime, int width = 0)
    {
        string text = "";
        int i = GetIndex(startMidiTime);
        for (; ; ++i)
        {
            Note n = tracks[curTrack][i];
            text += "[" + n.startTimeInSec + "]\n";
            text += "Type=FallingBlock\n";
            text += "Panel=" + curPanel + "\n";
            text += "Exit=" + curExit + "\n";
            if (!Utils.noteToFile.ContainsKey(n.note)) Debug.Log("Note " + n.startTime + " has no sound!");
            text += "Note=" + n.note + "\n";
            if (n.startTime == endMidiTime) break;
        }
        return text + "\n";
    }

    public static string InExitOrder(float startMidiTime, float endMidiTime, float combineTimeInTick, bool combineIntoVertical = false)
    {
        string text = "";
        int st = GetIndex(startMidiTime);
        List<List<Note>> tmp = new List<List<Note>>();

        for (; ; ++st)
        {
            Note n = tracks[curTrack][st];
            tmp.Add(new List<Note>() { n });
            if (n.startTime >= endMidiTime) break;
        }
        for (int a = 0; a < tmp.Count; ++a)
        {
            if (tmp[a].Count > 0)
            {
                for (int b = a + 1; b < tmp.Count; ++b)
                {
                    if (tmp[b].Count > 0 && tmp[b][0].startTime - tmp[a][0].startTime <= combineTimeInTick)
                    {
                        tmp[a].Add(tmp[b][0]);
                        tmp[b].Clear();
                    }
                    else break;
                }
            }
        }
        foreach(var v in tmp)
        {
            if (v.Count == 1)
            {
                Note n = v[0];
                text += "[" + n.startTimeInSec + "]\n";
                text += "Type=FallingBlock\n";
                text += "Panel=" + curPanel + "\n";
                text += "Exit=" + curExit + "\n";
                ++curExit;
                if (curExit >= exitCount) curExit = 0;
                if (!Utils.noteToFile.ContainsKey(n.note)) Debug.Log("Note " + n.startTime + " has no sound!");
                text += "Note=" + n.note + "\n";
            }
            else if (v.Count > 1)
            {
                v.Sort((a, b) => a.startTimeInSec.CompareTo(b.startTimeInSec));
                Note n = v[0];
                List<float> delays = new List<float>();
                for (int i = 1; i < v.Count; ++i)
                {
                    delays.Add(v[i].startTimeInSec - v[i - 1].startTimeInSec);
                }
                if (combineIntoVertical)
                {
                    int width = Mathf.Min(3, v.Count);
                    text += "[" + n.startTimeInSec + "]\n";
                    text += "Type=HorizontalMove\n";
                    text += "Panel=" + curPanel + "\n";
                    text += "Exit=" + (width == 3 ? 0 : curExit) + "\n";
                    text += "Direction=";
                    if (width == 2) text += curExit < 2 ? "Down\n" : "Up\n";
                    if (width == 3) text += "Down\n";
                    else
                    {
                        ++curExit;
                        if (curExit >= exitCount) curExit = 0;
                    }
                    text += "Width=" + width + "\n";
                    text += "Note=";
                    for (int i = 0; i < v.Count; ++i) text += v[i].note + (i == v.Count - 1 ? "\n" : ",");
                    text += "Delays=";
                    for (int i = 0; i < delays.Count; ++i) text += delays[i] + (i == delays.Count - 1 ? "\n" : ",");
                }
                else
                {
                    text += "[" + n.startTimeInSec + "]\n";
                    text += "Type=FallingBlock\n";
                    text += "Panel=" + curPanel + "\n";
                    text += "Exit=" + curExit + "\n";
                    ++curExit;
                    if (curExit >= exitCount) curExit = 0;
                    text += "Note=";
                    for (int i = 0; i < v.Count; ++i) text += v[i].note + (i == v.Count - 1 ? "\n" : ",");
                    text += "Delays=";
                    for (int i = 0; i < delays.Count; ++i) text += delays[i] + (i == delays.Count - 1 ? "\n" : ",");
                }
            }
        }
        return text;
    }
    public static string Single(float startMidiTime)
    {
        string text = "";
        int i = GetIndex(startMidiTime);
        Note n = tracks[curTrack][i];
        text += "[" + n.startTimeInSec + "]\n";
        text += "Type=FallingBlock\n";
        text += "Panel=" + curPanel + "\n";
        text += "Exit=" + curExit + "\n";
        ++curExit;
        if (curExit >= exitCount) curExit = 0;
        text += "Note=" + n.note + "\n";
        return text;
    }

    public static string CombineIntoSingle(float startMidiTime, float endMidiTime)
    {
        string text = "";
        int st = GetIndex(startMidiTime);
        List<int> noteVals = new List<int>() { tracks[curTrack][st].note };
        List<float> delays = new List<float>();
        for (int i = st + 1; ; ++i)
        {
            Note n = tracks[curTrack][i];
            noteVals.Add(n.note);
            if (!Utils.noteToFile.ContainsKey(n.note)) Debug.Log("Note " + n.startTimeInSec + " has no sound!");
            delays.Add(n.startTimeInSec - tracks[curTrack][i - 1].startTimeInSec);
            if (n.startTime == endMidiTime) break;
        }

        text += "[" + tracks[curTrack][st].startTimeInSec + "]\n";
        text += "Type=FallingBlock\n";
        text += "Panel=" + curPanel + "\n";
        text += "Exit=" + curExit + "\n";
        ++curExit;
        if (curExit >= exitCount) curExit = 0;
        text += "Note=";
        for (int i = 0; i < noteVals.Count; ++i) text += noteVals[i] + (i == noteVals.Count - 1 ? "\n" : ",");
        text += "Delays=";
        for (int i = 0; i < delays.Count; ++i) text += delays[i] + (i == delays.Count - 1 ? "\n" : ",");
        return text;
    }

    public static string CombineIntoLongPress(float startMidiTime, float endMidiTime, int length)
    {
        string text = "";
        int st = GetIndex(startMidiTime);
        List<int> noteVals = new List<int>() { tracks[curTrack][st].note };
        List<float> delays = new List<float>();
        for (int i = st + 1; ; ++i)
        {
            Note n = tracks[curTrack][i];
            noteVals.Add(n.note);
            if (!Utils.noteToFile.ContainsKey(n.note)) Debug.Log("Note " + n.startTimeInSec + " has no sound!");
            delays.Add(n.startTimeInSec - tracks[curTrack][i - 1].startTimeInSec);
            if (n.startTime == endMidiTime) break;
        }

        text += "[" + tracks[curTrack][st].startTimeInSec + "]\n";
        text += "Type=LongFallingBlock\n";
        text += "Panel=" + curPanel + "\n";
        text += "Exit=" + curExit + "\n";
        ++curExit;
        if (curExit >= exitCount) curExit = 0;
        text += "Length=" + length + "\n";
        text += "Note=";
        for (int i = 0; i < noteVals.Count; ++i) text += noteVals[i] + (i == noteVals.Count - 1 ? "\n" : ",");
        text += "Delays=";
        for (int i = 0; i < delays.Count; ++i) text += delays[i] + (i == delays.Count - 1 ? "\n" : ",");
        return text;
    }

    public static string CombineIntoVerticalMove(float startMidiTime, float endMidiTime, int width)
    {
        string text = "";
        int st = GetIndex(startMidiTime);
        List<int> noteVals = new List<int>() { tracks[curTrack][st].note };
        List<float> delays = new List<float>();
        for (int i = st + 1; ; ++i)
        {
            Note n = tracks[curTrack][i];
            noteVals.Add(n.note);
            if (!Utils.noteToFile.ContainsKey(n.note)) Debug.Log("Note " + n.startTimeInSec + " has no sound!");
            delays.Add(n.startTimeInSec - tracks[curTrack][i - 1].startTimeInSec);
            if (n.startTime == endMidiTime) break;
        }
        text += "[" + tracks[curTrack][st].startTimeInSec + "]\n";
        text += "Type=HorizontalMove\n";
        text += "Panel=" + curPanel + "\n";
        text += "Exit=" + (width == 3 ? 0 : curExit) + "\n";
        text += "Direction=";
        if (width == 2) text += curExit < 2 ? "Down\n" : "Up\n";
        if (width == 3) text += "Down\n";
        else
        {
            ++curExit;
            if (curExit >= exitCount) curExit = 0;
        }
        text += "Width=" + width + "\n";
        text += "Note=";
        for (int i = 0; i < noteVals.Count; ++i) text += noteVals[i] + (i == noteVals.Count - 1 ? "\n" : ",");
        text += "Delays=";
        for (int i = 0; i < delays.Count; ++i) text += delays[i] + (i == delays.Count - 1 ? "\n" : ",");
        return text;
    }

    static int GetIndex(float midiTime)
    {
        for (int i = 0; i < tracks[curTrack].Count; ++i)
        {
            if (tracks[curTrack][i].startTime >= midiTime) return i;
        }
        return -1;
    }

    private void Start()
    {
        TranslateCheng_Mobile();
    }
}
