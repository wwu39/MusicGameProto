using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiParser;
using System.IO;
using System.Runtime.InteropServices;

public class MidiTranslator
{
    static MidiFile file = null;
    static int exitCount = 6;
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
        var file = new MidiFile("Assets/Music/Resources/The Bass Part.mid");
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
    public static void Dump2()
    {
        var file = new MidiFile("Assets/Music/Resources/The Bass Part.mid");
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
                if (me.MidiEventType== MidiEventType.NoteOn)
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
                        var on = stack.Pop();
                        if (stack.Count == 0) map.Remove(me.Note);
                        text += "[Note" + noteNum + "]" + System.Environment.NewLine;
                        text += "Note=" + NoteToString(on.Note) + System.Environment.NewLine;
                        text += "StartTime=" + on.Time + System.Environment.NewLine;
                        int length = me.Time - on.Time;
                        text += "Length=" + length + System.Environment.NewLine;
                        text += "InitialVelocity=" + on.Velocity + System.Environment.NewLine;
                        text += "FinalVelocity=" + me.Velocity + System.Environment.NewLine + System.Environment.NewLine;
                        ++noteNum;
                    }
                    else
                    {
                        Debug.Log("Error");
                    }
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

    }
    public static void Translate()
    {
        string text = "[General]\nExit=" + exitCount + "\nDelay=3\n\n";
        string input = "Assets/Music/Resources/The Bass Part";
        file = new MidiFile(input + ".mid");
        int end = file.Tracks[1].MidiEvents.Count;
        text += Convertion1(0, 93, new int[] { 0, 1, 4, 5 });
        text += Convertion2(93, 146, 4);
        text += Convertion1(146, 158, new int[] { 1, 2, 3, 4 });
        text += Convertion2(158, 202, 3);
        text += Convertion3(212, 384);
        text += Convertion2(384, 403, 2);
        text += Convertion1(403, 516, new int[] { 0, 1, 4, 5 });
        text += Convertion2(516, 577, 4);
        text += Convertion1(577, 599, new int[] { 0, 1, 4, 5 });
        text += Convertion2(599, 668, 4);
        text += Convertion1(668, end, new int[] { 0, 1, 2, 3, 4, 5 });
        File.WriteAllText(input+".b3ks", text);
    }

    static string Convertion1(int start, int end, int[] exitGroup) // 导出所有NoteOn和速度不为0
    {
        int exit = 0;
        string text = "";
        for (int j = start; j < end; ++j)
        {
            MidiEvent me = file.Tracks[1].MidiEvents[j];
            if (me.MidiEventType == MidiEventType.NoteOn && me.Velocity != 0)
            {
                text += "[" + (float)me.Time / 1000 + "]\nType=FallingBlock\n";
                text += "Index=" + j + "\n";
                text += "Exit=" + exitGroup[exit] + "\n\n";
                exit = (exit + 1) % exitGroup.Length;
            }
        }
        return text;
    }
    static string Convertion3(int start, int end) // 并轨模式
    {
        int exit = 0;
        string text = "";
        MidiEvent me = file.Tracks[1].MidiEvents[start];
        text += "[" + (float)me.Time / 1000 + "]\nType=ChangeGameMode\n";
        text += "Index=" + start + "\n";
        text += "Exit=0\nNewGameMode=1\n\n";

        for (int j = start + 1; j < end - 1; ++j)
        {
            me = file.Tracks[1].MidiEvents[j];
            if (me.MidiEventType == MidiEventType.NoteOn && me.Velocity != 0)
            {
                text += "[" + (float)me.Time / 1000 + "]\nType=FallingBlock\n";
                text += "Index=" + j + "\n";
                text += "Exit=" + exit + "\n\n";
                exit = (exit + 1) % 6;
            }
        }

        me = file.Tracks[1].MidiEvents[end];
        text += "[" + (float)me.Time / 1000 + "]\nType=ChangeGameMode\n";
        text += "Index=" + end + "\n";
        text += "Exit=0\nNewGameMode=0\n\n";
        return text;
    }
    static string Convertion2(int start, int end, int max)
    {
        string text = "";
        int exit = 0;
        List<List<int>> groups = new List<List<int>>();
        List<int> firstGroup = new List<int>();
        int lastTime = file.Tracks[1].MidiEvents[start].Time;
        firstGroup.Add(lastTime);
        groups.Add(firstGroup);
        for (int i = start + 1; i < end; ++i)
        {
            MidiEvent me = file.Tracks[1].MidiEvents[i];
            if (me.MidiEventType == MidiEventType.NoteOn && me.Velocity != 0)
            {
                if (me.Time - lastTime <= 1000 && groups[groups.Count-1].Count < max)
                {
                    groups[groups.Count - 1].Add(me.Time);
                }
                else
                {
                    List<int> newGroup = new List<int>();
                    newGroup.Add(me.Time);
                    groups.Add(newGroup);
                }
                lastTime = me.Time;
            }
        }
        foreach (var a in groups)
        {
            text += "[" + (float)a[0] / 1000 + "]\nType=LongFallingBlock\n";
            text += "Length=" + a.Count + "\n";
            text += "Fusion=";
            foreach (int b in a) text += (float)b / 1000 + " ";
            text += "\nExit=" + exit + "\n\n";
            exit = (exit + 1) % exitCount;
        }
        return text;
    }
    static string ConversionEmpty(int start, int end)
    {
        return "";
    }
}
