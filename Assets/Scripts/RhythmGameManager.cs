using FMOD;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Android;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DefRes // 开发用分辨率
{
    public static int x { private set; get; } = 1920;
    public static int y { private set; get; } = 1080;
}

public struct GeneralSettings
{
    public static int mode; // 0=正常, 1=拖轨
    public static int exitCount;
    public static float bingguiSpeed = 1750;
}

public class ExitData
{
    public int id;
    public GameObject obj, idctor;
    public float x1, x2;
    public RhythmObject currentRhythmObject;
    public Vector2 center;
    public bool IsBeingTouchedBy(Touch t)
    {
        var pos = Utils.ScreenToCanvasPos(t.position);
        if (RhythmGameManager.bottomRect.Contains(pos) && pos.x > x1 && pos.x < x2)
            return true;
        return false;
    }
    public bool IsBeingTouchedBy(Touch t, out Vector2 pos)
    {
        pos = Utils.ScreenToCanvasPos(t.position);
        if (RhythmGameManager.bottomRect.Contains(pos) && pos.x > x1 && pos.x < x2)
            return true;
        return false;
    }
}

public class RhythmGameManager : MonoBehaviour
{
    [SerializeField] Transform parentNode;
    [SerializeField] GameObject selectSong;
    public static ExitData[] exits;
    public static ExitData binggui;
    [SerializeField] GameObject bottom;
    [SerializeField] Text UIScore;
    [SerializeField] bool showIndicators;
    [SerializeField] Button reload;
    public static float blockHeight { private set; get; } = 90; // 方块高度
    public static float exitWidth { private set; get; } = 180; // 出口/方块横向长度

    int score;
    float time;
    public static Rect bottomRect;
    Vector2 buttonStart = new Vector2(-720, 388);
    Vector2 buttonSpaces = new Vector2(360, -90);
    List<ExitData[]> oldExits = new List<ExitData[]>();

    public static RhythmGameManager ins;

