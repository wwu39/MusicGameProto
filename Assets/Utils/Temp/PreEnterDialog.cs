using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PreEnterDialog : MonoBehaviour
{
    float min = 0.5f;
    float max = 5f;
    [SerializeField] Text fallingTimeText;
    [SerializeField] Scrollbar fallingTimeScrollbar;
    [SerializeField] Button backButton;
    [SerializeField] Button startButton;

    private void Start()
    {
        fallingTimeScrollbar.onValueChanged.AddListener(OnFallingTimeChanged);
        fallingTimeScrollbar.value = 2.5f / 4.5f;
        startButton.onClick.AddListener(delegate { SceneManager.LoadScene(1); });
        backButton.onClick.AddListener(delegate { Destroy(gameObject); });
    }

    void OnFallingTimeChanged(float f)
    {
        float fallingTime = Mathf.Round(((max - min) * f + min) * 100) / 100;
        fallingTimeText.text = "下落时间：" + fallingTime;
        SelectLevel.ins.fallingTime = fallingTime;
    }
}
