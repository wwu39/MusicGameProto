using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bottom : MonoBehaviour
{
    [SerializeField] Graphic[] coloringParts;
    [SerializeField] float colorTime = 0.5f;
    Color color;
    float time;
    bool animating;

    // 底边触碰变色
    void Update()
    {
        if (animating)
        {
            time += Time.deltaTime;
            if (time >= colorTime)
            {
                foreach (Graphic g in coloringParts) g.color = Color.black;
                time = 0;
                animating = false;
            }
            else foreach (Graphic g in coloringParts) g.color = color;
        }
    }
    public static void SetColor(PanelType panel, Color c)
    {
        // i=0 is left, i=1 is right
        var bottom = panel == PanelType.Left ? Panel.Left.bottom : Panel.Right.bottom;
        bottom.color = c;
        bottom.animating = true;
        bottom.time = 0;
    }
}