    private void Awake()
    {
        ins = this;
        reload.onClick.AddListener(delegate 
        {
            SceneManager.LoadScene(0);
            Timeline.Stop();
        });
    }
    void Start()
    {
        score = 0;
        UIScore.text = "Score: 0";

        RectTransform brt = bottom.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(DefRes.x, blockHeight);
        bottomRect = new Rect(brt.anchoredPosition - brt.sizeDelta / 2, brt.sizeDelta);


        var info = new DirectoryInfo("Assets/Music/Resources");
        var files = info.GetFiles();
        int i = 0;
        foreach(var f in files)
        {
            if (f.Extension == ".b3ks")
            {
                string songName = f.Name.Substring(0, f.Name.Length - 5);
                var btn = Instantiate(Resources.Load<GameObject>("SongName"), selectSong.transform);
                btn.GetComponent<RectTransform>().anchoredPosition = buttonStart + new Vector2(i % 5 * buttonSpaces.x, i / 5 * buttonSpaces.y);
                btn.GetComponentInChildren<Text>().text = songName;
                btn.GetComponent<Button>().onClick.AddListener(delegate
                {
                    Timeline.StartMusicScript(songName);
                    GenerateExits();
                    Destroy(selectSong);
                });
                ++i;
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            print("*********************************");
            SceneManager.LoadScene(0);
        }
    }
    private void FixedUpdate()
    {
        BingguiUpdate();
        oldExits.RemoveAll(RemoveOldExits);
    }

    bool RemoveOldExits(ExitData[] es)
    {
        bool shouldBeRemoved = true;
        foreach (ExitData ed in es)
        {
            if (ed.currentRhythmObject)
            {
                shouldBeRemoved = false;
                break;
            }
        }
        if (shouldBeRemoved)
        {
            print("Removing");
            StopAllCoroutines();
            foreach (ExitData ed in es)
            {
                Destroy(ed.obj);
                if (ed.idctor) Destroy(ed.idctor);
            }
        }
        return shouldBeRemoved;
    }

    public void RestartGame(string newSongName = "", int startScore = 0)
    {
        score = startScore;
        UIScore.text = "Score: " + score;

        RectTransform brt = bottom.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(DefRes.x, blockHeight);
        bottomRect = new Rect(brt.anchoredPosition - brt.sizeDelta / 2, brt.sizeDelta);

        Timeline.Stop();
        Timeline.StartMusicScript(newSongName);
        GenerateExits();
    }

    public void StopGame()
    {
        exits = null;
        Timeline.Stop();
    }

    public static float GetBottom()
    {
        return (ins.bottom.transform as RectTransform).anchoredPosition.y;
    }
    public static void UpdateScore(int s)
    {
        ins.score += s;
        ins.UIScore.text = "Score: " + ins.score;
    }

    public static RhythmObject CreateBlock(int exit, string blockName, Color? c = null, int perfectScore = 20, int goodScore = 10, int badScore = 0)
    {
        string prefabName = blockName;
        if (GeneralSettings.mode > 0 && prefabName != "ChangeGameMode") prefabName += "_" + GeneralSettings.mode;
        print("Creating " + prefabName);
        return Instantiate(Resources.Load<GameObject>(prefabName), ins.parentNode).GetComponent<RhythmObject>().Initialize(exit, c == null ? Color.white : c.GetValueOrDefault(), perfectScore, goodScore, badScore);
    }

    public static void GenerateExits()
    {
        // handling old exits
        if (exits != null)
        {
            //for (int i = 0; i < exits.Length; ++i) ins.StartCoroutine(ins.ExitFlashing(exits[i]));
            ins.oldExits.Add(exits);
        }

        if (GeneralSettings.mode == 1) // 并轨
        {
            if (binggui != null) Destroy(binggui.obj);
            binggui = new ExitData();
            binggui.obj = Instantiate(Resources.Load<GameObject>("exit"), ins.parentNode);
            binggui.obj.GetComponentInChildren<Graphic>().color = Color.green;
            RectTransform rt = binggui.obj.transform as RectTransform;
            rt.sizeDelta = new Vector2(exitWidth, blockHeight);
            binggui.center = rt.anchoredPosition = new Vector2(0, GetBottom());
            binggui.x1 = rt.anchoredPosition.x - rt.sizeDelta.x / 2;
            binggui.x2 = rt.anchoredPosition.x + rt.sizeDelta.x / 2;
            var puff = Instantiate(Resources.Load<GameObject>("Puff"), ins.parentNode);
            (puff.transform as RectTransform).anchoredPosition = binggui.center;
        }
        else if (binggui != null)
        {
            Destroy(binggui.obj);
            binggui = null;
        }

        int num = GeneralSettings.exitCount;
        exits = new ExitData[num];
        float step = DefRes.x / num;
        float top = GeneralSettings.mode == 1 ? DefRes.y / 2 + blockHeight / 2 : DefRes.y / 2 - blockHeight;
        for (int i = 0; i < num; ++i)
        {
            exits[i] = new ExitData();
            exits[i].id = i;
            exits[i].obj = Instantiate(Resources.Load<GameObject>("exit"), ins.parentNode);
            RectTransform rt = exits[i].obj.transform as RectTransform;
            rt.sizeDelta = new Vector2(exitWidth, blockHeight);
            rt.anchoredPosition = new Vector2(step * (i + 0.5f) - DefRes.x / 2f, top);
            exits[i].x1 = rt.anchoredPosition.x - rt.sizeDelta.x / 2;
            exits[i].x2 = rt.anchoredPosition.x + rt.sizeDelta.x / 2;
            exits[i].center = new Vector2(rt.anchoredPosition.x, GetBottom());

            if (ins.showIndicators)
            {
                GameObject indicator = Instantiate(exits[i].obj, ins.parentNode);
                indicator.GetComponentInChildren<Graphic>().color = Color.black;
                rt = indicator.transform as RectTransform;
                Vector2 pos = rt.anchoredPosition;
                pos.y = GetBottom();
                rt.anchoredPosition = pos;
                exits[i].idctor = indicator;
            }

            var puff = Instantiate(Resources.Load<GameObject>("Puff"), ins.parentNode);
            (puff.transform as RectTransform).anchoredPosition = new Vector2(rt.anchoredPosition.x, top);
        }
    }

    IEnumerator ExitFlashing(ExitData e)
    {
        bool flashing = true;
        while (true)
        {
            flashing = !flashing;
            e.obj.SetActive(flashing);
            if (e.idctor) e.idctor.SetActive(flashing);
            yield return new WaitForSeconds(0.1f);
        }
    }

    void BingguiUpdate()
    {
        if (GeneralSettings.mode != 1) return;
        for (int i = 0; i < Input.touchCount; ++i)
        {
            Touch t = Input.GetTouch(i);
            var pos = Utils.ScreenToCanvasPos(t.position);
            if (true || bottomRect.Contains(pos))
            {
                if (binggui.center.x < pos.x)
                {
                    binggui.center.x = Mathf.Min(binggui.center.x + GeneralSettings.bingguiSpeed * Time.fixedDeltaTime, pos.x);
                    RectTransform rt = binggui.obj.GetComponent<RectTransform>();
                    rt.anchoredPosition = binggui.center;
                    binggui.x1 = rt.anchoredPosition.x - rt.sizeDelta.x / 2;
                    binggui.x2 = rt.anchoredPosition.x + rt.sizeDelta.x / 2;

                }
                if (binggui.center.x > pos.x)
                {
                    binggui.center.x = Mathf.Max(binggui.center.x - GeneralSettings.bingguiSpeed * Time.fixedDeltaTime, pos.x);
                    RectTransform rt = binggui.obj.GetComponent<RectTransform>();
                    rt.anchoredPosition = binggui.center;
                    binggui.x1 = rt.anchoredPosition.x - rt.sizeDelta.x / 2;
                    binggui.x2 = rt.anchoredPosition.x + rt.sizeDelta.x / 2;
                }
                break;
            }
        }
    }

    float altime;
    void Update_Debug_GenerateRandomBlocks()
    {
        if (altime > 303) return;
        if (time >= Random.Range(1.111f, 3.333f))
        {
            int exit = Random.Range(0, exits.Length);
            if (Random.Range(0, 15) == 1)
            {
                LongFallingBlock b = (LongFallingBlock)CreateBlock(exit, "LongFallingBlock", Utils.GetRandomColor());
                b.fallingTime = Random.Range(3.5f, 4.5f);
                b.length = Random.Range(2, 3);
                b.ApplyLength();
            }
            else
            {
                var b = CreateBlock(exit, "FallingBlock", Utils.GetRandomColor());
                b.fallingTime = Random.Range(3.5f, 4.5f);
            }
            altime += time;
            time = 0;
        }
        else time += Time.deltaTime;
    }
}
