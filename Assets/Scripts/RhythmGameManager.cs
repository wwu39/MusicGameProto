using FMOD;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DefRes // 开发用分辨率
{
    public static int x { private set; get; } = 1920;
    public static int y { private set; get; } = 1080;
}
public class BlockSize
{
    public static int x = 180;
    public static int y = 90;
}

public struct GeneralSettings
{
    public static int mode; // 0=正常, 1=拖轨
    public static int exitCount;
    public static float delay;
    public static float bingguiSpeed = 2750;
    public static int specialMode = 0; // 0=正常, 1=把所有方块当作下落方块, 2=不下落任何方块
    public static void Reset()
    {
        mode = 0;
        exitCount = 0;
        delay = 0;
        bingguiSpeed = 2750;
        specialMode = 0;
    }
}

public struct Scoring
{
    public static int perfectCount;
    public static int goodCount;
    public static int missCount;
    public static void Reset()
    {
        perfectCount = 0;
        goodCount = 0;
        missCount = 0;
    }
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
    [SerializeField] GameObject gameContent;
    [SerializeField] GameObject selectSong;
    public static ExitData[] exits;
    public static ExitData binggui;
    [SerializeField] GameObject bottom;
    [SerializeField] Text UIScore;
    [SerializeField] bool showIndicators;
    [SerializeField] Button reload;
    public Button pauseButton;
    [SerializeField] Sprite[] pauseButtonSprites;
    [SerializeField] Text timeShown;
    [SerializeField] GameObject loading;
    public GameObject invisibleBlocker;

    int score;
    public static Rect bottomRect;
    Vector2 buttonStart = new Vector2(-720, 388);
    Vector2 buttonSpaces = new Vector2(360, -90);
    List<ExitData[]> oldExits = new List<ExitData[]>();

    public static RhythmGameManager ins;

    public event Void_Float OnBinguiXFracUpdate;

