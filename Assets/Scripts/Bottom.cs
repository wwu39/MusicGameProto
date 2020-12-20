using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bottom : MonoBehaviour
{
    static Bottom ins;
    [SerializeField] Graphic[] coloringParts;
    [SerializeField] float colorTime = 0.5f;
    Color color;
    float time;
    bool animating;
    private void Awake()
    {
        ins = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (animating)
        {
            time += Time.deltaTime;
            if (time >= colorTime)
            {
                foreach (Graphic g in coloringParts) g.color = Color.black;
                time = 0;
                animating = false;
            }
            else foreach (Graphic g in coloringParts) g.color = color;
        }
    }
    public static void SetColor(Color c)
    {
        ins.color = c;
        ins.animating = true;
        ins.time = 0;
    }
}
