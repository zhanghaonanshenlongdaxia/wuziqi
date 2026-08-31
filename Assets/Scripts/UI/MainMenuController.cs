using UnityEngine;
using UnityEngine.UI;
using Wuziqi.Game;

public class MainMenuController : MonoBehaviour
{
    [Header("References")]
    public Button startButton;
    public Button exitButton;

    [Header("Roots")]
    public GameObject gamePlayRoot;

    private void Awake()
    {
        if (startButton) startButton.onClick.AddListener(EnterGame);
        if (exitButton) exitButton.onClick.AddListener(ExitGame);
    }

    private void EnterGame()
    {
        // 停菜单 BGM
        var bgm = GameObject.Find("BGMPlayer");
        if (bgm)
        {
            var src = bgm.GetComponent<AudioSource>();
            if (src) src.Stop();
        }

        // 隐藏主菜单，显示游戏
        gameObject.SetActive(false);
        if (gamePlayRoot) gamePlayRoot.SetActive(true);

        // 启动游戏逻辑
        if (GameManager.Instance) GameManager.Instance.StartGame();
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
