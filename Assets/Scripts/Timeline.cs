using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct KeyData
{
    public float startTime;
    public Dictionary<string, string> prop;
    public KeyData(float st)
    {
        startTime = st;
        prop = new Dictionary<string, string>();
    }
}
public class Timeline : MonoBehaviour
{
    FMOD.Studio.EventInstance vEventIns;

    static Timeline ins;
    List<KeyData> keyData;
    private void Awake()
    {
        ins = this;
    }
    public static void StartMusicScript(string scriptName)
    {
        Dictionary<string, Dictionary<string, string>> sections;
        Interpreter.Open("Assets/Music/Resources/" + scriptName + ".b3ks", out ins.keyData, out sections);
        GeneralSettings.exitCount = int.Parse(sections["General"]["Exit"]);
        string musicName;
        if (!sections["General"].TryGetValue("Music", out musicName)) musicName = scriptName;
        string str;
        if (sections["General"].TryGetValue("GameMode", out str)) GeneralSettings.mode = int.Parse(str); else GeneralSettings.mode = 0;
        foreach (var k in ins.keyData) ins.StartCoroutine(ins.StartFalling(k));
        if (musicName != "none")
        {
            ins.vEventIns = FMODUnity.RuntimeManager.CreateInstance("event:/" + musicName);
            ins.vEventIns.start();
        }
    }

    public static void Stop()
    {
        ins.StopAllCoroutines();
        ins.vEventIns.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }
    IEnumerator StartFalling(KeyData kd)
    {
        yield return new WaitForSeconds(kd.startTime);
        //print(kd.prop["Type"] + " is falling from Exit " + kd.prop["Exit"] + " in " + kd.prop["FallingTime"]);
        string str;
        int exit = 0;
        if (kd.prop.TryGetValue("Exit", out str)) exit = int.Parse(str);
        string blockType = kd.prop["Type"];
        var block = RhythmGameManager.CreateBlock(exit, blockType, Utils.GetRandomColor());
        if (kd.prop.TryGetValue("FallingTime", out str)) block.fallingTime = float.Parse(str); else block.fallingTime = 3;
        switch (blockType)
        {
            case "FallingBlock":
                break;
            case "LongFallingBlock":
                LongFallingBlock lfb = (LongFallingBlock)block;
                lfb.length = int.Parse(kd.prop["Length"]);
                break;
            case "HorizontalMove":
                HorizontalMove hrm = (HorizontalMove)block;
                hrm.width = int.Parse(kd.prop["Width"]);
                hrm.direction = kd.prop["Direction"] == "Left" ? Direction.Left : Direction.Right;
                break;
            case "Rouxian":
                Rouxian rx = (Rouxian)block;
                rx.width = int.Parse(kd.prop["Width"]);
                rx.timeLast = float.Parse(kd.prop["TimeLast"]);
                rx.FMODEvent = kd.prop["FMODEvent"];
                break;
            case "ChangeGameMode":
                ChangeGameMode cec = (ChangeGameMode)block;
                if (kd.prop.TryGetValue("NewExitCount", out str)) cec.newExitCount = int.Parse(str); else cec.newExitCount = -1;
                if (kd.prop.TryGetValue("NewGameMode", out str)) cec.newGameMode = int.Parse(str); else cec.newGameMode = -1;
                break;
            default:
                break;
        }
        // TODO: sync music
    }
}
