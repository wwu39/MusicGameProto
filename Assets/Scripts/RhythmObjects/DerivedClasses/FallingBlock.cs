using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class FallingBlock : RhythmObject
{

    public override RhythmType Type => RhythmType.FallingBlock;

    protected override void CheckActivateCondition()
    {
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() + 2.5 * RhythmGameManager.blockHeight)
        {
            Activate();
        }
    }

    protected override void Update_Activated()
    {
        for (int i = 0; i < Input.touchCount; ++i)
        {
            if (exits[exit].IsBeingTouchedBy(Input.GetTouch(i)))
            {
                OnClick();
                break;
            }
        }

        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() - 1.5f * RhythmGameManager.blockHeight)
        {
            RhythmGameManager.UpdateScore(badScore);
            FlyingText.Create("Miss", Color.grey, exits[exit].center);
            Destroy(gameObject);
        }

    }
    void OnClick()
    {
        float diff = rt.anchoredPosition.y - RhythmGameManager.GetBottom();
        if (diff > 1.5f * RhythmGameManager.blockHeight || diff < -1.5f * RhythmGameManager.blockHeight)
        {
            RhythmGameManager.UpdateScore(badScore);
            FlyingText.Create("Miss", Color.grey, rt.anchoredPosition);
            Destroy(gameObject);
        }
        else if (diff > 0.5f * RhythmGameManager.blockHeight || diff < -0.5f * RhythmGameManager.blockHeight)
        {
            RhythmGameManager.UpdateScore(goodScore);
            FlyingText.Create("Good", Color.green, rt.anchoredPosition);
            Destroy(gameObject);
        }
        else
        {
            RhythmGameManager.UpdateScore(perfectScore);
            FlyingText.Create("Perfect", Color.yellow, rt.anchoredPosition);
            Destroy(gameObject);
        }
    }
}
