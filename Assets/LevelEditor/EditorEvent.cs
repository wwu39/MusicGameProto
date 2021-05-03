using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum EventType
{
    Note, Meta
}
public class EditorEvent : MonoBehaviour
{
    [SerializeField] Button btn;
    public EventType eventType;
    public KeyData kd;
    private void OnValidate()
    {
        btn = GetComponent<Button>();
    }
    // Start is called before the first frame update
    void Start()
    {
        btn.onClick.AddListener(OnClicked);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnClicked()
    {
        // add select
    }
}
