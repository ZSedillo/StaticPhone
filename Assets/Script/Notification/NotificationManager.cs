using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("Notification Tray (Pull-down Container)")]
    [SerializeField] private Transform trayContentParent;
    [SerializeField] private GameObject notificationItemPrefab;

    [Header("Pop-up Heads-Up Banner")]
    [SerializeField] private NotificationBannerPopup topBanner;

    [Header("App & Chat Navigation - Dating App")]
    [Tooltip("The Dating App icon Button located on your HomeScreen")]
    [SerializeField] private Button btnDatingAppIcon;
    [SerializeField] private GameObject datingAppWindow;
    [SerializeField] private DirectChatRoomController directChatRoom;

    [Header("App Navigation - OnlyYaps")]
    [Tooltip("The OnlyYaps App icon Button located on your HomeScreen")]
    [SerializeField] private Button btnOnlyYapsAppIcon;
    [SerializeField] private GameObject onlyYapsAppWindow;

    [Header("Visual Assets")]
    [SerializeField] private List<Sprite> avatarSprites = new List<Sprite>();

    [Header("Text Settings")]
    [SerializeField] private int maxPreviewChars = 32;

    private string currentOpenChatGirlName = string.Empty;

    private class ActiveNotificationData
    {
        public string senderName;
        public string message;
        public string timestamp;
        public int avatarIdx;
        public GameObject spawnedItem;
    }

    private List<ActiveNotificationData> activeNotifications = new List<ActiveNotificationData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCurrentOpenChat(string girlName)
    {
        currentOpenChatGirlName = girlName;
        RemoveNotificationsFrom(girlName);
    }

    public void ClearCurrentOpenChat()
    {
        currentOpenChatGirlName = string.Empty;
    }

    public void TriggerNotification(string senderName, string message, int avatarIndex)
    {
        // Don't notify if the player is actively chatting with this character
        if (!string.IsNullOrEmpty(currentOpenChatGirlName) &&
            currentOpenChatGirlName.Equals(senderName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string shortPreview = message;
        if (!string.IsNullOrEmpty(message) && message.Length > maxPreviewChars)
        {
            shortPreview = message.Substring(0, maxPreviewChars).TrimEnd() + "...";
        }

        string time = DateTime.Now.ToString("h:mm tt");
        Sprite avatar = (avatarIndex >= 0 && avatarIndex < avatarSprites.Count) ? avatarSprites[avatarIndex] : null;

        // 1. Pull-down tray logic
        if (trayContentParent != null && notificationItemPrefab != null)
        {
            ActiveNotificationData existingNotif = activeNotifications.Find(n =>
                n.senderName.Equals(senderName, StringComparison.OrdinalIgnoreCase));

            if (existingNotif != null && existingNotif.spawnedItem != null)
            {
                existingNotif.message = shortPreview;
                existingNotif.timestamp = time;

                existingNotif.spawnedItem.transform.SetAsFirstSibling();

                NotificationItemUI existingUI = existingNotif.spawnedItem.GetComponent<NotificationItemUI>();
                if (existingUI != null)
                {
                    existingUI.Setup(
                        senderName,
                        shortPreview,
                        time,
                        avatar,
                        onClick: () => OpenChatFromNotification(senderName, avatarIndex),
                        onDismiss: () => DismissNotification(existingNotif)
                    );
                }
            }
            else
            {
                GameObject itemObj = Instantiate(notificationItemPrefab, trayContentParent);
                itemObj.transform.localScale = Vector3.one;
                itemObj.transform.localPosition = Vector3.zero;

                itemObj.transform.SetAsFirstSibling();

                NotificationItemUI itemUI = itemObj.GetComponent<NotificationItemUI>();

                ActiveNotificationData notifData = new ActiveNotificationData
                {
                    senderName = senderName,
                    message = shortPreview,
                    timestamp = time,
                    avatarIdx = avatarIndex,
                    spawnedItem = itemObj
                };

                activeNotifications.Add(notifData);

                if (itemUI != null)
                {
                    itemUI.Setup(
                        senderName,
                        shortPreview,
                        time,
                        avatar,
                        onClick: () => OpenChatFromNotification(senderName, avatarIndex),
                        onDismiss: () => DismissNotification(notifData)
                    );
                }
            }
        }
        else
        {
            Debug.LogWarning("[NotificationManager] TrayContentParent or NotificationItemPrefab is missing on PhoneContainer!");
        }

        // 2. Display top pop-up banner
        if (topBanner != null)
        {
            topBanner.Show(
                senderName,
                shortPreview,
                avatar,
                onClick: () => OpenChatFromNotification(senderName, avatarIndex)
            );
        }
    }

    private void OpenChatFromNotification(string senderName, int avatarIndex)
    {
        // 1. Reset pull-down shade back up if open
        if (trayContentParent != null)
        {
            Transform containerTransform = trayContentParent.GetComponentInParent<NotificationSwipe>()?.transform 
                                           ?? trayContentParent.parent;

            if (containerTransform != null)
            {
                RectTransform containerRect = containerTransform.GetComponent<RectTransform>();
                if (containerRect != null)
                {
                    Vector2 pos = containerRect.anchoredPosition;
                    pos.y = 880f;
                    containerRect.anchoredPosition = pos;
                }

                NotificationSwipe swipeScript = containerTransform.GetComponent<NotificationSwipe>();
                if (swipeScript != null)
                {
                    swipeScript.isOpen = false;
                }
            }
        }

        // 2. Hide top banner if visible
        if (topBanner != null)
        {
            topBanner.HideBanner();
        }

        // 3. ONLYYAPS ROUTE: If the notification is from OnlyYaps, launch OnlyYaps
        if (senderName.Equals("OnlyYaps", StringComparison.OrdinalIgnoreCase))
        {
            if (datingAppWindow != null) datingAppWindow.SetActive(false);

            if (btnOnlyYapsAppIcon != null)
            {
                btnOnlyYapsAppIcon.onClick.Invoke();
            }
            else if (onlyYapsAppWindow != null)
            {
                onlyYapsAppWindow.SetActive(true);
            }

            RemoveNotificationsFrom(senderName);
            return;
        }

        // 4. DATING APP ROUTE: Launch Dating App and navigate to partner's direct chat
        if (btnDatingAppIcon != null)
        {
            btnDatingAppIcon.onClick.Invoke();
        }
        else if (datingAppWindow != null)
        {
            datingAppWindow.SetActive(true);
        }

        if (directChatRoom != null)
        {
            Sprite avatar = (avatarIndex >= 0 && avatarIndex < avatarSprites.Count) ? avatarSprites[avatarIndex] : null;
            directChatRoom.OpenChatRoom(senderName, avatar);
            directChatRoom.transform.SetAsLastSibling();
        }

        RemoveNotificationsFrom(senderName);
    }

    private void DismissNotification(ActiveNotificationData notif)
    {
        if (notif.spawnedItem != null) Destroy(notif.spawnedItem);
        activeNotifications.Remove(notif);
    }

    public void RemoveNotificationsFrom(string senderName)
    {
        for (int i = activeNotifications.Count - 1; i >= 0; i--)
        {
            if (activeNotifications[i].senderName.Equals(senderName, StringComparison.OrdinalIgnoreCase))
            {
                if (activeNotifications[i].spawnedItem != null) Destroy(activeNotifications[i].spawnedItem);
                activeNotifications.RemoveAt(i);
            }
        }
    }

    public void ClearAllNotifications()
    {
        foreach (var notif in activeNotifications)
        {
            if (notif.spawnedItem != null) Destroy(notif.spawnedItem);
        }
        activeNotifications.Clear();

        if (topBanner != null) topBanner.HideBanner();
    }
}