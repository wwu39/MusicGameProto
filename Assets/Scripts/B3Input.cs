using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B3Input
{
    public Vector2 screenPos;
    public static List<B3Input> list;
    public static void Gather()
    {
        list = new List<B3Input>();
    }
}
