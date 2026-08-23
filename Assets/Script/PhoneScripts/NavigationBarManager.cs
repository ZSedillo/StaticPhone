using UnityEngine;
using UnityEngine.UI;
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
            appHistory.Push(currentApp);
            currentApp.gameObject.SetActive(false);
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
        
        // 3. Smoothly animate Recents closed instead of abrupt disable
        if (recentsViewUI != null && recentsViewUI.activeSelf)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateRecentsUI(false));
        }

        // 4. Reset other OS panels
        if (notificationPanel != null && notificationPanel.gameObject.activeInHierarchy) 
        {
            notificationPanel.CloseNotification();
        }
        if (homeScreenSwiper != null) homeScreenSwiper.GoToHomePage();
    }
    public void OnBackButtonClicked()
    {
        if (recentsViewUI != null && recentsViewUI.activeSelf)
        {
            recentsViewUI.SetActive(false);
            if (currentApp != null) currentApp.OpenApp();
            return;
        }

        if (currentApp != null)
        {
            currentApp.CloseApp();
            currentApp = null;
            
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
            StartCoroutine(OpenRecentsRoutine());
        }
        else
        {
            // Close recents regardless of whether an app is active
            StopAllCoroutines();
            StartCoroutine(AnimateRecentsUI(false));
            
            // Reopen the current app if one was suspended
            if (currentApp != null)
            {
                currentApp.OpenApp(); 
            }
        }
    }

    private IEnumerator OpenRecentsRoutine()
    {
        // 1. Capture snapshot if an app is currently open
        if (currentApp != null)
        {
            currentApp.SuspendAppInstantly();
            yield return new WaitForEndOfFrame();
            yield return null; 
        }

        // 2. Animate Recents Panel
        StartCoroutine(AnimateRecentsUI(true));

        // 3. Clear old cards
        foreach (Transform child in recentsContentContainer) 
        {
            Destroy(child.gameObject);
        }

        // 4. Build cards for each active app
        foreach (AppWindow app in openAppsList.ToArray())
        {
            if (app == null) continue;

            AppWindow appToKill = app;
            GameObject card = Instantiate(recentCardPrefab, recentsContentContainer);
            
            // --- SET SNAPSHOT / BACKGROUND COLOR ---
            Image cardImage = card.GetComponent<Image>();
            if (cardImage != null)
            {
                if (appToKill.liveSnapshot != null)
                {
                    cardImage.color = Color.white;
                    cardImage.sprite = appToKill.liveSnapshot;
                }
                else
                {
                    cardImage.color = appToKill.appBackgroundColor;
                }
            }
            
            // --- SET APP ICON ---
            Transform iconTransform = card.transform.Find("AppIcon");
            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null && appToKill.appIcon != null)
                {
                    iconTransform.gameObject.SetActive(true);
                    iconImage.sprite = appToKill.appIcon;
                }
                else
                {
                    iconTransform.gameObject.SetActive(false);
                }
            }
            
            // --- ATTACH ACTIONS TO SWIPE SCRIPT ---
            SwipeToCloseCard swipeScript = card.GetComponent<SwipeToCloseCard>();
            if (swipeScript != null)
            {
                swipeScript.Setup(
                    () => { // Kill app
                        // 1. Remove from active background list
                        openAppsList.Remove(appToKill);

                        // 2. Wipe snapshot memory & deactivate window
                        appToKill.liveSnapshot = null;
                        appToKill.gameObject.SetActive(false);

                        // 3. Clear current app pointer if it was this app
                        if (currentApp == appToKill) currentApp = null;
                    },
                    () => { // Tap to reopen app
                        StopAllCoroutines();
                        StartCoroutine(AnimateRecentsUI(false));
                        LaunchApplication(appToKill);
                    }
                );
            }

        }
        StartCoroutine(CenterRecentsView());
    }

    private IEnumerator AnimateRecentsUI(bool open)
    {
        float speed = 15f;
        Vector3 targetScale = open ? Vector3.one : Vector3.zero;
        
        if (open)
        {
            recentsViewUI.SetActive(true);
            recentsViewUI.transform.localScale = Vector3.zero;
        }

        while (Vector3.Distance(recentsViewUI.transform.localScale, targetScale) > 0.01f)
        {
            recentsViewUI.transform.localScale = Vector3.Lerp(recentsViewUI.transform.localScale, targetScale, Time.deltaTime * speed);
            yield return null;
        }
        
        recentsViewUI.transform.localScale = targetScale;
        
        if (!open)
        {
            recentsViewUI.SetActive(false);
        }
    }
    private IEnumerator CenterRecentsView()
    {
        yield return new WaitForEndOfFrame();

        ScrollRect scrollRect = recentsViewUI.GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.horizontalNormalizedPosition = 0f;

            // Trigger scale calculation once cards are positioned
            RecentsCardScroller scroller = recentsContentContainer.GetComponent<RecentsCardScroller>();
            if (scroller != null)
            {
                scroller.RefreshCardScales();
            }
        }
    }
}