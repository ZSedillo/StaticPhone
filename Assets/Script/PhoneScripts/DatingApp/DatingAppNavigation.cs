using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DatingAppNavigation : MonoBehaviour
{
    [System.Serializable]
    public class NavTab
    {
        public string tabName;
        public GameObject panel;
        public Button navButton;
    }

    [Header("Navigation Tabs (0: Profile, 1: Explore, 2: Likes, 3: Chats)")]
    public List<NavTab> tabs = new List<NavTab>();

    [Header("Icon Tint Colors")]
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(1f, 1f, 1f, 0.4f);

    [Header("Transition Settings")]
    public float fadeDuration = 0.15f;

    private int currentTabIndex = -1;
    private Coroutine activeTransitionCoroutine;

    void Start()
    {
        // Wire up button click events
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i;
            if (tabs[i].navButton != null)
            {
                tabs[i].navButton.onClick.AddListener(() => SwitchTab(index));
            }
        }

        // Default to Explore tab (Index 1) on launch
        SwitchTab(1, instant: true);
    }

    public void SwitchTab(int targetIndex)
    {
        SwitchTab(targetIndex, false);
    }

    public void SwitchTab(int targetIndex, bool instant)
    {
        if (targetIndex == currentTabIndex || targetIndex < 0 || targetIndex >= tabs.Count) return;

        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }

        if (instant)
        {
            ApplyInstantSwitch(targetIndex);
        }
        else
        {
            activeTransitionCoroutine = StartCoroutine(TransitionToTab(targetIndex));
        }

        currentTabIndex = targetIndex;
        UpdateNavVisuals(currentTabIndex);
    }

    private void ApplyInstantSwitch(int targetIndex)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            if (tabs[i].panel != null)
            {
                bool isTarget = (i == targetIndex);
                tabs[i].panel.SetActive(isTarget);

                CanvasGroup cg = GetOrAddCanvasGroup(tabs[i].panel);
                cg.alpha = isTarget ? 1f : 0f;
            }
        }
    }

    private IEnumerator TransitionToTab(int targetIndex)
    {
        GameObject currentPanel = (currentTabIndex >= 0 && currentTabIndex < tabs.Count) ? tabs[currentTabIndex].panel : null;
        GameObject targetPanel = tabs[targetIndex].panel;

        CanvasGroup currentCg = currentPanel != null ? GetOrAddCanvasGroup(currentPanel) : null;
        CanvasGroup targetCg = targetPanel != null ? GetOrAddCanvasGroup(targetPanel) : null;

        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
            if (targetCg != null) targetCg.alpha = 0f;
        }

        // Smooth Crossfade
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            if (currentCg != null) currentCg.alpha = 1f - t;
            if (targetCg != null) targetCg.alpha = t;

            yield return null;
        }

        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            if (currentCg != null) currentCg.alpha = 0f;
        }

        if (targetCg != null) targetCg.alpha = 1f;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = obj.AddComponent<CanvasGroup>();
        }
        return cg;
    }

    private void UpdateNavVisuals(int activeIndex)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            if (tabs[i].navButton != null)
            {
                Image iconImage = tabs[i].navButton.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.color = (i == activeIndex) ? activeTabColor : inactiveTabColor;
                }
            }
        }
    }
}