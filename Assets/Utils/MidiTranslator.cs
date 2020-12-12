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
}

public abstract class BlockData
{
    public float startTime;
    public int exit;
    public int type;
    public abstract float TimeLast();
}

public class BlockData1 : BlockData
{
    public float length;
    public BlockData1(float startTime, int exit, float length)
    {
        this.startTime = startTime;
        this.exit = exit;
        this.length = length;
        type = 0;
    }
    public override float TimeLast() => length * 0.6f;
}
public class BlockData2 : BlockData
{
    public float width;
    public int dir = -1; // -1 for unassigned, 0 for left, 1 for right
    public BlockData2(float startTime, int exit)
    {
        this.startTime = startTime;
        this.exit = exit;
        width = 1;
        type = 1;
    }
    public override float TimeLast() => width * 0.6f;
}

public class MidiTranslator : MonoBehaviour
{
    static int bpm;
    static float tickPerSecond;
    static List<Note> notes;
    static MidiFile file = null;
    static string filename = "The Bass Part.mid";
    static int exitCount = 6;
    static int noteMax;
    static int noteMin;
    static string[] noteNames = new string[12]
    {
        "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
    };
    static string[] noteOctave = new string[11]
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
    public static void MakeNotes(int min = -1, int max = -1)
    {
        noteMax = int.MinValue;
        noteMin = int.MaxValue;
        if (max > 0 && min > 0)
        {
            noteMax = max;
            noteMin = min;
        }
        notes = new List<Note>();
        var file = new MidiFile("Midi/" + filename + ".mid");
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
                        notes.Add(n);
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
                        tickPerSecond = 60f / (bpm * file.TicksPerQuarterNote);
                        print("Set Ticks/Sec to " + tickPerSecond);
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
        }
        File.WriteAllText("nnn.txt", text);
        Debug.Log("MaxNote: " + noteMax + " MinNote: " + noteMin);
    }
    public static void TranslateBassPart()
    {
        filename = "The Bass Part";
        exitCount = 6;
        MakeNotes();
        string text = "[General]\nExit=" + exitCount + "\nDelay=2.7\n\n";
        int end = notes.Count;
        text += StandardConversion(0, 134, exitCount, false);
        text += HarpConversion(141, 195, exitCount, 2, 4);
        text += HarpConversion(210, 264, exitCount, 0, 4);
        text += StandardConversion(264, end, exitCount, true);
        File.WriteAllText("Assets/Music/Resources/" + filename + ".b3ks", text);
    }

    public static void TranslateJackBattle()
    {
        filename = "Jack Battle";
        exitCount = 4;
        MakeNotes(70, 79);
        string text = "[General]\nExit=" + exitCount + "\nDelay=2.7\n\n";
        int end = notes.Count;
        text += StandardConversion(0, 253, exitCount, false);
        text += StandardConversion(253, 1136, 6, false);
        text += StandardConversion(1136, end, 4, false);
        File.WriteAllText("Assets/Music/Resources/" + filename + ".b3ks", text);
    }

    public static void TranslateTrustYou()
    {
        filename = "Trust you";
        exitCount = 6;
        MakeNotes(int.MinValue, int.MaxValue);
        notes.RemoveAll(n => n.track != 6);
        string text = "[General]\nExit=" + exitCount + "\nDelay=3\n\n";
        text += StandardConversion(0, notes.Count, exitCount, true);
        /*
        float noteRange = noteMax - noteMin;
        for (int i = 0; i < notes.Count; ++i)
        {
            float startTimeInSec = notes[i].startTime * tickPerSecond;
            text += "[" + startTimeInSec + "]\n";
            text += "Exit=" + Mathf.RoundToInt((notes[i].note - noteMin) / noteRange * (exitCount - 1)) + "\n";
            if (notes[i].length <= 1000) text += "Type=FallingBlock\n";
            else text += "Type=LongFallingBlock\nLength=" + Mathf.Min(4, notes[i].length / 960) + "\n";
        }
        */
        File.WriteAllText("Assets/Music/Resources/" + filename + "/00.txt", text);
    }

    static string StandardConversion(int start, int end, int exitCount, bool gameOver)
    {
        float noteRange = noteMax - noteMin;
        List<Note>[] groups = new List<Note>[exitCount];
        for (int i = 0; i < exitCount; ++i) groups[i] = new List<Note>();
        for (int i = start; i < end; ++i)
        {
            Note n = notes[i];
            int exit = Mathf.RoundToInt((n.note - noteMin) / noteRange * (exitCount - 1));
            groups[exit].Add(n);
        }
        List<BlockData1>[] combine = new List<BlockData1>[exitCount];
        for (int i = 0; i < exitCount; ++i)
        {
            combine[i] = new List<BlockData1>();
            if (groups[i].Count == 0) continue;
            BlockData1 current = new BlockData1(groups[i][0].startTime * tickPerSecond, i, groups[i][0].length * tickPerSecond);
            for (int j = 1; j < groups[i].Count; ++j)
            {
                float startTime = groups[i][j].startTime * tickPerSecond;
                float length = groups[i][j].length * tickPerSecond;
                if (startTime - (current.startTime + current.length) <= 0.6f)
                {
                    current.length += length;
                }
                else
                {
                    combine[i].Add(current);
                    current = new BlockData1(startTime, i, length);
                }
            }
            combine[i].Add(current);
        }
        List<BlockData1> pass1 = new List<BlockData1>();
        for (int e = 0; e < exitCount; ++e) foreach (var b in combine[e]) pass1.Add(b);

        // pass 1 sorted by time
        pass1.Sort((x, y) => x.startTime.CompareTo(y.startTime));

        List<BlockData> pass2 = new List<BlockData>();
        BlockData2 bd2 = null;
        BlockData1 obd1 = null;
        foreach (var b in pass1)
        {
            int length = (int)Mathf.Max(b.length / 0.6f, 1f);
            if (length == 1)
            {
                if (bd2 == null)
                {
                    bd2 = new BlockData2(b.startTime, b.exit);
                    obd1 = b;
                }
                else
                {
                    if (b.startTime - bd2.startTime <= 1.2f * bd2.width)
                    {
                        if (bd2.dir == -1)
                        {
                            // 决定方向
                            if (b.exit == bd2.exit + bd2.width)
                            {
                                bd2.dir = 1;
                                bd2.width++;
                            }
                            else if (b.exit == bd2.exit - bd2.width)
                            {
                                bd2.dir = 0;
                                bd2.width++;
                            }
                            else
                            {
                                if (bd2.width == 1) pass2.Add(obd1); else pass2.Add(bd2);
                                bd2 = new BlockData2(b.startTime, b.exit);
                                obd1 = b;
                            }
                        }
                        else if (bd2.dir == 0)
                        {
                            if (b.exit == bd2.exit - bd2.width)
                            {
                                bd2.width++;
                            }
                            else
                            {
                                if (bd2.width == 1) pass2.Add(obd1); else pass2.Add(bd2);
                                bd2 = new BlockData2(b.startTime, b.exit);
                                obd1 = b;
                            }
                        }
                        else if (bd2.dir == -1)
                        {
                            if (b.exit == bd2.exit + bd2.width)
                            {
                                bd2.width++;
                            }
                            else
                            {
                                if (bd2.width == 1) pass2.Add(obd1); else pass2.Add(bd2);
                                bd2 = new BlockData2(b.startTime, b.exit);
                                obd1 = b;
                            }
                        }
                    }
                    else
                    {
                        if (bd2.width == 1) pass2.Add(obd1); else pass2.Add(bd2);
                        bd2 = new BlockData2(b.startTime, b.exit);
                        obd1 = b;
                    }
                }
            }
            else pass2.Add(b);
        }
        string text = "";
        pass2.Sort((x, y) => x.startTime.CompareTo(y.startTime));
        foreach (var b in pass2)
        {
            BlockData1 b1 = b as BlockData1;
            BlockData2 b2 = b as BlockData2;
            if (b1 != null)
            {
                int length = (int)Mathf.Max(b1.length / 0.6f, 1f);
                text += "[" + b1.startTime + "]\n";
                text += "Type=" + (length > 2 ? "LongFallingBlock" : "FallingBlock") + "\n";
                text += "Exit=" + b1.exit + "\n";
                if (length > 2) text += "Length=" + Mathf.Min(length, 4) + "\n";
            }
            if (b2 != null)
            {
                text += "[" + b2.startTime + "]\n";
                text += "Type=HorizontalMove\n";
                text += "Exit=" + b2.exit + "\n";
                text += "Width=" + b2.width + "\n";
                if (b2.dir == 0) text += "Direction=Left\n";
                else if (b2.dir == 1) text += "Direction=Right\n";
                else text += "Direction=Undefined\n";
            }
        }
        if (gameOver)
        {
            float gameOverTime = pass2[pass2.Count - 1].startTime + pass2[pass2.Count - 1].TimeLast() + 3;
            text += "\n";
            text += "[" + gameOverTime + "]\n";
            text += "Type=GameOver\n";
            text += "Exit=0\n";
        }
        return text;
    }

    static string HarpConversion(int start, int end, int exitCount, int harpExit, int harpWidth)
    {
        float harpStartTime = notes[start].startTime * tickPerSecond - 1.5f;
        float harpTimeLast = (notes[end - 1].startTime  + notes[end - 1].length) * tickPerSecond - harpStartTime;
        string text = "[" + harpStartTime + "]\n";
        text += "Type=Harp\n";
        text += "Exit=" + harpExit + "\n";
        text += "Width=" + harpWidth + "\n";
        text += "TimeLast=" + harpTimeLast + "\n";
        text += "FallingTime=1\n";

        float noteRange = noteMax - noteMin;
        List<Note>[] groups = new List<Note>[exitCount];
        for (int i = 0; i < exitCount; ++i) groups[i] = new List<Note>();
        for (int i = start; i < end; ++i)
        {
            Note n = notes[i];
            int exit = Mathf.RoundToInt((n.note - noteMin) / noteRange * (exitCount - 1));
            groups[exit].Add(n);
        }
        List<BlockData1>[] combine = new List<BlockData1>[exitCount];
        for (int i = 0; i < exitCount; ++i)
        {
            combine[i] = new List<BlockData1>();
            if (groups[i].Count == 0) continue;
            BlockData1 current = new BlockData1(groups[i][0].startTime * tickPerSecond, i, groups[i][0].length * tickPerSecond);
            for (int j = 1; j < groups[i].Count; ++j)
            {
                float startTime = groups[i][j].startTime * tickPerSecond;
                float length = groups[i][j].length * tickPerSecond;
                if (startTime - (current.startTime + current.length) <= 0.6f)
                {
                    current.length += length;
                }
                else
                {
                    combine[i].Add(current);
                    current = new BlockData1(startTime, i, length);
                }
            }
            combine[i].Add(current);
        }
        List<BlockData1> pass1 = new List<BlockData1>();
        for (int e = 0; e < exitCount; ++e) foreach (var b in combine[e]) pass1.Add(b);
        foreach (var b1 in pass1)
        {
            int length = (int)Mathf.Max(b1.length / 0.6f, 1f);
            text += "[" + b1.startTime + "]\n";
            text += "Type=" + (length > 1 ? "LongFallingBlock" : "FallingBlock") + "\n";
            text += "Exit=" + b1.exit + "\n";
            if (length > 1) text += "Length=" + length + "\n";
        }
        return text;
    }

    private void Start()
    {
        TranslateTrustYou();
    }
}
