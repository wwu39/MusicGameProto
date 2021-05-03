using MidiParser;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public enum EditingPage
{
    Midi, Panel_Left, Panel_Right
}

public class MusicalLevelEditor : MonoBehaviour
{
    public static MusicalLevelEditor ins;
    public static Color[] trackColors = new Color[]
    {
        new Color(1,0,0), new Color(1,1,0), new Color(0,1,0), new Color(0,1,1), new Color(0,0,1), new Color(1,0,1),
        new Color(1,0.5f,0), new Color(0.5f,1,0), new Color(0,1,0.5f), new Color(0,0.5f,1), new Color(0.5f,0,1), new Color(1,0,0.5f)
    };
    [SerializeField] Text title;
    [SerializeField] GameObject explorerPage;
    [SerializeField] GameObject editingPage;
    [SerializeField] GameObject[] subPages;
    [SerializeField] Button[] pageTabs;
    public ScrollRect[] scrolls;
    public RectTransform[] contents;
    [Header("Midi Page")]
    [SerializeField] Text trackTitle;
    [SerializeField] Text midiSelectInfo;
    [SerializeField] Text midiInfo;
    [SerializeField] MidiOperation midiOperationPanel;
    [Header("Level Page")]
    [SerializeField] InputField ExitCount;
    [SerializeField] InputField MusicEvent;
    [SerializeField] InputField GameMode;
    [SerializeField] InputField Delay;
    [SerializeField] InputField MusicStartPosition;
    [SerializeField] Toggle MidiTrack;
    [SerializeField] Button enterGame;
    [SerializeField] Toggle reverseRight;
    [Header("Prefabs")]
    public GameObject trackToggle;
    public GameObject textObj;
    public GameObject textObj2;
    public GameObject barImage;
    public GameObject node;
    public GameObject noteBar;
    public GameObject metaEvent;
    public GameObject playButton;
    public GameObject timeSelectPad;
    public GameObject rulerInteractable;
    [Header("BlockRepresentation")]
    [SerializeField] GameObject fallingBlock;
    Vector2 buttonStart = new Vector2(-720, 388);
    Vector2 buttonSpaces = new Vector2(360, -90);
    Vector2 trackToggleStart = new Vector2(-815, 400);
    Vector2 trackToggleSpaces = new Vector2(240, -50);
    EditingPage curPage = (EditingPage)(-1);
    public static float lengthPerSec_midiPage = 100;
    public static float heightStep = 15;
    public static GameObject[] trackOfbars;
    GameObject midiRuler;
    GameObject timeRuler;
    List<KeyData> keyData;
    public static float lengthPerSec_levelPage = PanelSize.x * 2 / 3;
    float levelPageContentWidth;
    float levelPageContentHeight = 410;
    float levelPageContentStart;
    public Vector2[] mousePositionInScroll { private set; get; }
    public SelectPad[] selectPads;

    public static ExitData[] exits;
    
    enum MidiPageState
    {
        None,
        Playback
    }
    struct MidiPage
    {
        public float ContentWidth;
        public float ContentHeight;
        public float ContentStart;

        public float playbackPos;
        public RectTransform playbackTick;
        public MidiPageState state;
        public float startTime;
        public float selectStartTime;
        public float endTime;
        public float absoluteStartTime;
        public List<NoteBar> allNotes;
        public HashSet<NoteBar> selected;
        public int curNote;
        public GameObject playButton;
        public GameObject center;
        public int FindNote(float time) => FindNote(0, allNotes.Count - 1, time);
        int FindNote(int st, int ed, float time)
        {
            if (ed - st < 16)
            {
                for(int i = st; i <= ed; ++i) 
                    if (allNotes[i].note.startTimeInSec >= time)
                        return i;
                return ed;
            }
            int mid = (st + ed) / 2;
            if (time > allNotes[mid].note.startTimeInSec) return FindNote(mid + 1, ed, time);
            else if (time < allNotes[mid].note.startTimeInSec) return FindNote(st, mid - 1, time);
            else return mid;
        }
    }
    MidiPage midiPage;
    public float MidiContentHeight => midiPage.ContentHeight;
    struct LevelPage
    {
        public RectTransform[] nodes;
    }
    LevelPage levelPage;

