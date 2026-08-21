using UnityEngine;

public class NavigationBarManager : MonoBehaviour
{
    [Header("Core References")]
    public HorizontalSwipeSnap homeScreenSwiper;
    public GameObject appContainer;
    
    [Tooltip("Drag your NotificationContainer here!")]
    public NotificationSwipe notificationPanel; // <-- ADD THIS LINE

    public void OnHomeButtonClicked()
    {
        // 1. Tell the Home Screen to slide back to Page 1
        if (homeScreenSwiper != null)
        {
            homeScreenSwiper.GoToHomePage();
        }

        // 2. Force the notification panel to close
        if (notificationPanel != null) // <-- ADD THIS BLOCK
        {
            notificationPanel.CloseNotification();
        }

        // 3. Close any apps that are currently open
        if (appContainer != null)
        {
            foreach (Transform app in appContainer.transform)
            {
                app.gameObject.SetActive(false);
            }
        }
    }

    public void OnBackButtonClicked()
    {
        Debug.Log("Back Button Pressed!");
    }

    public void OnRecentsButtonClicked()
    {
        Debug.Log("Recents Button Pressed!");
    }
}