    private void Awake()
    {
        ins = this;
        exits = null;
        binggui = null;
        reload.onClick.AddListener(delegate
        {
            Timeline.Stop();
            if (binggui != null)
            {
                Destroy(binggui.obj);
                binggui = null;
            }
            SceneManager.LoadScene(0);
        });
    }
    void Start()
    {
        score = 0;
        UIScore.text = "Score: 0";

        RectTransform brt = bottom.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(DefRes.x, BlockSize.y);
        bottomRect = new Rect(brt.anchoredPosition - brt.sizeDelta / 2 - new Vector2(0, BlockSize.y / 2f), brt.sizeDelta + new Vector2(0, BlockSize.y));

        pauseButton.onClick.AddListener(OnPauseButtonPressed);
        pauseButton.gameObject.SetActive(false);

        if (!SelectLevel.ins)
        {
            var info = new DirectoryInfo("Assets/Music/Resources");
            var dirs = info.GetDirectories();
            int i = 0;
            foreach (var d in dirs)
            {
                string songName = d.Name;
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
        else
        {
            Timeline.StartMusicScript(SelectLevel.ins.preselectedSongName);
            Destroy(SelectLevel.ins.gameObject);
            GenerateExits();
            Destroy(selectSong);
        }
    }

    bool pause;
    private void Update()
    {
        timeShown.text = ((int)((Time.time - Timeline.ins.startTime) * 1000)).ToString();
        if (Input.GetKeyDown(KeyCode.Escape)) OnPauseButtonPressed();
    }

    private void FixedUpdate()
    {
        BingguiUpdate();
        oldExits.RemoveAll(RemoveOldExits);
    }

    public void OnPauseButtonPressed()
    {
        pause = !pause;
        pauseButton.image.sprite = pauseButtonSprites[pause ? 1 : 0];
        Timeline.Pause(pause);
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
                // Instantiate(Resources.Load<GameObject>("explosion"), ins.parentNode).transform.position = ed.obj.transform.position;
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
        brt.sizeDelta = new Vector2(DefRes.x, BlockSize.y);
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

    public static bool IsMetaBlock(string blockName)
    {
        HashSet<string> metas = new HashSet<string>()
        {
            "ChangeGameMode",
            "GameOver",
            "PlayAVideo"
        };
        return metas.Contains(blockName);
    }
    public static RhythmObject CreateBlock(int exit, string blockName, Color? c = null, int perfectScore = 20, int goodScore = 10, int badScore = 0, float debugTime = -1)
    {
        string prefabName = blockName;
        if (GeneralSettings.mode > 0 && !IsMetaBlock(blockName)) prefabName += "_" + GeneralSettings.mode;
        var ret = Instantiate(Resources.Load<GameObject>(prefabName), ins.parentNode).GetComponent<RhythmObject>().Initialize(exit, c == null ? Color.white : c.GetValueOrDefault(), perfectScore, goodScore, badScore);
        if (debugTime > 0)
        {
            var text = ret.GetComponentInChildren<Text>();
            if (text)
            {
                text.color = Color.black;
                text.text = debugTime.ToString();
            }
        }
        return ret;
    }

    public static Beat CreateBeat(int exit, Color? c = null, int perfectScore = 20, int goodScore = 10, int badScore = 0)
    {
        var ret = Instantiate(Resources.Load<GameObject>("Beat"), ins.parentNode).GetComponent<Beat>().Initialize(exit, c == null ? Color.white : c.GetValueOrDefault(), perfectScore, goodScore, badScore);
        var pos = (ret.transform as RectTransform).anchoredPosition;
        pos.y = Harp.GetHeight();
        (ret.transform as RectTransform).anchoredPosition = pos;
        return (Beat)ret;
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
            rt.sizeDelta = new Vector2(BlockSize.x, BlockSize.y);
            binggui.center = rt.anchoredPosition = new Vector2(0, GetBottom());
            binggui.x1 = rt.anchoredPosition.x - rt.sizeDelta.x / 2;
            binggui.x2 = rt.anchoredPosition.x + rt.sizeDelta.x / 2;
            //var puff = Instantiate(Resources.Load<GameObject>("explosion"), ins.parentNode);
            //(puff.transform as RectTransform).anchoredPosition = binggui.center;
            ins.OnBinguiXFracUpdate?.Invoke((binggui.center.x + DefRes.x / 2f) / DefRes.x);
        }
        else if (binggui != null)
        {
            Destroy(binggui.obj);
            binggui = null;
        }

        int num = GeneralSettings.exitCount;
        exits = new ExitData[num];
        float step = DefRes.x / num;
        float top = GeneralSettings.mode == 1 ? DefRes.y / 2 + BlockSize.y / 2 : DefRes.y / 2 - BlockSize.y * 1.5f;
        for (int i = 0; i < num; ++i)
        {
            exits[i] = new ExitData();
            exits[i].id = i;
            exits[i].obj = Instantiate(Resources.Load<GameObject>("exit"), ins.parentNode);
            RectTransform rt = exits[i].obj.transform as RectTransform;
            rt.sizeDelta = new Vector2(BlockSize.x, BlockSize.y);
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

            // var puff = Instantiate(Resources.Load<GameObject>("explosion"), ins.parentNode);
            // (puff.transform as RectTransform).anchoredPosition = new Vector2(rt.anchoredPosition.x, top);
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
                    OnBinguiXFracUpdate?.Invoke((binggui.center.x + DefRes.x / 2f) / DefRes.x);
                }
                if (binggui.center.x > pos.x)
                {
                    binggui.center.x = Mathf.Max(binggui.center.x - GeneralSettings.bingguiSpeed * Time.fixedDeltaTime, pos.x);
                    RectTransform rt = binggui.obj.GetComponent<RectTransform>();
                    rt.anchoredPosition = binggui.center;
                    binggui.x1 = rt.anchoredPosition.x - rt.sizeDelta.x / 2;
                    binggui.x2 = rt.anchoredPosition.x + rt.sizeDelta.x / 2;
                    OnBinguiXFracUpdate?.Invoke((binggui.center.x + DefRes.x / 2f) / DefRes.x);
                }
                break;
            }
        }
    }

    public void LoadMainMenu()
    {
        StartCoroutine(WaitForLoadMainMenuFinished());
    }

    IEnumerator WaitForLoadMainMenuFinished()
    {
        Time.timeScale = 1;
        loading.SetActive(true);
        AsyncOperation async = SceneManager.LoadSceneAsync(0);
        do
        {
            yield return new WaitForSecondsRealtime(0.1f);
        }
        while (async == null);
        async.allowSceneActivation = false;
        yield return new WaitForSecondsRealtime(1f);
        loading.SetActive(false);
        async.allowSceneActivation = true;
    }

    public static void HideContent(bool hide = true)
    {
        ins.gameContent.SetActive(!hide);
    }

    public static void HideExit(bool hide = true)
    {
        foreach (ExitData ed in exits) ed.obj.SetActive(!hide);
        if (binggui != null) binggui.obj.SetActive(!hide);
    }

    public static void HideBottomBar(bool hide = true)
    {
        ins.bottom.SetActive(!hide);
    }
}
