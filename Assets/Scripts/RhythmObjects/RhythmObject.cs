using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public delegate void Void_0Arg();
public enum RhythmType
{
    None,
    FallingBlock, // 点按
    LongFallingBlock, // 长按
    HorizontalMove, // 单次左右滑
    Rouxian
}
public abstract class RhythmObject : MonoBehaviour
{
    [HideInInspector] public int perfectScore = 20;
    [HideInInspector] public int goodScore = 10;
    [HideInInspector] public int badScore = 0;
    [HideInInspector] public int exit;
    protected bool activated;
    protected bool fallBelowBottom = true;
    public float fallingTime;
    protected RectTransform rt;
    float time, altime;
    protected Vector2 start, end;

    public event Void_0Arg OnBottomReached;

    protected virtual void Start()
    {
        rt = transform as RectTransform;
        Vector2 size = rt.sizeDelta;
        size.x = RhythmGameManager.exitWidth;
        size.y = RhythmGameManager.blockHeight;
        rt.sizeDelta = size;
    }
    protected virtual void Update()
    {
        Update_Falling();
        if (!activated)
            CheckActivateCondition();
        else 
            Update_Activated();
    }
    public virtual RhythmObject Initialize(int _exit, Color? c = null, int _perfectScore = 20, int _goodScore = 10, int _badScore = 0)
    {
        if (c != null) foreach (Graphic g in GetComponentsInChildren<Graphic>()) g.color = c.Value;
        exit = _exit;
        perfectScore = _perfectScore;
        goodScore = _goodScore;
        badScore = _badScore;
        transform.position = RhythmGameManager.exits[exit].obj.transform.position;
        end = start = (transform as RectTransform).anchoredPosition;
        end.y = RhythmGameManager.GetBottom();
        return this;
    }
    protected virtual void Activate()
    {
        if (!RhythmGameManager.exits[exit].currentRhythmObject)
        {
            activated = true;
            RhythmGameManager.exits[exit].currentRhythmObject = this;
        }
    }
    public abstract RhythmType Type { get; }

    protected abstract void CheckActivateCondition();
    protected void Update_Falling()
    {
        if (time >= 0.0166666667f)
        {
            bool a = altime < fallingTime;
            altime += time;
            bool b = altime < fallingTime;
            if (a != b) OnBottomReached.Invoke();
            if (!fallBelowBottom) if (altime > fallingTime) altime = fallingTime;
            rt.anchoredPosition = Utils.LerpWithoutClamp(start, end, altime / fallingTime);
            time = 0;
        }
        else time += Time.deltaTime;
    }
    protected abstract void Update_Activated();
    protected void Score(int s, Vector2? pos = null)
    {
        int _score;
        string _text;
        Color _color;
        switch (s)
        {
            default:
                _score = badScore;
                _text = "Miss";
                _color = Color.grey;
                break;
            case 1:
                _score = goodScore;
                _text = "Good";
                _color = Color.green;
                break;
            case 2:
                _score = perfectScore;
                _text = "Perfect";
                _color = Color.yellow;
                break;
        }
        RhythmGameManager.UpdateScore(_score);
        FlyingText.Create(_text, _color, pos == null ? RhythmGameManager.exits[exit].center : pos.Value);
    }
}
