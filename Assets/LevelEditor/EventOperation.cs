using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventOperation : MonoBehaviour
{
    [SerializeField] GameObject[] pages;
    [SerializeField] Dropdown eventTypeDP;
    [SerializeField] InputField startTimeIF;
    [SerializeField] Button delete;
    EditorEvent currentEvent;
    Dictionary<string, int> eventTypeNameToIndex;
    float offset;
    private void Awake()
    {
        eventTypeDP.ClearOptions();
        eventTypeNameToIndex = new Dictionary<string, int>();
        for (int i = 0; i < LevelPage.chineseMetaEvent.Length; ++i)
        {
            eventTypeDP.options.Add(new Dropdown.OptionData(LevelPage.chineseMetaEvent[i]));
            eventTypeNameToIndex.Add(EventTypes.Meta[i], i);
        }
    }
    private void Start()
    {
        eventTypeDP.onValueChanged.AddListener(OnEventTypeChanged);
        startTimeIF.onValueChanged.AddListener(OnStartTimeChanged);
        delete.onClick.AddListener(LevelPage.DeleteSelected);
    }
    private void Update()
    {
        // handle dragging
        if (Input.GetMouseButton(0) && LevelPage.OneSelection)
        {
            if (currentEvent.pointerDownStartTime > 0 && Time.time - currentEvent.pointerDownStartTime >= 0.25f)
            {
                currentEvent.dragging = true;
                currentEvent.pointerDownStartTime = -1;
                MusicalLevelEditor.ins.scrolls[1].horizontal = false;
                MusicalLevelEditor.ins.scrolls[2].horizontal = false;
                startTimeIF.interactable = false;
                offset = MusicalLevelEditor.ins.mousePositionInScroll[1].x - currentEvent.mirror.GetComponent<RectTransform>().anchoredPosition.x;
                print("Dragging starts");
            }
        }
        else currentEvent.pointerDownStartTime = -1;

        if (currentEvent && currentEvent.dragging)
        {
            if (Input.GetMouseButton(0))
            {
                float x = MusicalLevelEditor.ins.mousePositionInScroll[1].x - offset;
                float time = MusicalLevelEditor.ins.PagePosXToTime(EditingPage.Panel_Left, x);
                currentEvent.kd.startTime = time;
                startTimeIF.text = time.ToString();
                currentEvent.mirror.GetComponent<RectTransform>().anchoredPosition = currentEvent.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 0);
            }
            else
            {
                currentEvent.dragging = false;
                startTimeIF.interactable = true;
                print("Dragging ends");
            }
        }
    }
    public void Show(EditorEvent e)
    {
        currentEvent = e;
        for (int i = 0; i < pages.Length; ++i) pages[i].SetActive(i != (int)e.eventType);
        string str;
        if (e.eventType == LevelEventType.Meta)
        {
            if (e.kd.prop.TryGetValue("Type", out str))
            {
                eventTypeDP.value = eventTypeNameToIndex[str];
            }
            else eventTypeDP.value = 0;
            startTimeIF.text = e.kd.startTime.ToString();
        }
        else
        {

        }
    }
    void OnEventTypeChanged(int idx)
    {
        currentEvent.kd.prop["Type"] = EventTypes.Meta[idx];
    }
    void OnStartTimeChanged(string t)
    {
        float time = float.Parse(t);
        currentEvent.kd.startTime = time;
        currentEvent.mirror.GetComponent<RectTransform>().anchoredPosition = currentEvent.GetComponent<RectTransform>().anchoredPosition = new Vector2(MusicalLevelEditor.ins.TimeToPagePosX(EditingPage.Panel_Left, time), 0);
    }
}
