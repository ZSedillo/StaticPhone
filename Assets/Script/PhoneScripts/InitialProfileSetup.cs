using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ProfileDataWrapper
{
    public List<string> names;
    public List<string> personalityTypes;
    public List<string> bios;
    public int minAge;
    public int maxAge;
}

public class InitialProfileSetup : MonoBehaviour
{
    [Header("Step-by-Step UI Panels")]
    [SerializeField] private GameObject[] stepPanels; // Holds the 5 screens
    [SerializeField] private TextMeshProUGUI summaryTextDisplay; // Shows the final info

    [Header("Input References")]
    [SerializeField] private TMP_InputField inputName;
    [SerializeField] private TMP_InputField inputAge;
    [SerializeField] private TMP_Dropdown dropdownPersonality;
    [SerializeField] private TMP_InputField inputBio;
    
    [Header("Buttons")]
    [SerializeField] private Button[] btnNextSteps; // The 'Next' buttons on steps 1-4
    [SerializeField] private Button btnSignUp;      // Final 'Sign Up' button
    [SerializeField] private Button btnPass;        // 'Pass' button to restart/cancel

    [Header("Avatar Settings")]
    [SerializeField] private Image avatarDisplay;
    [SerializeField] private Button btnPrevAvatar;
    [SerializeField] private Button btnNextAvatar;
    [SerializeField] private Button btnTakeSelfie;
    [SerializeField] private List<Sprite> presetAvatars = new List<Sprite>();
    
    [Header("Data Source")]
    [SerializeField] private TextAsset profileJsonFile;

    private int currentStepIndex = 0;
    private int currentAvatarIndex = 0;
    private WebCamTexture webcamTexture;
    private bool isUsingWebcamPhoto = false;
    private List<string> allPersonalities = new List<string>();

    void Start()
    {
        LoadPersonalitiesFromJson();

        // Hook up all 'Next' buttons
        foreach (Button btn in btnNextSteps)
        {
            if (btn != null) btn.onClick.AddListener(GoToNextStep);
        }

        // Hook up final buttons
        if (btnSignUp != null) btnSignUp.onClick.AddListener(CompleteSetup);
        if (btnPass != null) btnPass.onClick.AddListener(RestartSetup);

        // Hook up Avatar buttons
        if (btnPrevAvatar != null) btnPrevAvatar.onClick.AddListener(PreviousAvatar);
        if (btnNextAvatar != null) btnNextAvatar.onClick.AddListener(NextAvatar);
        if (btnTakeSelfie != null) btnTakeSelfie.onClick.AddListener(CaptureWebcamSelfie);

        UpdateAvatarDisplay();
        ShowStep(0); // Always start on the first screen
    }

    private void GoToNextStep()
    {
        if (currentStepIndex < stepPanels.Length - 1)
        {
            currentStepIndex++;
            
            // If we just reached the final step, generate the text summary
            if (currentStepIndex == stepPanels.Length - 1)
            {
                UpdateSummaryScreen();
            }

            ShowStep(currentStepIndex);
        }
    }

    private void RestartSetup()
    {
        // "Pass" button sends them back to the start to redo it
        currentStepIndex = 0;
        ShowStep(currentStepIndex);
    }

    private void ShowStep(int stepIndex)
    {
        // Loop through all panels and only turn on the one matching our current step
        for (int i = 0; i < stepPanels.Length; i++)
        {
            if (stepPanels[i] != null)
            {
                stepPanels[i].SetActive(i == stepIndex);
            }
        }
    }

