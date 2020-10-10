using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class DefRes // 开发用分辨率
{
    public static int x { private set; get; } = 1920;
    public static int y { private set; get; } = 1080;
}

public class ExitData
{
    public int id;
    public GameObject obj;
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
}

public struct GeneralSettings
{
    public static int exitCount;
}

public class RhythmGameManager : MonoBehaviour
{
    public string songName;
    [SerializeField] Transform parentNode;
    public static ExitData[] exits;
    [SerializeField] GameObject bottom;
    [SerializeField] Text UIScore;
    public static float blockHeight { private set; get; } = 90; // 方块高度
    public static float exitWidth { private set; get; } = 180; // 出口/方块横向长度

    int score;
    float time;
    public static Rect bottomRect;

    public static RhythmGameManager ins;

    private void Awake()
    {
        ins = this;
    }
    void Start()
    {
        score = 0;
        UIScore.text = "Score: 0";

        RectTransform brt = bottom.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(DefRes.x, blockHeight);
        bottomRect = new Rect(brt.anchoredPosition - brt.sizeDelta / 2, brt.sizeDelta);


        Timeline.StartMusicScript(songName);
        GenerateExits();
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
        return Instantiate(Resources.Load<GameObject>(blockName), ins.parentNode).GetComponent<RhythmObject>().Initialize(exit, c == null ? Color.white : c.GetValueOrDefault(), perfectScore, goodScore, badScore);
    }

    void GenerateExits()
    {
        int num = GeneralSettings.exitCount;
        exits = new ExitData[num];
        float step = DefRes.x / num;
        for (int i = 0; i < num; ++i)
        {
            exits[i] = new ExitData();
            exits[i].id = i;
            exits[i].obj = Instantiate(Resources.Load<GameObject>("exit"), parentNode);
            RectTransform rt = exits[i].obj.transform as RectTransform;
            rt.sizeDelta = new Vector2(exitWidth, blockHeight);
            rt.anchoredPosition = new Vector2(step * (i + 0.5f) - DefRes.x / 2f, 450);
            exits[i].x1 = rt.anchoredPosition.x - rt.sizeDelta.x / 2;
            exits[i].x2 = rt.anchoredPosition.x + rt.sizeDelta.x / 2;
            exits[i].center = new Vector2(rt.anchoredPosition.x, GetBottom());

            GameObject indicator = Instantiate(exits[i].obj, parentNode);
            indicator.GetComponentInChildren<Graphic>().color = Color.black;
            rt = indicator.transform as RectTransform;
            Vector2 pos = rt.anchoredPosition;
            pos.y = GetBottom();
            rt.anchoredPosition = pos;
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
