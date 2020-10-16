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
        foreach (var k in ins.keyData) ins.StartCoroutine(ins.StartFalling(k));
        if (musicName != "none")
        {
            ins.vEventIns = FMODUnity.RuntimeManager.CreateInstance("event:/" + scriptName);
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
        int exit = int.Parse(kd.prop["Exit"]);
        string blockType = kd.prop["Type"];
        var block = RhythmGameManager.CreateBlock(exit, blockType, Utils.GetRandomColor());
        block.fallingTime = float.Parse(kd.prop["FallingTime"]);
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
                break;
            default:
                break;
        }
        // TODO: sync music
    }
}
