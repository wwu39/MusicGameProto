﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Beat : RhythmObject
{
    public float lifetime = 1f;
    [SerializeField] Image spreading, outter;
    [SerializeField] Sprite[] animSprites;
    Vector2 startSize = new Vector2(40, 40);
    Vector2 endSize = new Vector2(150, 150);
    float startTime;
    public static float radius { get; } = 65;
    public override RhythmType Type => RhythmType.Misc;

    protected override void Start()
    {
        base.Start();
        startTime = Time.time;
    }

    protected override void Update()
    {
        if (Time.time - startTime >= lifetime)
        {
            spreading.rectTransform.sizeDelta = endSize;
            Score(0, (transform as RectTransform).anchoredPosition);
            Destroy(gameObject);
        }
        else
        {
            float frac = (Time.time - startTime) / lifetime;
            spreading.rectTransform.sizeDelta = Vector2.Lerp(startSize, endSize, frac);
            outter.sprite = animSprites[Mathf.RoundToInt(frac * (animSprites.Length - 1))];
            for (int i = 0; i < Input.touchCount; ++i)
            {
                if (IsBeingTouchedBy(Input.GetTouch(i)))
                {
                    Score(2, (transform as RectTransform).anchoredPosition);
                    Destroy(gameObject);
                    break;
                }
            }
        }
    }
    protected override void CheckActivateCondition()
    {

    }

    protected override void Update_Activated()
    {
        
    }
    public bool IsBeingTouchedBy(Touch t)
    {
        var pos = Utils.ScreenToCanvasPos(t.position);
        return Vector2.Distance(pos, rt.anchoredPosition + (parent ? parent.rt.anchoredPosition : Vector2.zero)) < radius;
    }
}