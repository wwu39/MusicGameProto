using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VirtualTouchCheckBox : MonoBehaviour
{
    [SerializeField] Toggle checkBox;
    // Start is called before the first frame update
    private void Awake()
    {
        checkBox.interactable = false;
    }
    void Start()
    {
        checkBox.onValueChanged.AddListener(OnValueChanged);
        checkBox.isOn = false;
    }

    private void OnValidate()
    {
        checkBox = GetComponent<Toggle>();
    }

    void OnValueChanged(bool val)
    {
        
    }
}
