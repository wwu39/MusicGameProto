using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MLE = MusicalLevelEditor;

public class SelectPad : MonoBehaviour
{
    [SerializeField] InputField startTimeIF;
    [SerializeField] InputField endTimeIF;
    [SerializeField] Button selectButton;
    public EditingPage page;
    Image selectionBox;
    MLE mle;
    bool canClick;
    float startTime, endTime;
    void Start()
    {
        mle = MLE.ins;
        startTimeIF.onValueChanged.AddListener(OnValueChanged);
        endTimeIF.onValueChanged.AddListener(OnValueChanged);
        selectButton.onClick.AddListener(OnSelectButtonClicked);
    }

    void OnValueChanged(string val)
    {
        if (!float.TryParse(startTimeIF.text, out startTime) || !float.TryParse(endTimeIF.text, out endTime) || startTime >= endTime)
        {
            if (selectionBox) Destroy(selectionBox.gameObject);
            canClick = false;
            return;
        }
        canClick = true;
        if (!selectionBox)
        {
            selectionBox = Instantiate(mle.barImage, mle.contents[0]).GetComponent<Image>();
            selectionBox.color = new Color32(73, 174, 190, 60);
            selectionBox.name = "SelectBox";
            selectionBox.raycastTarget = false;
        }
        selectionBox.rectTransform.anchoredPosition = new Vector2(mle.TimeToPagePosX(page, (startTime + endTime) / 2), 0);
        selectionBox.rectTransform.sizeDelta = new Vector2(MLE.lengthPerSec_midiPage * (endTime - startTime), mle.MidiContentHeight);
    }
    public void OnSelectButtonClicked()
    {
        if (!canClick) return;
        MLE.SelectRange(startTime, endTime);
    }

    public void SetRange(float t1, float t2)
    {
        if (t1 > t2)
        {
            startTimeIF.text = t2.ToString();
            endTimeIF.text = t1.ToString();
        }
        else if (t1 < t2)
        {
            startTimeIF.text = t1.ToString();
            endTimeIF.text = t2.ToString();
        }
    }
    public bool Interactable
    {
        get => selectButton.interactable;
        set
        {
            selectButton.interactable = startTimeIF.interactable = endTimeIF.interactable = value;
        }
    }
}
