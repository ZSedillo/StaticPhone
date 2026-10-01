using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Required for the New Input System

public class CreditsController : MonoBehaviour
{
    [Header("Scrolling Settings")]
    public RectTransform creditsContainer;
    public float scrollSpeed = 50f;
    public float stopYPosition = 1500f; // Adjust this based on how long your credits are

    [Header("End Prompt")]
    public GameObject returnPrompt;

    private bool creditsFinished = false;

    private void Start()
    {
        if (returnPrompt != null)
        {
            returnPrompt.SetActive(false);
        }
    }

    private void Update()
    {
        // Scroll the credits upwards
        if (!creditsFinished && creditsContainer != null)
        {
            creditsContainer.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

            // Check if the credits have reached the target height
            if (creditsContainer.anchoredPosition.y >= stopYPosition)
            {
                creditsFinished = true;
                if (returnPrompt != null) returnPrompt.SetActive(true);
            }
        }

        // Check for Keyboard (ESC or SPACE)
        if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            ReturnToMainMenu();
        }
        // Check for Mouse (Left Click)
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ReturnToMainMenu();
        }
    }

    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}