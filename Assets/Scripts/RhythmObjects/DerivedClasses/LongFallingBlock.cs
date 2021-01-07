using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LongFallingBlock : RhythmObject
{
    public int length = 3;
    [SerializeField] Image longImage;
    [Header("UI 1")]
    [SerializeField] bool enableUI1;
    [SerializeField] Image frame;
    [SerializeField] Image bg;
    [SerializeField] Image shadow;

    bool[] checkpoints;
    public override RhythmType Type => RhythmType.FallingBlock;

    protected override void CheckActivateCondition()
    {
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() + 1.5 * BlockSize.y)
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
        bool getTouched = exits[exit].IsBeingTouched();

        float diff = rt.anchoredPosition.y - RhythmGameManager.GetBottom();

        // 判定第一次按键
        if (getTouched && !checkpoints[0])
        {
            if (diff > 1.5f * BlockSize.y)
            {
                Score(0);
                curScore = 0;
                checkpoints[0] = true;
            }
            else if (diff > 0.5f * BlockSize.y)
            {
                Score(1);
                curScore = 1;
                checkpoints[0] = true;
            }
            else if (diff > -0.5 * BlockSize.y)
            {
                Score(2);
                curScore = 2;
                checkpoints[0] = true;
            }
        }
        if (!checkpoints[0] && diff <= -0.6 * BlockSize.y)
        {
            Score(0);
            curScore = 0;
            checkpoints[0] = true;
        }

        // 判定第2~N次按键
        for (int i = 1; i < length; ++i)
        {
            if (!checkpoints[i] && diff > (-i - 0.5f) * BlockSize.y && diff <= (-i + 0.5f) * BlockSize.y)
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
        if (diff <= (-length + 0.5f) * BlockSize.y)
        {
            DestroyRhythmObject(this);
        }
    }

    public void ApplyLength()
    {
        if (!longImage) return;
        longImage.rectTransform.sizeDelta = new Vector2(BlockSize.x, length * BlockSize.y);
        longImage.rectTransform.anchoredPosition = new Vector2(0, (length - 1) * BlockSize.y / 2);
        if (enableUI1)
        {
            longImage.enabled = false;
            frame.rectTransform.sizeDelta = longImage.rectTransform.sizeDelta + new Vector2(25, 25);
            bg.rectTransform.sizeDelta = longImage.rectTransform.sizeDelta + new Vector2(67, 67);
            shadow.rectTransform.sizeDelta = longImage.rectTransform.sizeDelta + new Vector2(67, 67);
        }
        else
        {
            Destroy(frame.gameObject);
            Destroy(bg.gameObject);
            Destroy(shadow.gameObject);
        }
    }
}
