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

    [Header("App & Chat Navigation")]
    [Tooltip("The Dating App icon Button located on your HomeScreen")]
    [SerializeField] private Button btnDatingAppIcon;
    [SerializeField] private GameObject datingAppWindow;
    [SerializeField] private DirectChatRoomController directChatRoom;
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

        // 1. Spawn into pull-down tray
        if (trayContentParent != null && notificationItemPrefab != null)
        {
            GameObject itemObj = Instantiate(notificationItemPrefab, trayContentParent);
            itemObj.transform.localScale = Vector3.one;
            itemObj.transform.localPosition = Vector3.zero;

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

    private void OpenChatFromNotification(string girlName, int avatarIndex)
    {
        // 1. Dismiss pull-down shade tray if it is currently open
        if (trayContentParent != null)
        {
            trayContentParent.gameObject.SetActive(false);
        }

        // 2. Launch the Dating App through its standard homescreen button
        if (btnDatingAppIcon != null)
        {
            btnDatingAppIcon.onClick.Invoke();
        }
        else if (datingAppWindow != null)
        {
            // Fallback: Enable and reset transform in case button isn't linked
            datingAppWindow.SetActive(true);
            RectTransform rect = datingAppWindow.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }
            datingAppWindow.transform.SetAsLastSibling();
        }

        // 3. Open Direct Chat Room directly to this match
        if (directChatRoom != null)
        {
            Sprite avatar = (avatarIndex >= 0 && avatarIndex < avatarSprites.Count) ? avatarSprites[avatarIndex] : null;
            directChatRoom.OpenChatRoom(girlName, avatar);
            directChatRoom.transform.SetAsLastSibling();
        }

        RemoveNotificationsFrom(girlName);
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