using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SettingsAppController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider volumeSlider;
    
    [SerializeField] private Button btnRestartProgress;
    [SerializeField] private Button btnExitToMenu;

    private void Start()
    {
        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
            volumeSlider.value = savedVolume;
            AudioListener.volume = savedVolume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (btnRestartProgress != null)
        {
            btnRestartProgress.onClick.AddListener(RestartProgress);
        }

        if (btnExitToMenu != null)
        {
            btnExitToMenu.onClick.AddListener(ExitToMainMenu);
        }
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("GameVolume", value);
    }

    private void RestartProgress()
    {
        Debug.Log("Restarting all progress...");
        
        // 1. Temporarily save the volume
        float currentVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
        
        // 2. Comprehensive wipe using your specific GameManager logic
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetAllProgress();
        }
        else
        {
            ChatSaveSystem.DeleteAllProgress();
        }

        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.ResetAllTasks();
        }

        DatingCardController.ResetSeenProfiles();
        
        PlayerPrefs.DeleteAll();
        
        // 3. Put the volume back and save
        PlayerPrefs.SetFloat("GameVolume", currentVolume);
        PlayerPrefs.Save();
        
        Debug.Log("Progress deleted.");
    }

    public void ExitToMainMenu()
    {
        Debug.Log("Wiping progress and returning to Main Menu...");
        
        // Call the massive wipe function first
        RestartProgress();
        
        // Then load the Main Menu
        SceneManager.LoadScene("Credits");
    }
}