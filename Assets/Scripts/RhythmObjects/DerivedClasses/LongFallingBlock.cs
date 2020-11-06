using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LongFallingBlock : RhythmObject
{
    public int length = 3;
    [SerializeField] Image longImage;
    bool[] checkpoints;
    public override RhythmType Type => RhythmType.FallingBlock;

    protected override void CheckActivateCondition()
    {
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() + 1.5 * RhythmGameManager.blockHeight)
        {
            Activate();
        }
    }

    protected override void Start()
    {
        base.Start();
        ApplyLength();
        checkpoints = new bool[length];
    }
    int curScore;
    protected override void Update_Activated()
    {
        bool getTouched = false;
        for (int i = 0; i < Input.touchCount; ++i)
        {
            if (exits[exit].IsBeingTouchedBy(Input.GetTouch(i)))
            {
                getTouched = true;
                break;
            }
        }

        float diff = rt.anchoredPosition.y - RhythmGameManager.GetBottom();

        // 判定第一次按键
        if (getTouched && !checkpoints[0])
        {
            if (diff > 1.5f * RhythmGameManager.blockHeight)
            {
                Score(0);
                curScore = 0;
                checkpoints[0] = true;
            }
            else if (diff > 0.5f * RhythmGameManager.blockHeight)
            {
                Score(1);
                curScore = 1;
                checkpoints[0] = true;
            }
            else if (diff > -0.5 * RhythmGameManager.blockHeight)
            {
                Score(2);
                curScore = 2;
                checkpoints[0] = true;
            }
        }
        if (!checkpoints[0] && diff <= -0.6 * RhythmGameManager.blockHeight)
        {
            Score(0);
            curScore = 0;
            checkpoints[0] = true;
        }

        // 判定第2~N次按键
        for (int i = 1; i < length; ++i)
        {
            if (!checkpoints[i] && diff > (-i - 0.5f) * RhythmGameManager.blockHeight && diff <= (-i + 0.5f) * RhythmGameManager.blockHeight)
            {
                if (getTouched)
                {
                    curScore = Mathf.Clamp(curScore + 1, 0, 2);
                    Score(curScore);
                }
                else
                {
                    curScore = Mathf.Clamp(curScore - 1, 0, 2);
                    Score(curScore);
                }
                checkpoints[i] = true;
            }
        }

        // 移除
        if (diff <= (-length + 0.5f) * RhythmGameManager.blockHeight)
        {
            Destroy(gameObject);
        }
    }

    public void ApplyLength()
    {
        if (!longImage) return;
        longImage.rectTransform.sizeDelta = new Vector2(RhythmGameManager.exitWidth, length * RhythmGameManager.blockHeight);
        longImage.rectTransform.anchoredPosition = new Vector2(0, (length - 0.5f) * RhythmGameManager.blockHeight / 2);
    }
}
