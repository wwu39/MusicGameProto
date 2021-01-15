using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LongFallingBlock : RhythmObject
{
    public int length = 3;
    [Header("Temp Art")]
    [SerializeField] Image frame;
    [SerializeField] Image bg;
    [SerializeField] Image shadow;

    bool[] checkpoints;
    public override RhythmType Type => RhythmType.FallingBlock;

    protected override void Start()
    {
        base.Start();
        ApplyLength();
        checkpoints = new bool[length];
    }
    int curScore;
    protected override void Update_Activated()
    {
        bool getTouched = GetExit().IsBeingTouched();

        float diff = (rt.anchoredPosition.x - GetBottom()) * (panel == PanelType.Left ? 1 : -1);

        // 判定第一次按键
        if (getTouched && !checkpoints[0])
        {
            if (diff > 1.5f * BlockSize.x)
            {
                print("First touched");
                Score(0);
                curScore = 0;
                checkpoints[0] = true;
            }
            else if (diff > 0.5f * BlockSize.x)
            {
                print("First touched");
                Score(1);
                curScore = 1;
                checkpoints[0] = true;
            }
            else if (diff > -0.5 * BlockSize.x)
            {
                print("First touched");
                Score(2);
                curScore = 2;
                checkpoints[0] = true;
            }
        }
        if (!checkpoints[0] && diff <= -0.6 * BlockSize.x)
        {
            Score(0);
            curScore = 0;
            checkpoints[0] = true;
        }

        // 判定第2~N次按键
        for (int i = 1; i < length; ++i)
        {
            if (!checkpoints[i] && diff > (-i - 0.5f) * BlockSize.x && diff <= (-i + 0.5f) * BlockSize.x)
            {
                if (getTouched)
                {
                    curScore = Mathf.Clamp(curScore + 1, 0, 2);
                    Score(curScore, sndIdx: i);
                }
                else
                {
                    curScore = Mathf.Clamp(curScore - 1, 0, 2);
                    Score(curScore, sndIdx: i);
                }
                checkpoints[i] = true;
            }
        }

        // 移除
        if (diff <= (-length + 0.5f) * BlockSize.x)
        {
            DestroyRhythmObject(this);
        }
    }

    public void ApplyLength()
    {
        var sizeDelta = new Vector2(length * BlockSize.x, BlockSize.y);
        var anchoredPosition = new Vector2((length - 1f) * BlockSize.x / 2, 0) * (panel == PanelType.Left ? 1 : -1);

        // temp art
        frame.rectTransform.sizeDelta = sizeDelta + new Vector2(9, 9);
        bg.rectTransform.sizeDelta = sizeDelta + new Vector2(32, 32);
        shadow.rectTransform.sizeDelta = sizeDelta + new Vector2(32, 32);
        frame.rectTransform.anchoredPosition = bg.rectTransform.anchoredPosition = shadow.rectTransform.anchoredPosition = anchoredPosition;
    }
}
