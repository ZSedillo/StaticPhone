using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class NavigationBarManager : MonoBehaviour
{
    [Header("Recents Settings")]
    public GameObject recentCardPrefab;
    public Transform recentsContentContainer;

    [Header("Core OS References")]
    public HorizontalSwipeSnap homeScreenSwiper;
    public NotificationSwipe notificationPanel;
    public GameObject recentsViewUI; 
    
    // Tracks the app you are currently looking at
    private AppWindow currentApp;
    
    // A history 'stack' for the Back Button
    private Stack<AppWindow> appHistory = new Stack<AppWindow>();
    
    // A list of apps running in the background for the Recents View
    public List<AppWindow> openAppsList = new List<AppWindow>(); 

    // --- APP ROUTING ---
    public void LaunchApplication(AppWindow appToOpen)
    {
        if (currentApp != null)
        {
            appHistory.Push(currentApp); // Remember the current app before we open the next one
            currentApp.gameObject.SetActive(false); // Hide the old app instantly
        }
        
        currentApp = appToOpen;
        currentApp.OpenApp();
        
        // Add to recents if it isn't already there
        if (!openAppsList.Contains(currentApp))
        {
            openAppsList.Add(currentApp);
        }
    }

    // --- NAVIGATION BUTTONS ---
    public void OnHomeButtonClicked()
    {
        // 1. Close current app
        if (currentApp != null)
        {
            currentApp.CloseApp();
            currentApp = null;
        }
        
        // 2. Wipe the back button history
        appHistory.Clear();
        
        // 3. Reset the OS UI
        if (recentsViewUI != null) recentsViewUI.SetActive(false);
        if (notificationPanel != null) notificationPanel.CloseNotification();
        if (homeScreenSwiper != null) homeScreenSwiper.GoToHomePage();
    }

    public void OnBackButtonClicked()
    {
        // If the Samsung Recents view is open, the back button just closes it
        if (recentsViewUI != null && recentsViewUI.activeSelf)
        {
            recentsViewUI.SetActive(false);
            if (currentApp != null) currentApp.OpenApp();
            return;
        }

        // If we have an app open, close it
        if (currentApp != null)
        {
            currentApp.CloseApp();
            currentApp = null;
            
            // If we navigated here from another app, go back to that app
            if (appHistory.Count > 0)
            {
                currentApp = appHistory.Pop();
                currentApp.OpenApp();
            }
        }
    }

    public void OnRecentsButtonClicked()
    {
        if (recentsViewUI == null) return;
        
        bool isOpening = !recentsViewUI.activeSelf;
        
        if (isOpening)
        {
            StopAllCoroutines();
            StartCoroutine(OpenRecentsRoutine()); // Start the sequence!
        }
        else if (currentApp != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateRecentsUI(false));
            currentApp.OpenApp(); 
        }
    }

private IEnumerator OpenRecentsRoutine()
    {
        // 1. Tell the app to take a picture
        if (currentApp != null)
        {
            currentApp.SuspendAppInstantly();
            
            // FIX: Force the Manager to wait 2 frames! 
            // This guarantees the app has fully saved the photo before we build the cards.
            yield return new WaitForEndOfFrame();
            yield return null; 
        }

        // 2. Start the smooth opening animation
        StartCoroutine(AnimateRecentsUI(true));

        // 3. Clear old cards
        foreach (Transform child in recentsContentContainer) Destroy(child.gameObject);

        // 4. Build the new cards
        foreach (AppWindow app in openAppsList.ToArray()) // Added .ToArray() for safety
        {
            AppWindow appToKill = app; // Creates a strict reference so the button doesn't get confused
            
            GameObject card = Instantiate(recentCardPrefab, recentsContentContainer);
            
            // --- SET THE SCREENSHOT ---
            if (appToKill.liveSnapshot != null)
            {
                card.GetComponent<UnityEngine.UI.Image>().color = Color.white;
                card.GetComponent<UnityEngine.UI.Image>().sprite = appToKill.liveSnapshot;
            }
            else
            {
                card.GetComponent<UnityEngine.UI.Image>().color = appToKill.appBackgroundColor;
            }
            
            // --- SET THE UNIQUE APP ICON ---
            Transform iconTransform = card.transform.Find("AppIcon");
            if (iconTransform != null)
            {
                iconTransform.gameObject.SetActive(true); 
                iconTransform.GetComponent<UnityEngine.UI.Image>().sprite = appToKill.appIcon; 
            }
            
            // --- APPLY SWIPE TO KILL ---
            SwipeToCloseCard swipeScript = card.GetComponent<SwipeToCloseCard>();
            if (swipeScript != null)
            {
                swipeScript.Setup(() => 
                {
                    openAppsList.Remove(appToKill); // Erase from memory list
                    appToKill.gameObject.SetActive(false); // Shut off the app window
                    
                    // If we just killed the app we were currently looking at, clear it!
                    if (currentApp == appToKill) currentApp = null; 
                });
            }
            
            // --- MAKE CLICKABLE ---
            card.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => 
            {
                StopAllCoroutines();
                StartCoroutine(AnimateRecentsUI(false));
                LaunchApplication(appToKill);
            });
        }
    }

    // --- SMOOTH ANIMATION COROUTINE ---
    private IEnumerator AnimateRecentsUI(bool open)
    {
        float speed = 15f; // Matches your AppWindow speed
        Vector3 targetScale = open ? Vector3.one : Vector3.zero;
        
        if (open)
        {
            recentsViewUI.SetActive(true);
            recentsViewUI.transform.localScale = Vector3.zero; // Start tiny
        }

        // Animate the scale
        while (Vector3.Distance(recentsViewUI.transform.localScale, targetScale) > 0.01f)
        {
            recentsViewUI.transform.localScale = Vector3.Lerp(recentsViewUI.transform.localScale, targetScale, Time.deltaTime * speed);
            yield return null;
        }
        
        recentsViewUI.transform.localScale = targetScale;
        
        // Hide completely if we are closing it
        if (!open)
        {
            recentsViewUI.SetActive(false);
        }
    }
}