using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoteBar : MonoBehaviour
{
    [HideInInspector] public int num;
    [HideInInspector] public Note note;
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(Pressed);
    }
    public void Pressed()
    {
        FMODUnity.RuntimeManager.PlayOneShot("event:/MidiNotes/" + note.note);
        if (Utils.ControlKeyHeldDown()) MusicalLevelEditor.SelectNote(this, true);
        else if (Utils.AltKeyHeldDown()) MusicalLevelEditor.DeselectNote(this);
        else MusicalLevelEditor.SelectNote(this, false);
        RefreshSelectedState();
    }
    public void PlaySound()
    {
        FMODUnity.RuntimeManager.PlayOneShot("event:/MidiNotes/" + note.note);
    }
    public void RefreshSelectedState()
    {
        GetComponent<Outline>().enabled = MusicalLevelEditor.IsSelected(this);
    }
}
