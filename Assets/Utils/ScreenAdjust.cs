using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenAdjust : MonoBehaviour
{
    [SerializeField] CanvasScaler cs;
    private void Awake()
    {
        cs = GetComponent<CanvasScaler>();
        float curSrceenRatio = (float) Screen.width / Screen.height;
        float defaultSrceenRatio = (float)DefRes.x / DefRes.y;
        cs.matchWidthOrHeight = curSrceenRatio > defaultSrceenRatio ? 1 : 0;
    }
}
