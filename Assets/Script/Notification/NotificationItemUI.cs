using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class NotificationItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image avatarOrAppIcon;
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtMessage;
    [SerializeField] private TextMeshProUGUI txtTimestamp;
    [SerializeField] private Button btnCardClick;
    [SerializeField] private Button btnDismiss;

    private Action onCardClicked;
    private Action onDismissed;

    public void Setup(string title, string message, string timestamp, Sprite icon, Action onClick, Action onDismiss)
    {
        if (txtTitle != null) txtTitle.text = title;
        if (txtMessage != null) txtMessage.text = message;
        if (txtTimestamp != null) txtTimestamp.text = timestamp;
        if (avatarOrAppIcon != null && icon != null) avatarOrAppIcon.sprite = icon;

        onCardClicked = onClick;
        onDismissed = onDismiss;

        if (btnCardClick != null)
        {
            btnCardClick.onClick.RemoveAllListeners();
            btnCardClick.onClick.AddListener(() => onCardClicked?.Invoke());
        }

        if (btnDismiss != null)
        {
            btnDismiss.onClick.RemoveAllListeners();
            btnDismiss.onClick.AddListener(() => onDismissed?.Invoke());
        }
    }
}