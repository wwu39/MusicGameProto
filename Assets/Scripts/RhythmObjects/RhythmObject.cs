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
    Rouxian,
    ChangeGameMode
}
public abstract class RhythmObject : MonoBehaviour
{
    [HideInInspector] public int perfectScore = 20;
    [HideInInspector] public int goodScore = 10;
    [HideInInspector] public int badScore = 0;
    [HideInInspector] public int exit;
    protected ExitData[] exits;
    protected bool activated;
    protected bool fallBelowBottom = true;
    public float fallingTime = 3;
    protected RectTransform rt;
    float time, altime;
    protected Vector2 start, end;

    public event Void_0Arg OnBottomReached;
    public event Void_Float OnFallingFracUpdated;
    public event Void_Int OnScored;

    float createTime;
    protected virtual void Start()
    {
        rt = transform as RectTransform;
        Vector2 size = rt.sizeDelta;
        size.x = RhythmGameManager.exitWidth;
        size.y = RhythmGameManager.blockHeight;
        rt.sizeDelta = size;

        createTime = Time.time;
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
        exits = RhythmGameManager.exits;
        perfectScore = _perfectScore;
        goodScore = _goodScore;
        badScore = _badScore;
        transform.position = exits[exit].obj.transform.position;
        end = start = (transform as RectTransform).anchoredPosition;
        end.y = RhythmGameManager.GetBottom();
        return this;
    }
    protected virtual void Activate()
    {
        if (!exits[exit].currentRhythmObject && !activated)
        {
            activated = true;
            exits[exit].currentRhythmObject = this;
        }
    }
    public abstract RhythmType Type { get; }

    protected abstract void CheckActivateCondition();
    protected void Update_Falling()
    {
        time += Time.deltaTime;
        if (time >= 0.0166666667f)
        {
            bool a = altime < fallingTime;
            altime += time;
            bool b = altime < fallingTime;
            if (a != b) OnBottomReached?.Invoke();
            if (!fallBelowBottom) if (altime > fallingTime) altime = fallingTime;
            float frac = altime / fallingTime;
            rt.anchoredPosition = Utils.LerpWithoutClamp(start, end, frac);
            OnFallingFracUpdated?.Invoke(frac);
            time = 0;
        }
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
        FlyingText.Create(_text, _color, pos == null ? exits[exit].center : pos.Value);
        OnScored?.Invoke(s);
    }

    protected bool TouchedByBinggui()
    {
        if (RhythmGameManager.binggui == null) return false;
        var pos = rt.anchoredPosition;
        return pos.x > RhythmGameManager.binggui.x1 && pos.x < RhythmGameManager.binggui.x2 && Mathf.Abs(pos.y - RhythmGameManager.binggui.center.y) <= RhythmGameManager.blockHeight;
    }
}
