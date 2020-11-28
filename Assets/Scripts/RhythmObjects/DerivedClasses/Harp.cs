using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Harp : RhythmObject
{
    public int width;
    public float timeLast;
    [SerializeField] Image harpImage;
    [SerializeField] Image harpImage2;
    [Header("UI 1")]
    [SerializeField] bool enableUI1;
    [SerializeField] Image outterFrame;
    [SerializeField] Image outterBg;
    [SerializeField] Image outterShadow;
    public override RhythmType Type => RhythmType.Rouxian;

    static Harp ins;
    static int i;
    static int[] heights = new int[6] { 325, 195, 65, -65, -195, -325 };
    public static int GetHeight() { i = (i + 1) % 6; return heights[i]; }

    float animTime = 0.5f;
    float startTime = 0;
    bool harpImageIn = false;
    bool harpImageOut = false;
    float harpImageStartHeight = 1;
    float harpImageEndHeight = DefRes.y - BlockSize.y * 4;
    float harpImageOverHeight = DefRes.y - BlockSize.y * 3;
    bool interacting = false;

    protected override void Start()
    {
        if (ins) Destroy(gameObject); else ins = this;
        base.Start();
        ApplyWidth();
        var pos = rt.anchoredPosition;
        pos.y = RhythmGameManager.GetBottom();
        rt.anchoredPosition = pos;
        harpImage2.enabled = harpImage.enabled = true;
        harpImage2.rectTransform.sizeDelta = harpImage.rectTransform.sizeDelta = new Vector2(outterFrame.rectTransform.sizeDelta.x, harpImageStartHeight);
        harpImage2.rectTransform.anchoredPosition = harpImage.rectTransform.anchoredPosition = new Vector2(outterFrame.rectTransform.anchoredPosition.x, BlockSize.y / 2f);
        harpImageIn = true;
        startTime = Time.time;
    }

    protected override void Update()
    {
        float diff = rt.anchoredPosition.y - RhythmGameManager.GetBottom();
        if (diff < -2f * BlockSize.y) Destroy(gameObject);
        if (harpImageIn)
        {
            float frac = (Time.time - startTime) / animTime;
            if (frac >= 1)
            {
                harpImageIn = false;
                frac = 1;
                StartCoroutine(Interacting());
            }
            float y = frac <= 0.8 ? (harpImageStartHeight + frac * 1.25f * (harpImageOverHeight - harpImageStartHeight)) : (harpImageOverHeight + (frac - 0.8f) * 5 * (harpImageEndHeight - harpImageOverHeight));
            harpImage2.rectTransform.sizeDelta = harpImage.rectTransform.sizeDelta = new Vector2(outterFrame.rectTransform.sizeDelta.x, y);
        }
        if (harpImageOut)
        {
            float frac = (Time.time - startTime) / animTime;
            if (frac >= 1)
            {
                harpImageOut = false;
                frac = 1;
                harpImage2.enabled = harpImage.enabled = false;
                Destroy(gameObject);
            }
            harpImage2.rectTransform.sizeDelta = harpImage.rectTransform.sizeDelta = new Vector2(outterFrame.rectTransform.sizeDelta.x, harpImageEndHeight + frac * (harpImageStartHeight - harpImageEndHeight));
        }
    }
    
    void ApplyWidth()
    {
        Vector2 pos = new Vector2((exits[exit + width - 1].x2 - exits[exit].x1) / 2 - BlockSize.x / 2, 0);
        outterFrame.rectTransform.anchoredPosition = pos;
        outterBg.rectTransform.anchoredPosition = pos;
        outterShadow.rectTransform.anchoredPosition = pos;

        Vector2 size = new Vector2(exits[exit + width - 1].x2 - exits[exit].x1, BlockSize.y);
        outterFrame.rectTransform.sizeDelta = size + new Vector2(25, 25);
        outterBg.rectTransform.sizeDelta = size + new Vector2(67, 67);
        outterShadow.rectTransform.sizeDelta = size + new Vector2(67, 67);
    }
    IEnumerator Interacting()
    {
        print("reached bottom");
        interacting = true;
        yield return new WaitForSeconds(timeLast - 1);
        print("time " + timeLast + " passed");
        harpImageOut = true;
        startTime = Time.time;
        interacting = false;
    }

    public static bool Contains(KeyData keyData)
    {
        if (!ins) return false;
        if (!ins.interacting) return false;
        int exit = int.Parse(keyData.prop["Exit"]);
        return exit >= ins.exit && exit < ins.exit + ins.width;
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
