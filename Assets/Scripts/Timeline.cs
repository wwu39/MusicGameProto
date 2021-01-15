using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

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
    [SerializeField] GameObject pauseGameUI;
    [HideInInspector] public FMOD.Studio.EventInstance vEventIns;
    [HideInInspector] public bool hasMusic;
    [SerializeField] VideoPlayer vp;
    [SerializeField] VideoClip[] clips;
    [SerializeField] RenderTexture[] videoRatios;
    [SerializeField] RawImage videoDisplay;

    public static Timeline ins;
    List<KeyData> keyData;
    public delegate void Void_RhythmObject(RhythmObject o);
    public event Void_RhythmObject OnBlockCreated;
    public float startTime;

    // 难度屏蔽
    // 0=困难难度下无屏蔽
    // 1=中等难度下方块屏蔽自身处理时间50%的其他方块
    // 2=简单难度下方块屏蔽自身处理时间100%的其他方块
    // 只有FallingBlock, LongFallingBlock, HorizontalMove受难度屏蔽影响
    bool difficultyShielding = false;
    static HashSet<string> difficultyShieldingTypes = new HashSet<string>() { "FallingBlock", "LongFallingBlock", "HorizontalMove" };
    private void OnValidate()
    {
        vp = GetComponent<VideoPlayer>();
    }
    private void Awake()
    {
        ins = this;
    }
    public static void StartMusicScript(string scriptName)
    {
        GeneralSettings.Reset();
        Dictionary<string, Dictionary<string, string>> sections;
        Interpreter.Open(scriptName, out ins.keyData, out sections);
        GeneralSettings.exitCount = int.Parse(sections["General"]["Exit"]);
        string musicName;
        if (!sections["General"].TryGetValue("Music", out musicName)) musicName = scriptName;
        string str;
        if (sections["General"].TryGetValue("GameMode", out str)) GeneralSettings.mode = int.Parse(str); else GeneralSettings.mode = 0;
        if (sections["General"].TryGetValue("Delay", out str)) GeneralSettings.delay = float.Parse(str); else GeneralSettings.delay = 3;
        if (sections["General"].TryGetValue("Difficulty", out str)) GeneralSettings.difficulty = int.Parse(str); else GeneralSettings.difficulty = 0;
        if (sections["General"].TryGetValue("MusicStartPosition", out str)) GeneralSettings.musicStartTime = float.Parse(str); else GeneralSettings.musicStartTime = 0;
        foreach (var k in ins.keyData) ins.StartCoroutine(ins.StartFalling(k));
        // ins.StartCoroutine(ins.GameOver(timeEnd + 10));
        if (musicName != "none")
        {
            ins.hasMusic = true;
            ins.vEventIns = FMODUnity.RuntimeManager.CreateInstance("event:/" + musicName);
            ins.StartCoroutine(ins.StartMusic(GeneralSettings.delay));
        }
        Scoring.Reset();
    }

    public static void Stop()
    {
        ins.StopAllCoroutines();
        ins.vEventIns.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }

    public static void SyncMusic(float time)
    {
        Debug.Log("Sync " + time);
        FMOD.Studio.PLAYBACK_STATE state;
        ins.vEventIns.getPlaybackState(out state);
        if (state == FMOD.Studio.PLAYBACK_STATE.PLAYING)
            ins.vEventIns.setTimelinePosition((int)(time * 1000));
    }

    bool resuming;
    int tlpos;
    public static void Pause(bool b)
    {
        if (ins.resuming) return;
        if (b)
        {
            ins.pauseGameUI.SetActive(true);
            Time.timeScale = 0;
            ins.vEventIns.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            ins.vEventIns.getTimelinePosition(out ins.tlpos);
            ins.vp.Pause();
        }
        else
        {
            ins.StartCoroutine(ins.ResumeInTime(3));
        }
    }

    IEnumerator ResumeInTime(int sec)
    {
        resuming = true;
        pauseGameUI.SetActive(false);
        RhythmGameManager.ins.invisibleBlocker.SetActive(true);
        if (sec < 1) sec = 1;
        for (int i = 0; i < sec; ++i)
        {
            var text = Instantiate(Resources.Load<GameObject>("CountDown"), GameObject.Find("Canvas").transform).GetComponent<Text>();
            text.text = (sec - i).ToString();
            if (i == sec - 1)
            {
                vEventIns.start();
                vEventIns.setTimelinePosition(tlpos - 1000);
                vEventIns.setVolume(0);
            }
            yield return new WaitForSecondsRealtime(1);
        }
        Time.timeScale = 1;
        vEventIns.setVolume(1);
        RhythmGameManager.ins.invisibleBlocker.SetActive(false);
        vp.Play();
        resuming = false;
    }

    IEnumerator StartFalling(KeyData kd)
    {
        string str; string[] seg;
        float tickPerSecond = 0;
        if (kd.prop.TryGetValue("TickPerSecond", out str))
        {
            tickPerSecond = float.Parse(str);
            kd.startTime *= tickPerSecond;
        }
        yield return new WaitForSeconds(kd.startTime - GeneralSettings.musicStartTime);
        //print(kd.prop["Type"] + " is falling from Exit " + kd.prop["Exit"] + " in " + kd.prop["FallingTime"]);
        int exit = 0;
        PanelType panel = PanelType.Left;
        if (kd.prop.TryGetValue("Exit", out str)) exit = int.Parse(str);
        if (kd.prop.TryGetValue("Panel", out str)) if (str == "Right") panel = PanelType.Right;
        float fallingTime;
        if (kd.prop.TryGetValue("FallingTime", out str)) fallingTime = float.Parse(str); else fallingTime = 3;

        string blockType = "None";
        if (kd.prop.TryGetValue("Type", out str)) blockType = str;
        if (GeneralSettings.specialMode == 1) blockType = "FallingBlock";

        // General Properties
        // if game layout needs to change
        bool generateNewExit = false;
        if (kd.prop.TryGetValue("NewExitCount", out str))
        {
            int newExitCount = int.Parse(str);
            if (newExitCount != GeneralSettings.exitCount)
            {
                GeneralSettings.exitCount = newExitCount;
                print("Change number of Exits to " + GeneralSettings.exitCount);
                generateNewExit = true;
            }
        }
        if (kd.prop.TryGetValue("NewGameMode", out str))
        {
            int newMode = int.Parse(str);
            if (newMode != GeneralSettings.mode)
            {
                GeneralSettings.mode = newMode;
                print("New game mode: " + GeneralSettings.mode);
                generateNewExit = true;
            }
        }
        if (generateNewExit) RhythmGameManager.GenerateExits();

        if (kd.prop.TryGetValue("HideBottom", out str))
        {
            if (str == "true") RhythmGameManager.HideBottomBar();
            else if (str == "false") RhythmGameManager.HideBottomBar(false);
        }
        if (kd.prop.TryGetValue("HideExits", out str))
        {
            if (str == "true") RhythmGameManager.HideExit();
            else if (str == "false") RhythmGameManager.HideExit(false);
            else
            {
                seg = str.Split(',');
                HashSet<int> set = new HashSet<int>();
                foreach (var s in seg) set.Add(int.Parse(s));
                RhythmGameManager.HideExit(set);
            }
        }
        if (kd.prop.TryGetValue("SpecialMode", out str))
        {
            GeneralSettings.specialMode = int.Parse(str);
        }
        if (kd.prop.TryGetValue("Enable3D", out str))
        {
            if (str == "yes") Road.ins.EnableDisplay(true);
            else if (str == "no") Road.ins.EnableDisplay(false);
        }
        if (kd.prop.TryGetValue("SetParam", out str))
        {
            seg = str.Split(',');
            vEventIns.setParameterByName(seg[0], float.Parse(seg[1]));
        }
        if (kd.prop.TryGetValue("SFX", out str)) FMODUnity.RuntimeManager.PlayOneShot("event:/" + str);
        if (kd.prop.TryGetValue("Image", out str)) Instantiate(Resources.Load<GameObject>(str), RhythmGameManager.ins.imageNode);
        if (kd.prop.TryGetValue("VideoVolumeFadeOut", out str)) ins.StartCoroutine(FadeVideoVolume(float.Parse(str)));
        if (kd.prop.TryGetValue("MusicFadeIn", out str)) ins.StartCoroutine(MusicFadeIn(float.Parse(str)));
        if (kd.prop.TryGetValue("MusicFadeOut", out str)) ins.StartCoroutine(MusicFadeOut(float.Parse(str)));
        if (kd.prop.TryGetValue("PlayVideoClip", out str))
        {
            int clipNum = int.Parse(str);
            int mode = kd.prop["Ratio"] == "16:9" ? 1 : 0;
            PlayVideo(clipNum, mode);
        }

        if (blockType == "GameOver")
        {
            RhythmGameManager.HideContent();
            Instantiate(Resources.Load<GameObject>("结算画面"), GameObject.Find("Canvas").transform);
        }
        else if (blockType == "ShowLeftPanel") Panel.ShowLeft();
        else if (blockType == "HideLeftPanel") Panel.HideLeft();
        else if (blockType == "ShowRightPanel") Panel.ShowRight();
        else if (blockType == "HideRightPanel") Panel.HideRight();
        else if (blockType == "HideBothPanels") Panel.HideBoth();

        // Generates block
        else if (blockType != "None" && (GeneralSettings.specialMode != 2 || IgnoresSpecialMode2(blockType)) && !ShieldedByDifficulty(blockType))
        {
            if (Harp.Contains(kd))
            {
                if (!Harp.ins.rouxian)
                {
                    Beat beat = RhythmGameManager.CreateBeat(exit, panel, Utils.GetRandomColor());
                    // float waitTime = Mathf.Max(0, fallingTime - beat.lifetime);
                    OnBlockCreated?.Invoke(beat);
                    // if (waitTime > 0) yield return new WaitForSeconds(waitTime);
                }
                else if (blockType == "Lyrics")
                {
                    Harp.CreateLyrics(kd.prop["Lyrics"], float.Parse(kd.prop["TimeLast"]));
                }
            }
            else
            {
                Color c = Utils.GetRandomColor();
                if (kd.prop.TryGetValue("Color", out str))
                {
                    seg = str.Split(',');
                    c = new Color32(byte.Parse(seg[0]), byte.Parse(seg[1]), byte.Parse(seg[2]), 255);
                }
                var block = RhythmGameManager.CreateBlock(exit, panel, blockType, c);
                block.fallingTime = fallingTime;

                if (kd.prop.TryGetValue("Note", out str))
                {
                    seg = str.Split(',');
                    block.sound = new SoundStruct[seg.Length];
                    for (int i = 0; i < seg.Length; ++i)
                    {
                        if (Utils.noteToFile.TryGetValue(int.Parse(seg[i]), out str))
                        {
                            block.sound[i].id = "MidiNotes/" + str;
                        }
                    }
                }
                if (kd.prop.TryGetValue("Delays", out str))
                {
                    seg = str.Split(',');
                    if (seg.Length > 1 && seg.Length == block.sound.Length - 1)
                    {
                        float val = 0;
                        for (int i = 0; i < seg.Length; ++i)
                        {
                            float cur = seg[i][0] == 'm' ? float.Parse(seg[i].Substring(1)) * tickPerSecond : float.Parse(seg[i]);
                            val += cur;
                            block.sound[i + 1].delay = val;
                        }
                    }
                    else
                    {
                        float val = seg[0][0] == 'm' ? float.Parse(seg[0].Substring(1)) * tickPerSecond : float.Parse(seg[0]);
                        if (val > 1000) val *= tickPerSecond;
                        for (int i = 1; i < block.sound.Length; ++i) block.sound[i].delay = val * i;
                    }
                }

                float shieldingTime = -999;
                switch (blockType)
                {
                    case "FallingBlock":
                        // 难度屏蔽
                        if (GeneralSettings.difficulty == 1) shieldingTime = 0.3f;
                        else if (GeneralSettings.difficulty == 2) shieldingTime = 0.6f;
                        break;
                    case "Beat":
                        Beat beat = (Beat)block;
                        if (kd.prop.TryGetValue("Position", out str))
                        {
                            seg = str.Split(',');
                            beat.rt.anchoredPosition = new Vector2(float.Parse(seg[0]) * DefRes.x, float.Parse(seg[1]) * DefRes.y);
                        }
                        else
                        {
                            var pos = beat.rt.anchoredPosition;
                            pos.y = 0;
                            beat.rt.anchoredPosition = pos;
                        }
                        if (kd.prop.TryGetValue("Lifetime", out str)) beat.lifetime = int.Parse(str); else beat.lifetime = 2;
                        break;
                    case "LongFallingBlock":
                        LongFallingBlock lfb = (LongFallingBlock)block;
                        lfb.length = int.Parse(kd.prop["Length"]);
                        // 难度屏蔽
                        if (GeneralSettings.difficulty == 1) shieldingTime = 0.6f * lfb.length * 0.5f;
                        else if (GeneralSettings.difficulty == 2) shieldingTime = 0.6f * lfb.length * 1f;
                        break;
                    case "HorizontalMove":
                        HorizontalMove hrm = (HorizontalMove)block;
                        hrm.width = int.Parse(kd.prop["Width"]);
                        hrm.direction = kd.prop["Direction"] == "Up" ? Direction.Up : Direction.Down;
                        // 难度屏蔽
                        if (GeneralSettings.difficulty == 1) shieldingTime = 0.6f * hrm.width * 0.5f;
                        else if (GeneralSettings.difficulty == 2) shieldingTime = 0.6f * hrm.width * 1f;
                        break;
                    case "Harp":
                        Harp h = (Harp)block;
                        h.width = int.Parse(kd.prop["Width"]);
                        h.timeLast = float.Parse(kd.prop["TimeLast"]);
                        if (kd.prop.TryGetValue("Rouxian", out str)) h.rouxian = str == "yes";
                        if (h.rouxian)
                        {
                            str = kd.prop["Roufa"];
                            if (str == "deep") h.roufa = 0; else if (str == "shallow") h.roufa = 1;
                            str = kd.prop["Rousu"];
                            if (str == "quick") h.rousu = 0; else if (str == "slow") h.rousu = 1;
                            if (kd.prop.TryGetValue("LimitTime", out str)) h.limitTime = float.Parse(str); else h.limitTime = 0.5f;
                            if (kd.prop.TryGetValue("Cooldown", out str)) h.cooldown = float.Parse(str); else h.cooldown = 0.6f;
                        }
                        break;
                    case "VocalText":
                        VocalText vt = (VocalText)block;
                        if (kd.prop.TryGetValue("Text", out str)) vt.text = str;
                        seg = kd.prop["StartPosition"].Split(',');
                        vt.startPos = new Vector2(float.Parse(seg[0]) * DefRes.x, float.Parse(seg[1]) * DefRes.y);
                        seg = kd.prop["EndPosition"].Split(',');
                        vt.endPos = new Vector2(float.Parse(seg[0]) * DefRes.x, float.Parse(seg[1]) * DefRes.y);
                        vt.rotation = int.Parse(kd.prop["Rotation"]);
                        vt.beatInterval = float.Parse(kd.prop["BeatInterval"]);
                        vt.voEvent = kd.prop["Voice"];
                        vt.length = int.Parse(kd.prop["Length"]);
                        vt.num = int.Parse(kd.prop["Num"]);
                        if (kd.prop.TryGetValue("AnimColor", out str))
                        {
                            seg = str.Split(',');
                            vt.animColor = new Color32(byte.Parse(seg[0]), byte.Parse(seg[1]), byte.Parse(seg[2]), 255);
                        }
                        if (kd.prop.TryGetValue("MaxMiss", out str)) vt.maxMiss = int.Parse(str); else vt.maxMiss = 1;
                        if (kd.prop.TryGetValue("BeatLifetime", out str)) vt.beatLifetime = int.Parse(str); else vt.beatLifetime = 2;
                        if (kd.prop.TryGetValue("NoAnim", out str)) if (str == "yes") vt.noAnim = true;
                        break;
                    default:
                        break;
                }
                OnBlockCreated?.Invoke(block);
                if (shieldingTime > 0)
                {
                    difficultyShielding = true;
                    yield return new WaitForSeconds(shieldingTime);
                    difficultyShielding = false;
                }
            }
        }
        // yield return new WaitForSeconds(block.fallingTime);
        // SyncMusic(kd.startTime + block.fallingTime);

    }

    static bool IgnoresSpecialMode2(string blockType)
    {
        print(blockType);
        RhythmType t = Resources.Load<GameObject>(blockType).GetComponent<RhythmObject>().Type;
        return t == RhythmType.Misc;
    }
    static bool ShieldedByDifficulty(string blockType)
    {
        return ins.difficultyShielding && difficultyShieldingTypes.Contains(blockType);
    }

    IEnumerator StartMusic(float time)
    {
        yield return new WaitForSeconds(time);
        vEventIns.start();
        startTime = Time.time;
        RhythmGameManager.ins.pauseButton.gameObject.SetActive(true);
        FMOD.Studio.PLAYBACK_STATE state;
        do
        {
            yield return new WaitForSeconds(1);
            float sync = Time.time - startTime + GeneralSettings.musicStartTime;
            ins.vEventIns.setTimelinePosition((int)(sync * 1000));
            ins.vEventIns.getPlaybackState(out state);
        }
        while (state == FMOD.Studio.PLAYBACK_STATE.PLAYING);
    }

    public static void PlayVideo(int clipNum, int mode = 1)
    {
        if (ins.vp.isPlaying) return;
        ins.vp.clip = ins.clips[clipNum];
        ins.videoDisplay.GetComponent<RawImage>().texture = ins.vp.targetTexture = ins.videoRatios[mode];
        ins.videoDisplay.rectTransform.sizeDelta = new Vector2(mode == 0 ? DefRes.x * 0.75f : DefRes.x, DefRes.y);
        for (ushort u = 0; u < ins.vp.audioTrackCount; ++u) ins.vp.SetDirectAudioVolume(u, 1);
        ins.vp.Play();
        ins.StartCoroutine(ins.CheckVideoFinish());
    }

    IEnumerator CheckVideoFinish()
    {
        videoDisplay.gameObject.SetActive(true);
        for (float i = 0; i <= 1; i += 0.2f)
        {
            videoDisplay.GetComponent<Graphic>().color = new Color(1, 1, 1, i);
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds((float)vp.clip.length);
        for (float i = 1; i >= 0; i -= 0.2f)
        {
            videoDisplay.GetComponent<Graphic>().color = new Color(1, 1, 1, i);
            yield return new WaitForSeconds(0.1f);
        }
        videoDisplay.gameObject.SetActive(false);
    }

    IEnumerator FadeVideoVolume(float sec)
    {
        float step = sec / 10;
        for (float i = 1; i >= 0; i -= 0.1f)
        {
            for (ushort u = 0; u < vp.audioTrackCount; ++u) vp.SetDirectAudioVolume(u, i);
            yield return new WaitForSeconds(step);
        }
    }
    
    IEnumerator MusicFadeIn(float sec)
    {
        float step = sec / 10;
        for (float i = 0; i <= 1; i += 0.1f)
        {
            vEventIns.setVolume(i);
            yield return new WaitForSeconds(step);
        }
    }
    IEnumerator MusicFadeOut(float sec)
    {
        float step = sec / 10;
        for (float i = 1; i >= 0; i -= 0.1f)
        {
            vEventIns.setVolume(i);
            yield return new WaitForSeconds(step);
        }
    }
}
