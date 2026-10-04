using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Required for Coroutines

public class MainMenuController : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;

    public void PlayGame()
    {
        Debug.Log("Loading Gameplay...");
        StartCoroutine(PlaySoundAndLoadScene("Gameplay"));
    }

    public void OpenCredits()
    {
        Debug.Log("Loading Credits...");
        StartCoroutine(PlaySoundAndLoadScene("Credits"));
    }

    public void ShutdownGame()
    {
        Debug.Log("Shutting down tablet...");
        
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }

        PlayerPrefs.Save();
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // This Coroutine plays the sound, waits for it to finish, then loads the scene
    private IEnumerator PlaySoundAndLoadScene(string sceneName)
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
            yield return new WaitForSeconds(buttonClickSound.length);
        }
        
        SceneManager.LoadScene(sceneName);
    }
}