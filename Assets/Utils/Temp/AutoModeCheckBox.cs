using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoModeCheckBox : MonoBehaviour
{
    [SerializeField] Toggle checkBox;
    void Start()
    {
        checkBox.onValueChanged.AddListener(OnValueChanged);
        checkBox.isOn = true;
    }

    private void OnValidate()
    {
        checkBox = GetComponent<Toggle>();
    }

    void OnValueChanged(bool val)
    {
        RhythmGameManager.ins.autoMode = val;
        if (val && RhythmGameManager.exits != null)
        {
            for (int i = 0; i < RhythmGameManager.exits.Length; ++i)
            {
                RhythmGameManager.exits[i].current = null;
            }
        }
    }
}