    private void Awake()
    {
        ins = this;
    }
    private void Start()
    {
        string dataPath = Application.dataPath;
        string midiFilesPath = dataPath.Substring(0, dataPath.Length - 6) + "Midi/";
        title.text = "Midi文件路径 " + midiFilesPath;
        var info = new DirectoryInfo(midiFilesPath);
        var files = info.GetFiles();
        int i = 0;
        foreach (var f in files)
        {
            string filename = f.Name.Substring(0, f.Name.Length - 4);
            var btn = Instantiate(Resources.Load<GameObject>("SongName"), explorerPage.transform);
            btn.name = "Button_LoadMidi";
            btn.GetComponent<RectTransform>().anchoredPosition = buttonStart + new Vector2(i % 5 * buttonSpaces.x, i / 5 * buttonSpaces.y);
            btn.GetComponentInChildren<Text>().text = filename;
            btn.GetComponentInChildren<Button>().onClick.AddListener(delegate {
                LoadMidi(filename);
                SetupLevelPage();
            });
            ++i;
        }
        for (int j = 0; j < pageTabs.Length; ++j)
        {
            EditingPage p = (EditingPage)j;
            pageTabs[j].onClick.AddListener(delegate { SelectTab(p); });
        }
        enterGame.onClick.AddListener(delegate
        {
            SelectLevel.ins.preselectedSongName = MidiTranslator.filename;
            SceneManager.LoadScene(1);
        });
        editingPage.SetActive(false);
        mousePositionInScroll = new Vector2[3];
        selectPads = new SelectPad[3];
    }
    void LoadMidi(string filename)
    {
        // MidiPage setup
        explorerPage.SetActive(false);
        editingPage.SetActive(true);
        SelectTab(0);
        MidiTranslator.filename = filename;
        MidiTranslator.MakeNotes();
        MidiTranslator.PrepareAllTracks();
        trackTitle.text = "音轨数：" + MidiTranslator.tracks.Length;
        trackOfbars = new GameObject[MidiTranslator.tracks.Length];
        for (int i = 0; i < MidiTranslator.tracks.Length; ++i)
        {
            var t = Instantiate(trackToggle, subPages[(int)EditingPage.Midi].transform);
            (t.transform as RectTransform).anchoredPosition = trackToggleStart + new Vector2(i % 8 * trackToggleSpaces.x, i / 8 * trackToggleSpaces.y);
            t.GetComponent<TrackToggle>().trackNum = i;
            Text text = t.GetComponentInChildren<Text>();
            text.text = i + ". " + MidiTranslator.trackNames[i];
            text.color = trackColors[i];
            trackOfbars[i] = Instantiate(node, contents[0]);
        }

        // set size of ScrollRect
        float contentTopEdge = ((MidiTranslator.tracks.Length - 1) / 8 + 1) * trackToggleSpaces.y + 20 + trackToggleStart.y;
        float contentBottomEdge = -1080 / 2 + 10;
        scrolls[0].onValueChanged.AddListener(OnMidiPageDragged);
        RectTransform scrollRT = scrolls[0].transform as RectTransform;
        scrollRT.sizeDelta = new Vector2(1900, contentTopEdge - contentBottomEdge);
        scrollRT.anchoredPosition = new Vector2(0, (contentTopEdge + contentBottomEdge) / 2f);
        scrolls[1].onValueChanged.AddListener(OnLevelPageLeftDragged);
        scrolls[2].onValueChanged.AddListener(OnLevelPageRightDragged);
        reverseRight.isOn = false;
        reverseRight.onValueChanged.AddListener(ReverseRight);

        // Play Button
        midiPage.playButton = Instantiate(playButton, subPages[0].transform);
        midiPage.playButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, contentTopEdge - 35);
        midiPage.playButton.GetComponent<Button>().onClick.AddListener(OnPlayButtonClicked);
        midiPage.center = Instantiate(barImage, contents[0]);
        midiPage.center.name = "DEBUG";
        midiPage.center.GetComponent<Image>().raycastTarget = false;
        (midiPage.center.transform as RectTransform).sizeDelta = new Vector2(5, 5);

        // Select Pad
        selectPads[0] = Instantiate(timeSelectPad, subPages[0].transform).GetComponent<SelectPad>();
        selectPads[0].page = EditingPage.Midi;
        selectPads[0].GetComponent<RectTransform>().anchoredPosition = new Vector2(700, contentTopEdge - 35);

