using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public delegate void Void_Float(float f);
public delegate void Void_FloatInt(float f, int i);
public delegate void Void_Int(int i);

public enum NlerpMode
{
    InSine,
    OutSine
}

public enum Platform
{
    Default,
    Mobile
}

public class Utils
{
    public static Dictionary<int, string> noteToFile = new Dictionary<int, string>()
    {
        {45,"a3"},{46,"a-3"},{57,"a4"},{58,"a-4"},{69,"a5"},{70,"a-5"},{47,"b3"},{59,"b4"},{71,"b5"},{48,"c3"},
        {49,"c-3"},{60,"c4"},{61,"c-4"},{72,"c5"},{73,"c-5"},{84,"c6"},{50,"d3"},{51,"d-3"},{62,"d4"},{63,"d-4"},
        {74,"d5"},{75,"d-5"},{52,"e3"},{64,"e4"},{76,"e5"},{53,"f3"},{54,"f-3"},{65,"f4"},{66,"f-4"},{77,"f5"},
        {78,"f-5"},{55,"g3"},{56,"g-3"},{67,"g4"},{68,"g-4"},{79,"g5"},{80,"g-5"},

        // 新添的
        {82,"82" },{85,"85"},{87,"87"}
    };
    public static Color[] colorList = new Color[]
    {
        new Color(1,0,0),
        new Color(1,1,0),
        new Color(0,1,0),
        //new Color(0,1,1),
        new Color(0,0,1),
        new Color(1,0,1),
    };
    public static Color GetRandomColor()
    {
        return colorList[Random.Range(0, colorList.Length)];
    }
    public static float Pow(float b, int p)
    {
        float result = 1;
        for (int i = 0; i < p; ++i) result *= b;
        return result;
    }
    public static Vector2 ScreenToCanvasPos(Vector2 pos)
    {
        float frac_x = pos.x / Screen.width;
        float frac_y = pos.y / Screen.height;
        float x = ScreenAdjust.canvasRes.x * (frac_x - 0.5f);
        float y = ScreenAdjust.canvasRes.y * (frac_y - 0.5f);
        return new Vector2(x, y);
    }

    public static Vector2 LerpWithoutClamp(Vector2 a, Vector2 b, float frac)
    {
        return a + (b - a) * frac;
    }

    public static Vector2 NLerp(Vector2 a, Vector2 b, float frac, NlerpMode curve)
    {
        if (frac <= 0) return a; else if (frac >= 1) return b;
        if (curve == NlerpMode.InSine) return a + (b - a) * Mathf.Sin(frac * Mathf.PI / 2);
        else if (curve == NlerpMode.OutSine) return a + (b - a) * (1 - Mathf.Sin((1 - frac) * Mathf.PI / 2));
        return a + (b - a) * frac;
    }
}
