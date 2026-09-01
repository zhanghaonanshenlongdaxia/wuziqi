using UnityEngine;
using UnityEngine.UI;
using Wuziqi.Game;
using Wuziqi.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("References")]
    public Button startButton;
    public Button exitButton;

    [Header("Roots")]
    public GameObject gamePlayRoot;

    [Header("问候弹窗")]
    [SerializeField] private ConfirmDialog confirmDialogPrefab;

    private ConfirmDialog activeDialog;

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

        // 暂停游戏，显示问候弹窗
        if (GameManager.Instance) GameManager.Instance.PauseGame();
        ShowGreetingDialog();
    }

    private void ShowGreetingDialog()
    {
        if (confirmDialogPrefab == null) { Debug.LogWarning("[Greeting] prefab is null"); return; }

        var cat = CatManager.Instance?.Selected;
        if (cat == null) { Debug.LogWarning("[Greeting] cat is null"); return; }
        var greetings = cat.GetGreetings();
        if (greetings.Length == 0) { Debug.LogWarning("[Greeting] greetings is empty for " + cat.catName); return; }

        string greeting = greetings[Random.Range(0, greetings.Length)];
        Debug.Log($"[Greeting] cat={cat.catName}, greeting={greeting}");

        activeDialog = Instantiate(confirmDialogPrefab, transform.root);
        activeDialog.transform.SetAsLastSibling();
        activeDialog.Show(
            message: greeting,
            onConfirm: () =>
            {
                if (activeDialog) { Destroy(activeDialog.gameObject); activeDialog = null; }
                if (GameManager.Instance) GameManager.Instance.ResumeGame();
            },
            title: cat.catName,
            confirmText: "开始游戏"
        );
        Debug.Log("[Greeting] dialog shown");
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
