using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOver : RhythmObject
{
    [SerializeField] GameObject gameoverScreen;
    public override RhythmType Type => RhythmType.Misc;

    protected override void Start()
    {
        base.Start();
        print("GameOver");
        RhythmGameManager.HideContent();
        Instantiate(gameoverScreen, GameObject.Find("Canvas").transform);
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
