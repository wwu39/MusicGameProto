using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public enum VibDepth
{
    no,
    shallow,
    deep
}
public enum VibRate
{
    slow,
    fast
}

public class Rouxian : RhythmObject
{
    static float lowSpeed=0.4f;
    Vector2 rightend;
    public class TouchTracker
    {
        Rouxian owner;
        public Vector2 lastPos;
        public float leftMax;
        public float rightMax;
        public int leftToRightCount;
        public int rightToLeftCount;
        public float time;
        public float cordLength;
        public string cordLengthComps;
        public Vector2 velocity;
        public float speed;
        public float distance;
        public TouchTracker(Touch t, Rouxian _owner)
        {
            owner = _owner;
            lastPos = t.position;
            leftMax = rightMax = -1;
            time = Time.time;
            cordLength = 0;
        }
        public string GetStateString()
        {
            float frac = cordLength / owner.keyWidth;
            string ret = "";
            bool isLowSpeed = IsLowSpeed(speed, frac);
            ret += isLowSpeed ? "慢" : "快";
            owner.SetVibRate(isLowSpeed ? VibRate.slow : VibRate.fast);
            string deepness = "揉";
            VibDepth vd = VibDepth.no;
            if (frac < deepness[0])
            {
                deepness = "浅揉";
                vd = VibDepth.shallow;
            }
            else if (frac > deepness[1])
            {
                deepness = "深揉";
                vd = VibDepth.deep;
            }
            //ret += cordLength + "=" + cordLengthComps;
            ret += deepness;
            owner.SetVibDepth(vd);
            return ret;
        }
        bool IsLowSpeed(float speed, float cordLengthFrac)
        {
            return speed * (1 - cordLengthFrac) < lowSpeed * owner.keyWidth;
        }
    }

    [Header("Properties")]
    public string FMODEvent;
    public int width;
    public float timeLast;
    public VibDepth vd;
    public VibRate vr;
    [Header("Graphics")]
    [SerializeField] Image block;
    [SerializeField] Image back;
    [SerializeField] Image cord;
    [SerializeField] Text debugText;
    GameObject particle;
    FMOD.Studio.EventInstance vEventIns;
    TouchTracker tt;
    int speedUpdateCount = 0;
    int speedUpdateRate = 30;
    float cordPos;
    float keyWidth;
    public override RhythmType Type => RhythmType.Rouxian;

    protected override void Start()
    {
        base.Start();
        GraphicSetup();
        fallBelowBottom = false;
        OnBottomReached += delegate { StartCoroutine(Interacting()); };
        if (FMODEvent != "none") vEventIns = FMODUnity.RuntimeManager.CreateInstance("event:/" + FMODEvent);
        cordPos = GetCordPos();
    }

    public override RhythmObject Initialize(int _exit, Color? c = null, int _perfectScore = 20, int _goodScore = 10, int _badScore = 0)
    {
        var ret = base.Initialize(_exit, c, _perfectScore, _goodScore, _badScore);
        back.color /= 2;
        cord.color = Color.black;
        return ret;
    }
    protected override void CheckActivateCondition()
    {
        if (rt.anchoredPosition.y <= RhythmGameManager.GetBottom())
        {
            Activate();
        }
    }

    private void FixedUpdate()
    {
        if (Input.touchCount == 0)
        {
            tt = null;
            vEventIns.setVolume(0);
            debugText.text = "没有在揉";
            return;
        }
        vEventIns.setVolume(0);
        Touch t = Input.GetTouch(0);
        if (tt == null) tt = new TouchTracker(t, this);
        tt.velocity = t.position - tt.lastPos;

        if (t.position.x > cordPos)
        {
            float right = t.position.x - cordPos;
            if (right > tt.rightMax) tt.rightMax = right;
        }
        else if (t.position.x < cordPos)
        {
            float left = cordPos - t.position.x;
            if (left > tt.leftMax) tt.leftMax = left;
        }

        if (t.position.x > cordPos && tt.lastPos.x < cordPos)
        {
            tt.rightToLeftCount++;
            IfFinishOneRound();
        }

        if (t.position.x < cordPos && tt.lastPos.x > cordPos)
        {
            tt.leftToRightCount++;
            IfFinishOneRound();
        }

        // update speed
        tt.distance += (t.position - tt.lastPos).magnitude;
        if (speedUpdateCount >= speedUpdateRate)
        {
            float time = speedUpdateRate * Time.fixedDeltaTime;
            tt.speed = tt.distance / time;
            tt.distance = 0;
            tt.time = 0;
            speedUpdateCount = 0;
        }
        else
        {
            ++speedUpdateCount;
        }
        tt.lastPos = t.position;
        debugText.text = tt.GetStateString();
    }

