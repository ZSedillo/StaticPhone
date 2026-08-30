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

public class PlayerProfileController : MonoBehaviour
{
    [Header("Scroll Container to Resize")]
    public RectTransform profileContentRect;
    public float extraBottomPadding = 60f;

    [Header("Avatar Settings")]
    public Image avatarDisplay;
    public Button btnPrevAvatar;
    public Button btnNextAvatar;
    public Button btnTakeSelfie;
    public List<Sprite> presetAvatars = new List<Sprite>();
    private int currentAvatarIndex = 0;
    private WebCamTexture webcamTexture;
    private bool isUsingWebcamPhoto = false;

    [Header("View Mode References")]
    public GameObject viewModeContainer;
    public Button btnEdit;
    public TextMeshProUGUI displayNameAge;
    public TextMeshProUGUI displayPersonality;
    public TextMeshProUGUI displayBio;

    [Header("Edit Mode References")]
    public GameObject editModeContainer;
    public TMP_InputField inputName;
    public TMP_InputField inputAge;
    public TMP_Dropdown dropdownPersonality;
    public TMP_InputField inputBio;
    public Button btnConfirm;
    public Button btnCancel;

    [Header("Data Source")]
    public TextAsset profileJsonFile;

    public static string CurrentName = "Matchi";
    public static int CurrentAge = 21;
    public static string CurrentPersonality = "Introvert";
    public static string CurrentBio = "Just another insomniac staring at a static screen.";
    public static Sprite CurrentAvatarSprite;

    private List<string> allPersonalities = new List<string>();

    void Start()
    {
        LoadPersonalitiesFromJson();

        if (btnPrevAvatar != null) btnPrevAvatar.onClick.AddListener(PreviousAvatar);
        if (btnNextAvatar != null) btnNextAvatar.onClick.AddListener(NextAvatar);
        if (btnTakeSelfie != null) btnTakeSelfie.onClick.AddListener(CaptureWebcamSelfie);

        if (btnEdit != null) btnEdit.onClick.AddListener(() => SetEditMode(true));
        if (btnConfirm != null) btnConfirm.onClick.AddListener(SaveProfileChanges);
        if (btnCancel != null) btnCancel.onClick.AddListener(() => SetEditMode(false));

        UpdateAvatarDisplay();
        RefreshViewDisplay();
        SetEditMode(false);
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
                Debug.LogWarning("Failed to parse JSON, using fallback: " + e.Message);
            }
        }

        if (allPersonalities.Count == 0)
        {
            allPersonalities = new List<string> {
                "Introvert", "Workaholic", "Gamer", "Chaotic", "Overthinker", "Night Owl", "Hopeless Romantic"
            };
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
            CurrentAvatarSprite = presetAvatars[currentAvatarIndex];
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
            CurrentAvatarSprite = webcamSprite;
            isUsingWebcamPhoto = true;

            webcamTexture.Stop();
        }
    }

    public void SetEditMode(bool isEditing)
    {
        if (editModeContainer != null) editModeContainer.SetActive(isEditing);
        if (viewModeContainer != null) viewModeContainer.SetActive(!isEditing);
        if (btnEdit != null) btnEdit.gameObject.SetActive(!isEditing);

        if (isEditing)
        {
            if (inputName != null) inputName.text = CurrentName;
            if (inputAge != null) inputAge.text = CurrentAge.ToString();
            if (inputBio != null) inputBio.text = CurrentBio;

            if (dropdownPersonality != null)
            {
                dropdownPersonality.ClearOptions();
                dropdownPersonality.AddOptions(allPersonalities);

                int idx = allPersonalities.IndexOf(CurrentPersonality);
                dropdownPersonality.value = idx >= 0 ? idx : 0;
                dropdownPersonality.RefreshShownValue();
            }
        }
        else
        {
            StartCoroutine(RecalculateContentHeightNextFrame());
        }
    }

    public void SaveProfileChanges()
    {
        if (inputName != null && !string.IsNullOrEmpty(inputName.text)) CurrentName = inputName.text;
        if (inputAge != null && int.TryParse(inputAge.text, out int parsedAge)) CurrentAge = parsedAge;
        if (inputBio != null) CurrentBio = inputBio.text;

        if (dropdownPersonality != null && dropdownPersonality.value < allPersonalities.Count)
        {
            CurrentPersonality = allPersonalities[dropdownPersonality.value];
        }

        RefreshViewDisplay();
        SetEditMode(false);
    }

    private void RefreshViewDisplay()
    {
        if (displayNameAge != null) displayNameAge.text = $"{CurrentName}, {CurrentAge}";
        if (displayPersonality != null) displayPersonality.text = CurrentPersonality;
        if (displayBio != null) displayBio.text = CurrentBio;

        StartCoroutine(RecalculateContentHeightNextFrame());
    }

    private IEnumerator RecalculateContentHeightNextFrame()
    {
        yield return null;

        if (profileContentRect == null || displayBio == null) yield break;

        float textHeight = displayBio.preferredHeight;
        displayBio.rectTransform.sizeDelta = new Vector2(displayBio.rectTransform.sizeDelta.x, textHeight);

        float bioLocalBottomY = Mathf.Abs(displayBio.transform.localPosition.y) + textHeight;
        float infoSectionTopOffset = 315f;
        float totalCalculatedHeight = infoSectionTopOffset + bioLocalBottomY + extraBottomPadding;

        float finalHeight = Mathf.Max(totalCalculatedHeight, 750f);
        profileContentRect.sizeDelta = new Vector2(profileContentRect.sizeDelta.x, finalHeight);
    }

    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}