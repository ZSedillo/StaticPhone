using UnityEngine;
using UnityEngine.UI;

public class DatingAppNavigation : MonoBehaviour
{
    [Header("Panels")]
    public GameObject exploreViewPanel;
    public GameObject likesViewPanel;
    public GameObject chatsViewPanel;
    public GameObject profileViewPanel;

    [Header("Navigation Buttons")]
    public Button navBtnProfile;
    public Button navBtnExplore;
    public Button navBtnLikes;
    public Button navBtnChats;

    [Header("Colors")]
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(1f, 1f, 1f, 0.4f);

    void Start()
    {
        if (navBtnProfile != null) navBtnProfile.onClick.AddListener(() => SwitchTab(0));
        if (navBtnExplore != null) navBtnExplore.onClick.AddListener(() => SwitchTab(1));
        if (navBtnLikes != null) navBtnLikes.onClick.AddListener(() => SwitchTab(2));
        if (navBtnChats != null) navBtnChats.onClick.AddListener(() => SwitchTab(3));

        // Default directly to Explore/Discover screen
        SwitchTab(1);
    }

    public void SwitchTab(int tabIndex)
    {
        // 0 = Profile, 1 = Explore, 2 = Likes, 3 = Chats
        if (profileViewPanel != null) profileViewPanel.SetActive(tabIndex == 0);
        if (exploreViewPanel != null) exploreViewPanel.SetActive(tabIndex == 1);
        if (likesViewPanel != null) likesViewPanel.SetActive(tabIndex == 2);
        if (chatsViewPanel != null) chatsViewPanel.SetActive(tabIndex == 3);

        UpdateNavVisuals(tabIndex);
    }

    private void UpdateNavVisuals(int activeIndex)
    {
        SetTint(navBtnProfile, activeIndex == 0);
        SetTint(navBtnExplore, activeIndex == 1);
        SetTint(navBtnLikes, activeIndex == 2);
        SetTint(navBtnChats, activeIndex == 3);
    }

    private void SetTint(Button btn, bool isActive)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = isActive ? activeTabColor : inactiveTabColor;
    }
}