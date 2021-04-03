using MidiParser;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MusicalLevelEditor : MonoBehaviour
{
    public enum EditingPage
    {
        Midi, Panel_Left, Panel_Right
    }
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
    [SerializeField] RectTransform[] contents;
    [SerializeField] Text trackTitle;
    [Header("Prefabs")]
    [SerializeField] GameObject trackToggle;
    [SerializeField] GameObject textObj;
    [SerializeField] GameObject textObj2;
    [SerializeField] GameObject barImage;
    [SerializeField] GameObject node;
    [SerializeField] GameObject noteBar;
    Vector2 buttonStart = new Vector2(-720, 388);
    Vector2 buttonSpaces = new Vector2(360, -90);
    Vector2 trackToggleStart = new Vector2(-815, 400);
    Vector2 trackToggleSpaces = new Vector2(240, -50);
    EditingPage curPage = (EditingPage)(-1);
    public static float lengthPerSec = 100;
    public static float heightStep = 15;
    public static GameObject[] trackOfbars;

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
            btn.GetComponentInChildren<Button>().onClick.AddListener(delegate { LoadMidi(filename); });
            ++i;
        }
        for (int j = 0; j < pageTabs.Length; ++j)
        {
            EditingPage p = (EditingPage)j;
            pageTabs[j].onClick.AddListener(delegate { SelectTab(p); });
        }
        editingPage.SetActive(false);
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
            (t.transform as RectTransform).anchoredPosition = trackToggleStart + new Vector2(i % 9 * trackToggleSpaces.x, i / 9 * trackToggleSpaces.y);
            t.GetComponent<TrackToggle>().trackNum = i;
            Text text = t.GetComponentInChildren<Text>();
            text.text = i + ". " + MidiTranslator.trackNames[i];
            text.color = trackColors[i];
        }

        // set size of ScrollRect
        float contentTopEdge = ((MidiTranslator.tracks.Length - 1) / 9 + 1) * trackToggleSpaces.y + 20 + trackToggleStart.y;
        float contentBottomEdge = -1080 / 2 + 10;
        RectTransform scrollRT = subPages[(int)EditingPage.Midi].GetComponentInChildren<ScrollRect>().transform as RectTransform;
        scrollRT.sizeDelta = new Vector2(1900, contentTopEdge - contentBottomEdge);
        scrollRT.anchoredPosition = new Vector2(0, (contentTopEdge + contentBottomEdge) / 2f);

        // ScrollRect Content Setup
        float contentHeight = 128 * heightStep + 70;
        float contentWidth = 1900 + (MidiTranslator.endTime + 60) * lengthPerSec;
        float contentBottomStart = -contentHeight / 2f + 50;
        contents[0].sizeDelta = new Vector2(contentWidth - 1900, contentHeight);
        for (int i = 0; i < 128; ++i)
        {
            var to = Instantiate(textObj, contents[0]);
            to.GetComponent<RectTransform>().anchoredPosition = new Vector2(-contentWidth / 2 + 80, contentBottomStart + i * heightStep);
            to.GetComponent<Text>().text = i + "-";
            /*
            if (i != 0 && i % 16 == 0) 
            {
                for (int j = 0; j * lengthPerSec < contentWidth; ++j)
                {
                    to = Instantiate(textObj2, contents[0]);
                    to.GetComponent<RectTransform>().anchoredPosition = new Vector2(-contentWidth / 2 + 80 + j * lengthPerSec, contentBottomStart + i * heightStep);
                    to.GetComponent<Text>().text = j.ToString();
                    to.GetComponent<Text>().color = Color.grey;
                }
            }
            */
        }
        var to2 = Instantiate(textObj, contents[0]);
        to2.GetComponent<RectTransform>().anchoredPosition = new Vector2(-contentWidth / 2 + 80, contentBottomStart + 128 * heightStep);
        to2.GetComponent<Text>().text = "音符";
        var line = Instantiate(barImage, contents[0]);
        line.GetComponent<Image>().color = Color.black;
        var rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2, contentHeight);
        rt.anchoredPosition = new Vector2(-contentWidth / 2 + 80, 0);
        line = Instantiate(barImage, contents[0]);
        line.GetComponent<Image>().color = Color.black;
        rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(contentWidth, 2);
        rt.anchoredPosition = new Vector2(0, contentBottomStart - 20);
        for (int i = 1; i * lengthPerSec < contentWidth; ++i)
        {
            float pos_x = -contentWidth / 2 + 80 + i * lengthPerSec;
            line = Instantiate(barImage, contents[0]);
            line.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            rt = line.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2, contentHeight);
            rt.anchoredPosition = new Vector2(pos_x, 0);
            var to = Instantiate(textObj2, contents[0]);
            to.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos_x, contentBottomStart - 35);
            to.GetComponent<Text>().text = i.ToString();
        }
        line = Instantiate(barImage, contents[0]);
        line.GetComponent<Image>().color = Color.red;
        rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2, contentHeight);
        rt.anchoredPosition = new Vector2(-contentWidth / 2 + MidiTranslator.endTime * lengthPerSec, 0);

        for (int i = 0; i < MidiTranslator.tracks.Length; ++i)
        {
            trackOfbars[i] = Instantiate(node, contents[0]);
            for (int j = 0; j < MidiTranslator.tracks[i].Count; ++j)
            {
                Note n = MidiTranslator.tracks[i][j];
                var bar = Instantiate(noteBar, trackOfbars[i].transform);
                bar.GetComponent<NoteBar>().note = n;
                bar.GetComponent<Image>().color = trackColors[n.track];
                rt = bar.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(n.startTimeInSec * lengthPerSec - contentWidth / 2, contentBottomStart + n.note * heightStep);
                rt.sizeDelta = new Vector2(n.lengthInSec * lengthPerSec, heightStep);
            }
        }
    }
    void SelectTab(EditingPage tab)
    {
        if (tab == curPage) return;
        for (int i = 0; i < 3; ++i)
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

    void ShowTrack(bool show)
    {

    }
}
