using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoteBar : MonoBehaviour
{
    [HideInInspector] public Note note;
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(Pressed);
    }
    void Pressed()
    {
        FMODUnity.RuntimeManager.PlayOneShot("event:/MidiNotes/" + note.note);
    }
}
