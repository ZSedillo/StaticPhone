using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeleteProgressButton : MonoBehaviour
{
    private void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnDeleteClicked);
        }
    }

    private void OnDeleteClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetAllProgress();
        }
        else
        {
            ChatSaveSystem.DeleteAllProgress();
        }

        // Always reset the static seen-card memory
        DatingCardController.ResetSeenProfiles();

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}