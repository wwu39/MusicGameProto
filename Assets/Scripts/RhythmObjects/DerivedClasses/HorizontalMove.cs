using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum Direction
{
    Left,
    Right
}
public class HorizontalMove : RhythmObject
{
    [Header("Properties")]
    public int width;
    public Direction direction = Direction.Right;
    [Header("Graphics")]
    [SerializeField] Image block;
    [SerializeField] Image line;
    [Header("UI 1")]
    [SerializeField] bool enableUI1;
    [SerializeField] Image outterFrame;
    [SerializeField] Image outterBg;
    bool[] checkpoints;

    public override RhythmType Type => RhythmType.HorizontalMove;

    protected override void Start()
    {
        base.Start();
        ApplyWidth();
        checkpoints = new bool[width];
    }
    public override RhythmObject Initialize(int _exit, Color? c = null, int _perfectScore = 20, int _goodScore = 10, int _badScore = 0)
    {
        var ret = base.Initialize(_exit, c, _perfectScore, _goodScore, _badScore);
        line.color = c != null ? c.Value / 2 : Color.grey;
        return ret;
    }
    protected override void CheckActivateCondition()
    {
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() + 1.5 * BlockSize.y)
        {
            Activate();
        }
    }

    float notTouchedTimeCount;
    protected override void Update_Activated()
    {
        bool getTouched = false;
        for (int i = 0; i < width; ++i)
        {
            int curExit = direction == Direction.Right ? exit + i : exit - i;
            if (exits[curExit].IsBeingTouched())
            {
                getTouched = true;
                break;
            }
        }

        int curScore = 2; // 0 = bad, 1 = good, 2 = perfect
        float diff = rt.anchoredPosition.y - RhythmGameManager.GetBottom();

        // 累计玩家不触碰的时间
        if (!fallBelowBottom && !getTouched) notTouchedTimeCount += Time.deltaTime;
        else notTouchedTimeCount = 0;

        // 如果玩家不触碰的时间超过0.1秒就会把剩下的全判为miss
        if (!fallBelowBottom && notTouchedTimeCount > 0.1f)
        {
            fallBelowBottom = true;
            for (int i = 1; i < checkpoints.Length; ++i)
            {
                if (!checkpoints[i])
                {
                    Score(0, exits[direction == Direction.Right ? exit + i : exit - i].center);
                    checkpoints[i] = true;
                }
            }
        }

        // 初次接触判定
        if (getTouched && !checkpoints[0])
        {
            if (diff > 1.5f * BlockSize.y || diff < -1.5f * BlockSize.y)
            {
                Score(0);
                curScore = 0;
                checkpoints[0] = true;
                fallBelowBottom = false;
            }
            else if (diff > 0.5f * BlockSize.y || diff < -0.5f * BlockSize.y)
            {
                Score(1);
                curScore = 1;
                checkpoints[0] = true;
                fallBelowBottom = false;
            }
            else
            {
                Score(2);
                curScore = 2;
                checkpoints[0] = true;
                fallBelowBottom = false;
            }
        }

        // 判定第2~N次按键
        if (!fallBelowBottom)
        {
            for (int i = 1; i < width; ++i)
            {
                if (!checkpoints[i])
                {
                    int curExit = direction == Direction.Right ? exit + i : exit - i;
                    if (exits[curExit].IsBeingTouched())
                    {
                        curScore = Mathf.Clamp(curScore + 1, 0, 2);
                        Score(curScore, exits[curExit].center, false, i);
                        checkpoints[i] = true;
                        block.rectTransform.anchoredPosition = new Vector2(exits[curExit].center.x - rt.anchoredPosition.x, 0);
                    }
                }
            }
        }

        bool finished = true;
        foreach (bool b in checkpoints) if (!b) { finished = false; break; }
        if (finished) Deactivate();

        if (diff < -2f * BlockSize.y)
        {
            for (int i = 0; i < checkpoints.Length; ++i)
            {
                if (!checkpoints[i])
                {
                    Score(0, exits[direction == Direction.Right ? exit + i : exit - i].center);
                }
            }
            DestroyRhythmObject(this);
        }
    }

    protected override void Activate()
    {
        if (!exits[exit].current && !activated)
        {
            activated = true;
            for (int i = 0; i < width; ++i) exits[direction == Direction.Right ? exit + i : exit - i].current = this;
        }
    }

    void ApplyWidth()
    {
        switch (direction)
        {
            case Direction.Right:
                line.rectTransform.anchoredPosition = new Vector2((exits[exit + width - 1].x2 - exits[exit].x1) / 2 - BlockSize.x / 2, 0);
                line.rectTransform.sizeDelta = new Vector2(exits[exit + width - 1].x2 - exits[exit].x1, BlockSize.y);
                break;
            case Direction.Left:
                line.rectTransform.anchoredPosition = new Vector2((exits[exit - width + 1].x1 - exits[exit].x2) / 2 + BlockSize.x / 2, 0);
                line.rectTransform.sizeDelta = new Vector2(exits[exit].x2 - exits[exit - width + 1].x1, BlockSize.y);
                break;
        }
        if (enableUI1)
        {
            line.enabled = false;
            outterFrame.rectTransform.sizeDelta = line.rectTransform.sizeDelta + new Vector2(25, 25);
            outterBg.rectTransform.sizeDelta = line.rectTransform.sizeDelta + new Vector2(67, 67);
        }
        else
        {
            Destroy(outterFrame.gameObject);
            Destroy(outterBg.gameObject);
        }
    }
}
