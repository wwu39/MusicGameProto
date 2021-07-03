using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugMode : MonoBehaviour
{
    static DebugMode ins;
    private void Awake()
    {
        ins = this;
    }
    public static void DestroyAllDebugObjects()
    {
        if (ins) Destroy(ins.gameObject);
    }
}
