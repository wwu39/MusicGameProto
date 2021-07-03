using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BGMusicVolume : MonoBehaviour
{
    [SerializeField] Scrollbar bar;
    [SerializeField] Text t;

    void Start()
    {
        bar.value = 1;
        bar.onValueChanged.AddListener(OnValueChanged);
        t.text = "背景音量: 1";
    }

    void OnValidate()
    {
        bar = GetComponentInChildren<Scrollbar>();
        t = GetComponentInChildren<Text>();
    }

    void OnValueChanged(float val)
    {
        Timeline.SetParam("BGVolumn", val);
        t.text = "背景音量" + val;
    }
}
