using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rouxian : RhythmObject
{
    public override RhythmType Type => RhythmType.Rouxian;
    public int width;
    protected override void CheckActivateCondition()
    {
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() + 2.5 * RhythmGameManager.blockHeight)
        {
            Activate();
        }
    }

    protected override void Update_Activated()
    {
        
    }
}
