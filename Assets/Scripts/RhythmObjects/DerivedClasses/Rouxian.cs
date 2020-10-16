using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Rouxian : RhythmObject
{
    [Header("Properties")]
    public int width;
    public float timeLast;
    [Header("Graphics")]
    [SerializeField] Image[] deepnessZones;
    [SerializeField] Image cord;
    public override RhythmType Type => RhythmType.Rouxian;

    protected override void Start()
    {
        base.Start();
        GraphicSetup();
        fallBelowBottom = false;
        OnBottomReached += delegate { StartCoroutine(Interacting()); };
    }

    public override RhythmObject Initialize(int _exit, Color? c = null, int _perfectScore = 20, int _goodScore = 10, int _badScore = 0)
    {
        return base.Initialize(_exit, null, _perfectScore, _goodScore, _badScore);
    }
    protected override void CheckActivateCondition()
    {
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() + 1.5 * RhythmGameManager.blockHeight)
        {
            Activate();
        }
    }

    protected override void Update_Activated()
    {
        bool getTouched = false;
        Vector2 touchPos;
        for (int i = 0; i < Input.touchCount; ++i)
        {
            if (RhythmGameManager.exits[exit].IsBeingTouchedBy(Input.GetTouch(i), out touchPos))
            {
                getTouched = true;
                break;
            }
        }

        float diff = rt.anchoredPosition.y - RhythmGameManager.GetBottom();

        if (diff < -2f * RhythmGameManager.blockHeight)
        {
            Destroy(gameObject);
        }
    }

    void GraphicSetup()
    {
        float maxWidth = RhythmGameManager.exits[exit + width - 1].x2 - RhythmGameManager.exits[exit].x1;
        float centerX = maxWidth / 2 - RhythmGameManager.exitWidth / 2;
        float[] deepness = new float[3] { 0.2f, 0.5f, 1f };
        for (int i = 0; i < 3; ++i)
        {
            deepnessZones[i].rectTransform.sizeDelta = new Vector2(maxWidth * deepness[i], RhythmGameManager.blockHeight);
            deepnessZones[i].rectTransform.anchoredPosition = new Vector2(centerX, 0);
        }
        cord.rectTransform.sizeDelta = new Vector2(4, RhythmGameManager.blockHeight);
        cord.rectTransform.anchoredPosition = new Vector2(centerX, 0);
    }

    IEnumerator Interacting()
    {
        print("reached bottom");
        yield return new WaitForSeconds(timeLast);
        print("time " + timeLast + " passed");
        fallBelowBottom = true;
    }
}
