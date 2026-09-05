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

        // 1. Pull-down tray logic
        if (trayContentParent != null && notificationItemPrefab != null)
        {
            // Check if a card for this sender already exists in the tray
            ActiveNotificationData existingNotif = activeNotifications.Find(n =>
                n.senderName.Equals(senderName, StringComparison.OrdinalIgnoreCase));

            if (existingNotif != null && existingNotif.spawnedItem != null)
            {
                // Update existing card, bump it to the very top, and glow
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
                // Spawn a new card
                GameObject itemObj = Instantiate(notificationItemPrefab, trayContentParent);
                itemObj.transform.localScale = Vector3.one;
                itemObj.transform.localPosition = Vector3.zero;

                // Move new notification to the top of the tray
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

    private void OpenChatFromNotification(string girlName, int avatarIndex)
    {
        // 1. Reset the pull-down shade (NotificationContainer) back up
        if (trayContentParent != null)
        {
            // Find the root NotificationContainer (parent of Content)
            Transform containerTransform = trayContentParent.GetComponentInParent<NotificationSwipe>()?.transform 
                                           ?? trayContentParent.parent;

            if (containerTransform != null)
            {
                RectTransform containerRect = containerTransform.GetComponent<RectTransform>();
                if (containerRect != null)
                {
                    Vector2 pos = containerRect.anchoredPosition;
                    pos.y = 880f; // The closed Pos Y of the shade
                    containerRect.anchoredPosition = pos;
                }

                // If you have a NotificationSwipe script, reset its open state flag
                NotificationSwipe swipeScript = containerTransform.GetComponent<NotificationSwipe>();
                if (swipeScript != null)
                {
                    swipeScript.isOpen = false; // or swipeScript.CloseTray() if you have a method for it
                }
            }
        }

        // 2. Open app via homescreen button
        if (btnDatingAppIcon != null)
        {
            btnDatingAppIcon.onClick.Invoke();
        }
        else if (datingAppWindow != null)
        {
            datingAppWindow.SetActive(true);
        }

        // 3. Open Direct Chat Room
        if (directChatRoom != null)
        {
            Sprite avatar = (avatarIndex >= 0 && avatarIndex < avatarSprites.Count) ? avatarSprites[avatarIndex] : null;
            directChatRoom.OpenChatRoom(girlName, avatar);
            directChatRoom.transform.SetAsLastSibling();
        }

        // 4. Hide popup banner if it's currently showing
        if (topBanner != null)
        {
            topBanner.HideBanner();
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