        // ScrollRect Content Setup
        midiPage.ContentHeight = 128 * heightStep + 70;
        midiPage.ContentWidth = 1900 + (MidiTranslator.endTime + 60) * lengthPerSec_midiPage;
        midiPage.ContentStart = -midiPage.ContentWidth / 2 + 80;
        midiPage.startTime = 0;
        midiPage.endTime = MidiTranslator.endTime;
        float contentBottomStart = -midiPage.ContentHeight / 2f + 50;
        contents[0].sizeDelta = new Vector2(midiPage.ContentWidth - scrollRT.sizeDelta.x, midiPage.ContentHeight);

        // 竖轴 - 音符标尺
        midiRuler = Instantiate(node, contents[0]);
        midiRuler.name = "MidiRuler";
        (midiRuler.transform as RectTransform).anchoredPosition = new Vector2(midiPage.ContentStart, 0);
        for (int i = 0; i < 128; ++i)
        {
            var to = Instantiate(textObj, midiRuler.transform);
            to.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, contentBottomStart + i * heightStep);
            to.GetComponent<Text>().text = i + "-";
        }
        var to2 = Instantiate(textObj, midiRuler.transform);
        to2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, contentBottomStart + 128 * heightStep);
        to2.GetComponent<Text>().text = "音符";
        var line = Instantiate(barImage, midiRuler.transform);
        line.GetComponent<Image>().color = Color.black;
        var rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2, midiPage.ContentHeight);

        // 横轴 - 时间标尺
        timeRuler = Instantiate(node, contents[0]);
        timeRuler.name = "TimeRuler";
        (timeRuler.transform as RectTransform).anchoredPosition = new Vector2(0, contentBottomStart - 20);
        line = Instantiate(barImage, timeRuler.transform);
        line.GetComponent<Image>().color = Color.black;
        rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(midiPage.ContentWidth, 2);
        for (int i = 0; i * lengthPerSec_midiPage < midiPage.ContentWidth; ++i)
        {
            float pos_x = midiPage.ContentStart + i * lengthPerSec_midiPage;
            line = Instantiate(barImage, contents[0]);
            line.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            rt = line.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2, midiPage.ContentHeight);
            rt.anchoredPosition = new Vector2(pos_x, 0);
            var to = Instantiate(textObj2, timeRuler.transform);
            to.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos_x, -8);
            to.GetComponent<Text>().text = "^\n" + i;
        }
        // 时间标尺交互
        line = Instantiate(rulerInteractable, timeRuler.transform);
        line.GetComponent<RulerClickable>().page = EditingPage.Midi;
        line.GetComponent<Image>().color = new Color(1, 0, 0, 0.5f);
        line.name = "Time Ruler Interactable";
        rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(midiPage.ContentWidth, 40);
        rt.anchoredPosition = new Vector2(0, -20);

        // 终点线
        line = Instantiate(barImage, contents[0]);
        line.GetComponent<Image>().color = Color.red;
        rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2, midiPage.ContentHeight);
        rt.anchoredPosition = new Vector2(midiPage.ContentStart + MidiTranslator.endTime * lengthPerSec_midiPage, 0);

        midiPage.allNotes = new List<NoteBar>();
        midiPage.selected = new HashSet<NoteBar>();
        for (int i = 0; i < MidiTranslator.tracks.Length; ++i)
        {
            for (int j = 0; j < MidiTranslator.tracks[i].Count; ++j)
            {
                Note n = MidiTranslator.tracks[i][j];
                var bar = Instantiate(noteBar, trackOfbars[i].transform);
                var notebar = bar.GetComponent<NoteBar>();
                notebar.note = n;
                bar.GetComponent<Image>().color = trackColors[n.track];
                rt = bar.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(midiPage.ContentStart + n.startTimeInSec * lengthPerSec_midiPage, contentBottomStart + n.note * heightStep);
                rt.sizeDelta = new Vector2(n.lengthInSec * lengthPerSec_midiPage, heightStep);
                midiPage.allNotes.Add(notebar);
            }
        }
        midiPage.allNotes.Sort((x, y) => x.note.startTimeInSec.CompareTo(y.note.startTimeInSec));
        for (int i = 0; i < midiPage.allNotes.Count; ++i) midiPage.allNotes[i].num = i;

        // 播放指针
        line = Instantiate(barImage, contents[0].transform);
        line.GetComponent<Image>().color = Color.black;
        rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2, midiPage.ContentHeight);
        rt.anchoredPosition = new Vector2(midiPage.ContentStart, 0);
        midiPage.playbackPos = 0;
        midiPage.playbackTick = rt;

        for (int i = 0; i < 3; ++i) contents[i].anchoredPosition = new Vector2(); 
        RefreshInfoPanel();
    }

    void SetupLevelPage()
    {
        Dictionary<string, Dictionary<string, string>> sections;
        Interpreter.Open(MidiTranslator.filename, out keyData, out sections);
        string str;
        ExitCount.text = sections["General"]["Exit"];
        if (sections["General"].TryGetValue("Music", out str)) MusicEvent.text = str; else MusicEvent.text = MidiTranslator.filename;
        if (sections["General"].TryGetValue("GameMode", out str)) GameMode.text = str; else GameMode.text = "0";
        if (sections["General"].TryGetValue("Delay", out str)) Delay.text = str; else Delay.text = "3";
        if (sections["General"].TryGetValue("MusicStartPosition", out str)) MusicStartPosition.text = str; else MusicStartPosition.text = "0";
        if (sections["General"].TryGetValue("MidiTrack", out str)) MidiTrack.isOn = str == "yes"; else MidiTrack.isOn = false;
        levelPage.nodes = new RectTransform[2];
        levelPage.nodes[0] = Instantiate(node, contents[1]).GetComponent<RectTransform>();
        levelPage.nodes[1] = Instantiate(node, contents[2]).GetComponent<RectTransform>();
        // find end pos
        GameObject line;
        RectTransform rt;
        foreach (var kd in keyData)
        {
            if (kd.prop.TryGetValue("Type", out str))
            {
                if (str == "GameOver")
                {
                    levelPageContentWidth = (kd.startTime + 2) * lengthPerSec_levelPage;
                    levelPageContentStart = -levelPageContentWidth / 2 + 50;
                    contents[1].sizeDelta = contents[2].sizeDelta = new Vector2(levelPageContentWidth - 1900, levelPageContentHeight);
                    MetaEventShow(Color.red, "游戏结束", kd.startTime, 0);
                    break;
                }
            }
        }
        // start line
        exits = new ExitData[6];
        for (int page = 1; page <= 2; ++page)
        {
            line = Instantiate(barImage, levelPage.nodes[page - 1]);
            line.GetComponent<Image>().color = new Color32(99, 198, 255, 255);
            rt = line.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(4, levelPageContentHeight);
            rt.anchoredPosition = new Vector2(levelPageContentStart, 0);
            for (int i = 0; i < 3; ++i)
            {
                var e = exits[i + (page - 1) * 3] = new ExitData();
                e.id = i;
                e.panel = (PanelType)(page - 1);
                e.obj = Instantiate(Resources.Load<GameObject>("exit"), line.transform);
                rt = e.obj.transform as RectTransform;
                rt.sizeDelta = new Vector2(BlockSize.x, BlockSize.y);
                rt.anchoredPosition = new Vector2(0, ExitPos(i));
                e.y_top = rt.anchoredPosition.y + rt.sizeDelta.y / 2;
                e.y_bot = rt.anchoredPosition.y - rt.sizeDelta.y / 2;
                e.center = rt.anchoredPosition;
            }

            // 时间标尺
            var ruler = Instantiate(node, levelPage.nodes[page - 1]);
            ruler.name = "TimeRuler";
            (ruler.transform as RectTransform).anchoredPosition = new Vector2(0, -levelPageContentHeight / 2 + 20);
            line = Instantiate(barImage, ruler.transform);
            line.GetComponent<Image>().color = Color.black;
            rt = line.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(levelPageContentWidth, 2);
            for (int i = 0; i * lengthPerSec_levelPage < levelPageContentWidth; ++i)
            {
                var to = Instantiate(textObj2, ruler.transform);
                to.GetComponent<RectTransform>().anchoredPosition = new Vector2(levelPageContentStart + i * lengthPerSec_levelPage, -8);
                to.GetComponent<Text>().text = "^\n" + i;
            }
        }
        // parse all events
        foreach (var kd in keyData) ParseKeyData(kd);
    }

    void ParseKeyData(KeyData kd)
    {
        int exit = 0;
        PanelType panel = PanelType.Left;
        string str; string[] seg;
        if (kd.prop.TryGetValue("Exit", out str)) exit = int.Parse(str);
        if (kd.prop.TryGetValue("Panel", out str)) if (str == "Right") panel = PanelType.Right;

        string blockType = "None";
        if (kd.prop.TryGetValue("Type", out str)) blockType = str;
        if (GeneralSettings.specialMode == 1) blockType = "FallingBlock";
        Color panelEventColor = new Color(0, 0, 1, 0.5f);
        if (blockType == "GameOver") return;
        else if (blockType == "ShowLeftPanel") MetaEventShow(panelEventColor, "开启左键盘", kd.startTime, 0.6f);
        else if (blockType == "HideLeftPanel") MetaEventShow(panelEventColor, "隐藏左键盘", kd.startTime, 0.6f);
        else if (blockType == "ShowRightPanel") MetaEventShow(panelEventColor, "开启右键盘", kd.startTime, 0.6f);
        else if (blockType == "HideRightPanel") MetaEventShow(panelEventColor, "隐藏右键盘", kd.startTime, 0.6f);
        else if (blockType == "HideBothPanels") MetaEventShow(panelEventColor, "隐藏左右键盘", kd.startTime, 0.6f);
        else if (blockType != "None")
        {
            Color c = Utils.GetRandomColor();
            if (kd.prop.TryGetValue("Color", out str))
            {
                seg = str.Split(',');
                c = new Color32(byte.Parse(seg[0]), byte.Parse(seg[1]), byte.Parse(seg[2]), 255);
            }
            RhythmObject ro = Instantiate(Resources.Load<GameObject>(blockType), levelPage.nodes[(int)panel]).GetComponent<RhythmObject>().Initialize_LevelEditor(exit, panel, c);
            ro.rt.anchoredPosition = new Vector2(levelPageContentStart + kd.startTime * lengthPerSec_levelPage, ExitPos(exit));
            ro.noUpdate = true;
            ro.gameObject.AddComponent<EditorEvent>().eventType = EventType.Note;
            switch (blockType)
            {
                case "FallingBlock":
                    break;
                case "LongFallingBlock":
                    LongFallingBlock lfb = (LongFallingBlock)ro;
                    lfb.length = int.Parse(kd.prop["Length"]);
                    break;
                case "HorizontalMove":
                    HorizontalMove hrm = (HorizontalMove)ro;
                    hrm.width = int.Parse(kd.prop["Width"]);
                    hrm.direction = kd.prop["Direction"] == "Up" ? Direction.Up : Direction.Down;
                    break;
            }
        }
    }
    void MetaEventShow(Color c, string txt, float st, float l)
    {
        for (int i = 1; i <= 2; ++i)
        {
            var line = Instantiate(metaEvent, levelPage.nodes[i - 1]);
            line.GetComponent<Image>().color = c;
            line.GetComponentInChildren<Text>().text = txt;
            var rt = line.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(Mathf.Max(l * lengthPerSec_levelPage, 4), levelPageContentHeight);
            rt.anchoredPosition = new Vector2(levelPageContentStart + st * lengthPerSec_levelPage + rt.sizeDelta.x / 2, 0);
            line.AddComponent<Button>();
            line.AddComponent<EditorEvent>().eventType = EventType.Meta;
        }
    }
    void SelectTab(EditingPage tab)
    {
        if (tab == curPage) return;
        for (int i = 0; i < 2; ++i)
        {
            if ((int)tab == i)
            {
                pageTabs[i].GetComponentInChildren<Image>().color = new Color32(200, 200, 200, 255);
                subPages[i].SetActive(true);
            }
            else
            {
                pageTabs[i].GetComponentInChildren<Image>().color = new Color32(255, 255, 255, 255);
                subPages[i].SetActive(false);
            }
        }
        curPage = tab;
    }

    void OnMidiPageDragged(Vector2 center)
    {
        Vector2 scrollSize = scrolls[0].GetComponent<RectTransform>().sizeDelta;
        float x = (-0.5f + center.x) * (midiPage.ContentWidth - scrollSize.x) - scrollSize.x / 2 + 80;
        float y = (-0.5f + center.y) * (midiPage.ContentHeight - scrollSize.y) - scrollSize.y / 2 + 50;
        (midiRuler.transform as RectTransform).anchoredPosition = new Vector2(x, 0);
        (timeRuler.transform as RectTransform).anchoredPosition = new Vector2(0, y);
    }

    Vector2 GetMidiScrollLocalCenter()
    {
        Vector2 center = scrolls[0].normalizedPosition;
        Vector2 scrollSize = scrolls[0].GetComponent<RectTransform>().sizeDelta;
        return new Vector2((-0.5f + center.x) * (midiPage.ContentWidth - scrollSize.x), (-0.5f + center.y) * (midiPage.ContentHeight - scrollSize.y));
    }

    void OnLevelPageLeftDragged(Vector2 pos)
    {
        if (!reverseRight.isOn) scrolls[(int)EditingPage.Panel_Right].horizontalNormalizedPosition = pos.x;
        else scrolls[(int)EditingPage.Panel_Right].horizontalNormalizedPosition = 1 - pos.x;
    }
    void OnLevelPageRightDragged(Vector2 pos)
    {
        if (!reverseRight.isOn) scrolls[(int)EditingPage.Panel_Left].horizontalNormalizedPosition = pos.x;
        else scrolls[(int)EditingPage.Panel_Left].horizontalNormalizedPosition = 1 - pos.x;
    }
    void ReverseRight(bool b)
    {
        levelPage.nodes[1].localScale = new Vector3(b ? -1 : 1, 1, 1);
    }

    float ExitPos(int exitIdx) => (1 - exitIdx) * PanelSize.y * 0.3f;
    
    void OnPlayButtonClicked()
    {
        if (midiPage.state == MidiPageState.Playback)
        {
            midiPage.state = MidiPageState.None;
            midiPage.startTime = midiPage.playbackPos;
            midiPage.playButton.GetComponentInChildren<Text>().text = "播放";
        }
        else if (midiPage.state == MidiPageState.None)
        {
            midiPage.absoluteStartTime = Time.time;
            midiPage.state = MidiPageState.Playback;
            midiPage.playButton.GetComponentInChildren<Text>().text = "暂停";
        }
    }

    public static void SelectNote(NoteBar nb, bool adding)
    {
        if (!adding) DeselectAll();
        ins.midiPage.selected.Add(nb);
        ins.RefreshInfoPanel();
    }
    public static void DeselectNote(NoteBar nb)
    {
        ins.midiPage.selected.Remove(nb);
        ins.RefreshInfoPanel();
    }
    public static void DeselectAll()
    {
        List<NoteBar> l = new List<NoteBar>();
        foreach (var nb in ins.midiPage.selected) l.Add(nb);
        ins.midiPage.selected.Clear();
        foreach (var nb in l) nb.RefreshSelectedState();
        ins.RefreshInfoPanel();
    }
    public static void SelectRange(float startTime, float endTime)
    {
        DeselectAll();
        int stIdx = ins.midiPage.FindNote(startTime);
        int edIdx = ins.midiPage.FindNote(endTime);
        ins.midiPage.curNote = stIdx;
        ins.midiPage.selectStartTime = ins.midiPage.startTime = startTime;
        ins.midiPage.endTime = endTime;
        for (int i = stIdx; i < edIdx; ++i)
        {
            if (ins.midiPage.allNotes[i].gameObject.activeInHierarchy)
            {
                ins.midiPage.selected.Add(ins.midiPage.allNotes[i]);
                ins.midiPage.allNotes[i].RefreshSelectedState();
            }
        }
        ins.RefreshInfoPanel();
    }

    public static bool IsSelected(NoteBar nb) => ins.midiPage.selected.Contains(nb);
    void RefreshInfoPanel()
    {
        if (midiPage.selected.Count == 0)
        {
            midiSelectInfo.text = "已选择" + midiPage.selected.Count + "个音符";
            (midiSelectInfo.transform.parent as RectTransform).anchoredPosition = new Vector2(785, -500);
            midiInfo.transform.parent.gameObject.SetActive(false);
            midiOperationPanel.gameObject.SetActive(false);
        }
        else if (midiPage.selected.Count == 1)
        {
            midiSelectInfo.text = "已选择" + midiPage.selected.Count + "个音符";
            (midiSelectInfo.transform.parent as RectTransform).anchoredPosition = new Vector2(785,16);
            midiInfo.transform.parent.gameObject.SetActive(true);
            midiOperationPanel.gameObject.SetActive(true);
            Note n = null;
            int num = -1;
            foreach (var nb in midiPage.selected)
            {
                n = nb.note;
                num = nb.num;
                break;
            }
            string info = "Midi信息(" + num + ")";
            info += "\n音符值：" + n.note;
            info += "\n开始时间：" + n.startTimeInSec;
            info += "\n持续时间：" + n.lengthInSec;
            info += "\n结束时间：" + (n.startTimeInSec + n.lengthInSec);
            info += "\n音轨：";
            info += n.track + "(" + MidiTranslator.trackNames[n.track];
            info += ")\n入键速度：" + n.InitialVelocity;
            info += "\n出键速度：" + n.FinalVelocity;
            info += "\n\n内嵌信息\n";
            ins.midiInfo.text = info;
        }
        else
        {
            midiSelectInfo.text = "已选择" + midiPage.selected.Count + "个音符";
            (midiSelectInfo.transform.parent as RectTransform).anchoredPosition = new Vector2(785, -242);
            midiInfo.transform.parent.gameObject.SetActive(false);
            midiOperationPanel.gameObject.SetActive(true);
        }
    }

    public float TimeToPagePosX(EditingPage page, float t)
    {
        if (page == EditingPage.Midi) return midiPage.ContentStart + t * lengthPerSec_midiPage;
        else return levelPageContentStart + t * lengthPerSec_levelPage;
    }
    public float PagePosXToTime(EditingPage page, float x)
    {
        if (page == EditingPage.Midi) return (x - midiPage.ContentStart) / lengthPerSec_midiPage;
        else return (x - levelPageContentStart) / lengthPerSec_levelPage;
    }

    public void ScrollMoveLeft(EditingPage page)
    {
        float newPos = scrolls[(int)page].horizontalNormalizedPosition - 0.01f * Time.deltaTime;
        if (newPos > 0) scrolls[(int)page].horizontalNormalizedPosition = newPos;
        else scrolls[(int)page].horizontalNormalizedPosition = 0;
    }
    public void ScrollMoveRight(EditingPage page)
    {
        scrolls[(int)page].horizontalNormalizedPosition += 0.01f * Time.deltaTime;
    }

    private void Update()
    {
        if (midiPage.state == MidiPageState.Playback)
        {
            midiPage.playbackPos = Time.time - midiPage.absoluteStartTime + midiPage.startTime;
            if (midiPage.playbackPos >= midiPage.endTime)
            {
                midiPage.state = MidiPageState.None;
                midiPage.playbackPos = midiPage.startTime = midiPage.selectStartTime;
                midiPage.curNote = midiPage.FindNote(midiPage.startTime);
                midiPage.playbackTick.anchoredPosition = new Vector2(midiPage.ContentStart + midiPage.startTime * lengthPerSec_midiPage, 0);
                midiPage.playButton.GetComponentInChildren<Text>().text = "播放";
            }
            else
            {
                midiPage.playbackTick.anchoredPosition = new Vector2(midiPage.ContentStart + midiPage.playbackPos * lengthPerSec_midiPage, 0);
                if (midiPage.playbackPos >= midiPage.allNotes[midiPage.curNote].note.startTimeInSec)
                {
                    if (midiPage.allNotes[midiPage.curNote].isActiveAndEnabled) midiPage.allNotes[midiPage.curNote].PlaySound();
                    ++midiPage.curNote;
                }
            }
        }

        // debug
        if (midiPage.center)
        {
            mousePositionInScroll[0] = (midiPage.center.transform as RectTransform).anchoredPosition = Utils.ScreenToCanvasPos(Input.mousePosition) + GetMidiScrollLocalCenter() - (scrolls[0].transform as RectTransform).anchoredPosition;
        }

        // control
        if (Input.GetMouseButtonDown(1)) // mouse right click
        {
            DeselectAll();
        }
    }
}