    float timeCount;
    float pardelay;
    bool toLeft;
    protected override void Update_Activated()
    {
        float diff = rt.anchoredPosition.y - RhythmGameManager.GetBottom();
        if (diff < -2f * BlockSize.y) Destroy(gameObject);

        Vector2 dotpos = new Vector2();
        if (timeCount >= 1f)
        {
            timeCount = 0;
            toLeft = !toLeft;
        }
        else
        {
            float frac = timeCount / 1f;
            if (toLeft)
            {
                block.rectTransform.anchoredPosition = Vector2.Lerp(rightend, Vector2.zero, frac);
                dotpos.x = block.rectTransform.anchoredPosition.x;
                dotpos.y = 400 + 100 * Mathf.Sin(2 * Mathf.PI * frac);
            }
            else
            {
                block.rectTransform.anchoredPosition = Vector2.Lerp(Vector2.zero, rightend, frac);
                dotpos.x = block.rectTransform.anchoredPosition.x;
                dotpos.y = 400 + 100 * Mathf.Cos(2 * Mathf.PI * (1 - frac) + 0.5f * Mathf.PI);
            }
            timeCount += Time.deltaTime;
        }
        if (pardelay >= 0.05f)
        {
            var trail = Instantiate(Resources.Load<GameObject>("Trail"), transform);
            (trail.transform as RectTransform).anchoredPosition = dotpos;
            pardelay = 0;
        }
        else
        {
            pardelay += Time.deltaTime;
        }
    }

    void GraphicSetup()
    {
        keyWidth = exits[exit + width - 1].x2 - exits[exit].x1;
        float centerX = keyWidth / 2 - BlockSize.x / 2;
        back.rectTransform.sizeDelta = new Vector2(keyWidth, BlockSize.y);
        block.rectTransform.sizeDelta = new Vector2(BlockSize.x, BlockSize.y);
        cord.rectTransform.sizeDelta = new Vector2(4, BlockSize.y);
        back.rectTransform.anchoredPosition = cord.rectTransform.anchoredPosition = new Vector2(centerX, 0);
        block.rectTransform.anchoredPosition = Vector2.zero;
        rightend = new Vector2(keyWidth - BlockSize.x, 0);

        OnBottomReached += delegate
        {
            particle = Instantiate(Resources.Load<GameObject>("RouxianParticle"), transform);
            (particle.transform as RectTransform).anchoredPosition = new Vector2(centerX, BlockSize.y / 2f);
            particle.transform.localScale = new Vector3(2, 1, 1) * 4.5f * keyWidth / 1140f;
        };
    }

    IEnumerator Interacting()
    {
        print("reached bottom");
        yield return new WaitForSeconds(timeLast);
        print("time " + timeLast + " passed");
        fallBelowBottom = true;
    }
    void IfFinishOneRound()
    {
        if (Mathf.Abs(tt.rightToLeftCount - tt.rightToLeftCount) > 1)
        {
            tt = null;
            debugText.text = "没有在揉";
            return;
        }
        if (tt.leftToRightCount == tt.rightToLeftCount)
        {
            //float newTime = Time.time;
            // float deltaTime = newTime - tt.time;
            if (tt.leftMax > 0 && tt.rightMax > 0)
            {
                tt.cordLength = tt.leftMax + tt.rightMax;
                tt.cordLengthComps = tt.leftMax + "+" + tt.rightMax;
                // tt.speed = tt.cordLength / deltaTime;
            }
            //tt.time = newTime;
            tt.leftMax = tt.rightMax = -1;
            print("Finish round " + tt.leftToRightCount + " " + tt.speed);
        }
    }
    void SetVibRate(VibRate r)
    {

    }

    void SetVibDepth(VibDepth d)
    {

    }

    float GetCordPos()
    {
        float maxWidth = exits[exit + width - 1].x2 - exits[exit].x1;
        float centerX = maxWidth / 2 - BlockSize.x / 2;
        return exits[exit].center.x + centerX;
    }
}
