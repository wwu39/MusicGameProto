using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TestFileGenerator
{
    // Start is called before the first frame update
    public TestFileGenerator()
    {
        string text = "[General]" + System.Environment.NewLine + "Exit=7" + System.Environment.NewLine;
        text += System.Environment.NewLine;
        for (float i = Random.Range(0, 1.231f); i < 180f; i += Random.Range(1.111f, 3.333f))
        {
            text += "[" + i + "]" + System.Environment.NewLine;
            text += "Exit=" + Random.Range(0, 7) + System.Environment.NewLine;
            text += "FallingTime=" + Random.Range(3.5f, 4.5f) + System.Environment.NewLine;
            text += System.Environment.NewLine;
        }
        File.WriteAllText("Assets\\Music\\Resources\\TextMusic\\001.txt", text);
    }
}
