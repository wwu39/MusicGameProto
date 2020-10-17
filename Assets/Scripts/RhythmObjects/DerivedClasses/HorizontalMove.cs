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
    [SerializeField] Image line;
    [SerializeField] Image arrow;
    bool[] checkpoints;

    public override RhythmType Type => RhythmType.HorizontalMove;

    protected override void Start()
    {
        base.Start();
        ApplyWidth();
        checkpoints = new bool[width];
    }
    protected override void CheckActivateCondition()
    {
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() + 2.5 * RhythmGameManager.blockHeight)
        {
            Activate();
        }
    }

    float notTouchedTimeCount;
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
        int curScore = 2; // 0 = bad, 1 = good, 2 = perfect
        float diff = rt.anchoredPosition.y - RhythmGameManager.GetBottom();

        if (!fallBelowBottom && !getTouched)
        {
            notTouchedTimeCount += Time.deltaTime;
        }
        else
        {
            notTouchedTimeCount = 0;
        }

        if (!fallBelowBottom && notTouchedTimeCount > 0.5f)
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
            if (diff > 1.5f * RhythmGameManager.blockHeight || diff < -1.5f * RhythmGameManager.blockHeight)
            {
                Score(0);
                curScore = 0;
                checkpoints[0] = true;
                fallBelowBottom = false;
            }
            else if (diff > 0.5f * RhythmGameManager.blockHeight || diff < -0.5f * RhythmGameManager.blockHeight)
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
                    for (int j = 0; j < Input.touchCount; ++j)
                    {
                        int curExit = direction == Direction.Right ? exit + i : exit - i;
                        if (exits[curExit].IsBeingTouchedBy(Input.GetTouch(j)))
                        {
                            curScore = Mathf.Clamp(curScore + 1, 0, 2);
                            Score(curScore, exits[curExit].center);
                            checkpoints[i] = true;
                            break;
                        }
                    }
                }
            }
        }

        if (diff < -2f * RhythmGameManager.blockHeight)
        {
            for (int i = 0; i < checkpoints.Length; ++i)
            {
                if (!checkpoints[i])
                {
                    Score(0, exits[direction == Direction.Right ? exit + i : exit - i].center);
                }
            }
            Destroy(gameObject);
        }
    }

    void ApplyWidth()
    {
        Vector2 size;
        switch (direction)
        {
            case Direction.Right:
                arrow.rectTransform.anchoredPosition = new Vector2(exits[exit + width - 1].x2 - exits[exit].x2 + RhythmGameManager.exitWidth / 2, 0);
                line.rectTransform.anchoredPosition = new Vector2((exits[exit + width - 1].x2 - exits[exit].x2) / 2 + RhythmGameManager.exitWidth / 2, 0);
                size = line.rectTransform.sizeDelta;
                size.x = exits[exit + width - 1].x2 - exits[exit].x2;
                line.rectTransform.sizeDelta = size;
                break;
            case Direction.Left:
                arrow.rectTransform.anchoredPosition = new Vector2(exits[exit - width + 1].x1 - exits[exit].x1 - RhythmGameManager.exitWidth / 2, 0);
                line.rectTransform.anchoredPosition = new Vector2((exits[exit - width + 1].x1 - exits[exit].x1) / 2 - RhythmGameManager.exitWidth / 2, 0);
                size = line.rectTransform.sizeDelta;
                size.x = exits[exit].x1 - exits[exit - width + 1].x1;
                line.rectTransform.sizeDelta = size;
                break;
        }
    }
}
