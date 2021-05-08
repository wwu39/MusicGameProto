using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MidiOperation : MonoBehaviour
{
    [SerializeField] Dropdown selection;
    [SerializeField] GameObject[] pages;
    [SerializeField] Button start;
    [SerializeField] Dropdown panelDP;
    [Header("FallingBlock")]
    [SerializeField] Dropdown order;
    [SerializeField] Dropdown ascending;
    // Start is called before the first frame update
    void Start()
    {
        SelectPage(selection.value);
        selection.onValueChanged.AddListener(SelectPage);
        start.onClick.AddListener(StartConvertion);
    }
    private void OnEnable()
    {
        start.interactable = true;
        start.GetComponentInChildren<Text>().text = "开始转换";
    }
    void SelectPage(int idx)
    {
        for (int i = 0; i < pages.Length; ++i) pages[i].SetActive(i == idx);
    }
    void StartConvertion()
    {
        // TODO
        start.interactable = false;
        start.GetComponentInChildren<Text>().text = MidiPage.ins.selected.Count + "个音符已转换";
    }
}
