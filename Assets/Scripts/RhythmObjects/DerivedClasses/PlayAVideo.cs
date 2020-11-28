using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAVideo : RhythmObject
{
    public int clipNum;
    public int mode;
    public override RhythmType Type => RhythmType.Misc;

    protected override void Start()
    {
        base.Start();
        Timeline.PlayVideo(clipNum, mode);
        Destroy(gameObject);
    }
    protected override void CheckActivateCondition()
    {
        throw new System.NotImplementedException();
    }

    protected override void Update_Activated()
    {
        throw new System.NotImplementedException();
    }
}
