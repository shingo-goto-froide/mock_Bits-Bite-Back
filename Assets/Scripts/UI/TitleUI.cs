using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    private void Start()
    {
        var startButton = transform.Find("StartButton")?.GetComponent<Button>();
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        GameManager.Instance.StartGame();
        SceneManager.LoadScene("PrepareScene");
    }
}
