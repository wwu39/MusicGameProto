using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlockEnlarge : MonoBehaviour
{
    [SerializeField] Graphic[] coloringParts;
    [SerializeField] float lifetime;
    [SerializeField] Vector3 startScale;
    [SerializeField] Vector3 endScale;
    [SerializeField] bool fades;
    Color startColor;
    float startTime;
    Color endColor;
    // Start is called before the first frame update
    void Start()
    {
        foreach (Graphic g in coloringParts) g.color = startColor;
        startTime = Time.time;
        transform.localScale = startScale;
        if (fades)
        {
            endColor = startColor;
            endColor.a = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        float frac = (Time.time - startTime) / lifetime;
        if (frac >= 1)
        {
            Destroy(gameObject);
        }
        else
        {
            transform.localScale = Vector3.Lerp(startScale, endScale, frac);
            if (fades) foreach (Graphic g in coloringParts) g.color = Color.Lerp(startColor, endColor, frac);
        }
    }

    public static void Create(Sprite sprite, Color c, Vector2 pos, Transform parent)
    {
        BlockEnlarge be = Instantiate(Resources.Load<GameObject>("BlockEnlarge"), parent).GetComponent<BlockEnlarge>();
        be.startColor = c;
        be.GetComponent<Image>().sprite = sprite;
        (be.transform as RectTransform).anchoredPosition = pos;
    }
}
