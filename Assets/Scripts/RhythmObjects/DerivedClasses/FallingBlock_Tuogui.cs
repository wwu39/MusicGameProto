﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingBlock_Tuogui : FallingBlock
{
    public override RhythmType Type => RhythmType.FallingBlock;

    protected override void CheckActivateCondition()
    {
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() + RhythmGameManager.blockHeight)
        {
            Activate();
        }
    }

    protected override void Update_Activated()
    {
        if (TouchedByBinggui())
        {
            Score(2);
            Destroy(gameObject);
        }
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() - 1.5f * RhythmGameManager.blockHeight)
        {
            Score(0);
            Destroy(gameObject);
        }
    }
}
