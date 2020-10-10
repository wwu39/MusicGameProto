using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;

public class Interpreter
{
    public static void Open(string filename, out List<KeyData> keyData, out Dictionary<string, Dictionary<string,string>> sections)
    {
        keyData = new List<KeyData>();
        sections = new Dictionary<string, Dictionary<string, string>>();
        bool isSection = true;
        string curSec = "";
        string[] lines = File.ReadAllLines(filename);
        foreach (var line in lines)
        {
            // remove comment
            string nocom = line.Split(';')[0].Trim();
            if (nocom.Length > 0)
            {
                if (nocom[0] == '[') // section begins
                {
                    curSec = nocom.Substring(1, line.Length - 2);
                    float curTime;
                    if (float.TryParse(curSec, out curTime))
                    {
                        KeyData kd = new KeyData(curTime);
                        keyData.Add(kd);
                        isSection = false;
                    }
                    else
                    {
                        sections.Add(curSec, new Dictionary<string, string>());
                        isSection = true;
                    }
                }
                else
                {
                    var pair = line.Split('=');
                    (isSection? sections[curSec]:keyData[keyData.Count - 1].prop).Add(pair[0], pair[1]);
                }
            }
        }
    }
}