    private void UpdateSummaryScreen()
    {
        if (summaryTextDisplay != null)
        {
            string nameTxt = inputName != null && !string.IsNullOrEmpty(inputName.text) ? inputName.text : "Unknown";
            string ageTxt = inputAge != null && !string.IsNullOrEmpty(inputAge.text) ? inputAge.text : "Unknown";
            string bioTxt = inputBio != null && !string.IsNullOrEmpty(inputBio.text) ? inputBio.text : "No bio provided.";
            string personalityTxt = "Unknown";
            
            if (dropdownPersonality != null && allPersonalities.Count > 0)
            {
                personalityTxt = allPersonalities[dropdownPersonality.value];
            }

            summaryTextDisplay.text = $"<b>Username:</b> {nameTxt}\n<b>Age:</b> {ageTxt}\n<b>Personality:</b> {personalityTxt}\n<b>Bio:</b> {bioTxt}";
        }
    }

    private void LoadPersonalitiesFromJson()
    {
        if (profileJsonFile != null)
        {
            try
            {
                ProfileDataWrapper data = JsonUtility.FromJson<ProfileDataWrapper>(profileJsonFile.text);
                if (data != null && data.personalityTypes != null && data.personalityTypes.Count > 0)
                {
                    allPersonalities = new List<string>(data.personalityTypes);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Failed to parse JSON: " + e.Message);
            }
        }

        if (allPersonalities.Count == 0)
        {
            allPersonalities = new List<string> { "Introvert", "Workaholic", "Gamer", "Chaotic", "Overthinker", "Night Owl" };
        }

        if (dropdownPersonality != null)
        {
            dropdownPersonality.ClearOptions();
            dropdownPersonality.AddOptions(allPersonalities);
        }
    }

    public void NextAvatar()
    {
        if (presetAvatars.Count == 0) return;
        isUsingWebcamPhoto = false;
        currentAvatarIndex = (currentAvatarIndex + 1) % presetAvatars.Count;
        UpdateAvatarDisplay();
    }

    public void PreviousAvatar()
    {
        if (presetAvatars.Count == 0) return;
        isUsingWebcamPhoto = false;
        currentAvatarIndex--;
        if (currentAvatarIndex < 0) currentAvatarIndex = presetAvatars.Count - 1;
        UpdateAvatarDisplay();
    }

    private void UpdateAvatarDisplay()
    {
        if (presetAvatars.Count > 0 && avatarDisplay != null && !isUsingWebcamPhoto)
        {
            avatarDisplay.sprite = presetAvatars[currentAvatarIndex];
            PlayerProfileController.CurrentAvatarSprite = presetAvatars[currentAvatarIndex];
        }
    }

    public void CaptureWebcamSelfie()
    {
        if (WebCamTexture.devices.Length == 0) return;
        if (webcamTexture == null)
        {
            webcamTexture = new WebCamTexture();
            webcamTexture.Play();
        }
        StartCoroutine(TakeSelfieSnapshot());
    }

    private IEnumerator TakeSelfieSnapshot()
    {
        yield return new WaitForSeconds(0.2f);
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            Texture2D photoTex = new Texture2D(webcamTexture.width, webcamTexture.height);
            photoTex.SetPixels(webcamTexture.GetPixels());
            photoTex.Apply();

            Sprite webcamSprite = Sprite.Create(photoTex, new Rect(0, 0, photoTex.width, photoTex.height), new Vector2(0.5f, 0.5f));
            avatarDisplay.sprite = webcamSprite;
            PlayerProfileController.CurrentAvatarSprite = webcamSprite;
            isUsingWebcamPhoto = true;

            webcamTexture.Stop();
        }
    }

    public void CompleteSetup()
    {
        if (GameManager.Instance != null)
        {
            UserProfileData user = GameManager.Instance.currentUser;

            if (inputName != null && !string.IsNullOrEmpty(inputName.text)) 
                user.playerName = inputName.text.Trim();

            if (inputAge != null && int.TryParse(inputAge.text.Trim(), out int parsedAge)) 
                user.playerAge = parsedAge;

            if (inputBio != null) 
                user.playerBio = inputBio.text;

            if (dropdownPersonality != null && dropdownPersonality.value < allPersonalities.Count)
                user.playerPersonality = allPersonalities[dropdownPersonality.value];

            user.avatarIndex = currentAvatarIndex;
        }

        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying) webcamTexture.Stop();
    }
}