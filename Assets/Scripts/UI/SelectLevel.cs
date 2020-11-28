using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Michsky.UI.Shift;
using UnityEngine.SceneManagement;

public class SelectLevel : MonoBehaviour
{
    [SerializeField] GameObject node;
    [SerializeField] GameObject chapterPrefab;
    [SerializeField] Sprite[] tilepages;
    [HideInInspector] public string preselectedSongName;

    public static SelectLevel ins;

    private void Awake()
    {
        if (ins) Destroy(gameObject); else ins = this;
        var info = new DirectoryInfo("Assets/Music/Resources");
        var files = info.GetFiles();
        int i = 0;
        foreach (var f in files)
        {
            if (f.Extension == ".b3ks")
            {
                var btn = Instantiate(chapterPrefab, node.transform).GetComponent<ChapterButton>();
                btn.buttonTitle = f.Name.Substring(0, f.Name.Length - 5);
                btn.statusItem = ChapterButton.StatusItem.NONE;
                btn.backgroundImage = tilepages[i % tilepages.Length];
                btn.GetComponent<Button>().onClick.AddListener(delegate
                {
                    preselectedSongName = btn.buttonTitle;
                    SceneManager.LoadScene(1);
                });
                ++i;
            }
        }
        DontDestroyOnLoad(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
