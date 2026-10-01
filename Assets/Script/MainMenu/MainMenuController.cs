using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // The word "public" is required here!
    public void PlayGame()
    {
        Debug.Log("Loading Gameplay...");
        SceneManager.LoadScene("Gameplay");
    }

    public void OpenCredits()
    {
        Debug.Log("Loading Credits...");
        SceneManager.LoadScene("Credits");
    }

    public void ShutdownGame()
    {
        Debug.Log("Shutting down tablet...");
        PlayerPrefs.Save();
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}