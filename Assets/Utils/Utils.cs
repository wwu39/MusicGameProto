using System.Runtime.InteropServices;
using UnityEngine;

public class Utils
{
    static Vector2 defRes = new Vector2(1920, 1080);
    public static Color[] colorList = new Color[6]
    {
        new Color(1,0,0),
        new Color(1,1,0),
        new Color(0,1,0),
        new Color(0,1,1),
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
        float x = defRes.x * frac_x - defRes.x / 2;
        float y = defRes.y * frac_y - defRes.y / 2;
        return new Vector2(x, y);
    }

    public static Vector2 LerpWithoutClamp(Vector2 a, Vector2 b, float frac)
    {
        return a + (b - a) * frac;
    }
}
