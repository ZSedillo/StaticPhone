using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserSetupPanelUI : MonoBehaviour
{
    [Header("UI Inputs")]
    public TMP_InputField inputName;
    public TMP_InputField inputAge;
    public Button btnConfirm;

    private void Start()
    {
        if (btnConfirm != null)
            btnConfirm.onClick.AddListener(OnConfirmClicked);
    }

    private void OnConfirmClicked()
    {
        string enteredName = inputName != null ? inputName.text.Trim() : "Player";
        int enteredAge = 20;

        if (inputAge != null && !int.TryParse(inputAge.text.Trim(), out enteredAge))
        {
            enteredAge = 20;
        }

        // Save data directly to GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerBasicInfo(enteredName, enteredAge);
        }

        // Close the setup panel
        gameObject.SetActive(false);
    }
}