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
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() + 1.5 * BlockSize.y)
        {
            Activate();
        }
    }

    protected override void Update_Activated()
    {
        if (exits[exit].IsBeingTouched())
        {
            OnClick();
        }

        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() - 1.5f * BlockSize.y)
        {
            Score(0);
            Destroy(gameObject);
        }

    }
    void OnClick()
    {
        float diff = rt.anchoredPosition.y - RhythmGameManager.GetBottom();
        if (diff > 1.5f * BlockSize.y || diff < -1.5f * BlockSize.y)
        {
            Score(0);
            Destroy(gameObject);
        }
        else if (diff > 0.5f * BlockSize.y || diff < -0.5f * BlockSize.y)
        {
            Score(1);
            Destroy(gameObject);
        }
        else
        {
            Score(2);
            Destroy(gameObject);
        }
    }
}
