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
    static float[] deepness = new float[3] { 0.2f, 0.5f, 1f };
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
    [SerializeField] Image[] deepnessZones;
    [SerializeField] Image cord;
    [SerializeField] Text debugText;
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
        vEventIns = FMODUnity.RuntimeManager.CreateInstance("event:/" + FMODEvent);
        cordPos = GetCordPos();
    }

    public override RhythmObject Initialize(int _exit, Color? c = null, int _perfectScore = 20, int _goodScore = 10, int _badScore = 0)
    {
        return base.Initialize(_exit, null, _perfectScore, _goodScore, _badScore);
    }
    protected override void CheckActivateCondition()
    {
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom() + 1.5 * RhythmGameManager.blockHeight)
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

    protected override void Update_Activated()
    {
        float diff = rt.anchoredPosition.y - RhythmGameManager.GetBottom();
        if (diff < -2f * RhythmGameManager.blockHeight) Destroy(gameObject);
    }

    void GraphicSetup()
    {
        keyWidth = exits[exit + width - 1].x2 - exits[exit].x1;
        float centerX = keyWidth / 2 - RhythmGameManager.exitWidth / 2;
        for (int i = 0; i < 3; ++i)
        {
            deepnessZones[i].rectTransform.sizeDelta = new Vector2(keyWidth * deepness[i], RhythmGameManager.blockHeight);
            deepnessZones[i].rectTransform.anchoredPosition = new Vector2(centerX, 0);
        }
        cord.rectTransform.sizeDelta = new Vector2(4, RhythmGameManager.blockHeight);
        cord.rectTransform.anchoredPosition = new Vector2(centerX, 0);
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
        float centerX = maxWidth / 2 - RhythmGameManager.exitWidth / 2;
        return exits[exit].center.x + centerX;
    }
}
