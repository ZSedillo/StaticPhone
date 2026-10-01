using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SettingsAppController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider volumeSlider;
    
    [SerializeField] private Button btnRestartProgress;
    [SerializeField] private Button btnExitToMenu; // Renamed to clarify its new purpose

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
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        //ChatSaveSystem.WipeAllData();
        DatingCardController.ResetSeenProfiles();
        Debug.Log("Progress deleted. Please restart the game.");
    }

    // You can also call this directly from your MainMenu App Icon!
    public void ExitToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenu");
    }
}