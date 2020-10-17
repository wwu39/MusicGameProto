using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeGameMode : RhythmObject
{
    public int newExitCount;
    public int newGameMode;
    public override RhythmType Type => RhythmType.ChangeGameMode;

    protected override void Start()
    {
        base.Start();
        if (newExitCount > 0) GeneralSettings.exitCount = newExitCount;
        if (newGameMode >= 0) GeneralSettings.mode = newGameMode;
        print("new game mode: " + GeneralSettings.mode);
        RhythmGameManager.GenerateExits();
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
