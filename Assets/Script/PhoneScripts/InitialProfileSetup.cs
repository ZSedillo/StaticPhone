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
    [SerializeField] private GameObject[] stepPanels;
    [SerializeField] private TextMeshProUGUI summaryTextDisplay;

    [Header("Input References")]
    [SerializeField] private TMP_InputField inputName;
    [SerializeField] private TMP_InputField inputAge;
    [SerializeField] private TMP_Dropdown dropdownPersonality;
    [SerializeField] private TMP_InputField inputBio;
    
    [Header("Buttons")]
    [SerializeField] private Button[] btnNextSteps; 
    [SerializeField] private Button[] btnPrevSteps; 
    [SerializeField] private Button btnSignUp;      
    [SerializeField] private Button btnPass;        

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
    private bool isTransitioning = false;

    void Start()
    {
        LoadPersonalitiesFromJson();

        // Hook up Next buttons
        foreach (Button btn in btnNextSteps)
        {
            if (btn != null) btn.onClick.AddListener(GoToNextStep);
        }

        // Hook up Return buttons
        foreach (Button btn in btnPrevSteps)
        {
            if (btn != null) btn.onClick.AddListener(GoToPreviousStep);
        }

        // Hook up Final buttons
        if (btnSignUp != null) btnSignUp.onClick.AddListener(CompleteSetup);
        if (btnPass != null) btnPass.onClick.AddListener(RestartSetup);
        
        // Hook up Avatar buttons
        if (btnPrevAvatar != null) btnPrevAvatar.onClick.AddListener(PreviousAvatar);
        if (btnNextAvatar != null) btnNextAvatar.onClick.AddListener(NextAvatar);
        if (btnTakeSelfie != null) btnTakeSelfie.onClick.AddListener(CaptureWebcamSelfie);

        UpdateAvatarDisplay();
        InitializeSteps();
    }

    private void GoToNextStep()
    {
        if (isTransitioning || currentStepIndex >= stepPanels.Length - 1) return;
        
        int nextIndex = currentStepIndex + 1;
        if (nextIndex == stepPanels.Length - 1) UpdateSummaryScreen();
        
        StartCoroutine(FadeTransition(currentStepIndex, nextIndex));
    }

    private void GoToPreviousStep()
    {
        if (isTransitioning || currentStepIndex <= 0) return;
        
        int prevIndex = currentStepIndex - 1;
        StartCoroutine(FadeTransition(currentStepIndex, prevIndex));
    }

    private void RestartSetup()
    {
        if (isTransitioning) return;
        StartCoroutine(FadeTransition(currentStepIndex, 0));
    }

    // --- SMOOTH FADE ANIMATION ---
    private IEnumerator FadeTransition(int fromIndex, int toIndex)
    {
        isTransitioning = true;
        CanvasGroup fromCG = GetOrAddCanvasGroup(stepPanels[fromIndex]);
        CanvasGroup toCG = GetOrAddCanvasGroup(stepPanels[toIndex]);

        // Fade out current screen quickly
        float elapsed = 0f;
        while(elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            fromCG.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.15f);
            yield return null;
        }
        stepPanels[fromIndex].SetActive(false);

        // Update index
        currentStepIndex = toIndex;

        // Fade in new screen
        stepPanels[toIndex].SetActive(true);
        elapsed = 0f;
        while(elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            toCG.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.15f);
            yield return null;
        }
        
        isTransitioning = false;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }

    private void InitializeSteps()
    {
        for (int i = 0; i < stepPanels.Length; i++)
        {
            if (stepPanels[i] == null) continue;
            CanvasGroup cg = GetOrAddCanvasGroup(stepPanels[i]);
            bool isFirst = (i == 0);
            stepPanels[i].SetActive(isFirst);
            cg.alpha = isFirst ? 1f : 0f;
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

            // Using Unity Rich Text to bold the labels
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
            catch (System.Exception e) { Debug.LogWarning("Failed to parse JSON: " + e.Message); }
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
            {
                // Forces the age to stay between 1 and 99
                user.playerAge = Mathf.Clamp(parsedAge, 1, 99);
            }
            
            if (inputBio != null) 
                user.playerBio = inputBio.text;
                
            if (dropdownPersonality != null && dropdownPersonality.value < allPersonalities.Count)
                user.playerPersonality = allPersonalities[dropdownPersonality.value];
                
            user.avatarIndex = currentAvatarIndex;
        }
        
        // Hides the setup screen so the main game can begin
        gameObject.SetActive(false); 
    }

    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying) webcamTexture.Stop();
    }
}