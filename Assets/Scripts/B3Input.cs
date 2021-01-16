using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B3Input
{
    Vector2 position;

    public class KeyboardInputSetting
    {
        public KeyCode[] exitTouches = new KeyCode[6]
        {
            KeyCode.Q, KeyCode.A, KeyCode.Z,
            KeyCode.P, KeyCode.Semicolon, KeyCode.Slash,
        };
    }
    public static KeyboardInputSetting keyboard = new KeyboardInputSetting();

    static List<B3Input> list;
    public static int count { get => list.Count; }

    public B3Input(Vector2 canvasPoint)
    {
        position = canvasPoint;
    }
    public static void GatherAllInputs()
    {
        list = new List<B3Input>();

        // 收集触屏输入
        for (int i = 0; i < Input.touchCount; ++i)
            list.Add(new B3Input(Utils.ScreenToCanvasPos(Input.GetTouch(i).position)));

        // 收集键盘输入
        for (int i = 0; i < keyboard.exitTouches.Length; ++i)
            if (Input.GetKey(keyboard.exitTouches[i]))
                list.Add(new B3Input(RhythmGameManager.exits[i].center));

        // 收集手柄输入
        // TODO
    }
    public static Vector2 GetInput(int i)
    {
        return list[i].position;
    }
}
