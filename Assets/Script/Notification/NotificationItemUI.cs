using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotificationItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image cardBackgroundImage;
    [SerializeField] private Image avatarOrAppIcon;
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtMessage;
    [SerializeField] private TextMeshProUGUI txtTimestamp;
    [SerializeField] private Button btnCardClick;
    [SerializeField] private Button btnDismiss;

    [Header("Glow Settings")]
    [SerializeField] private Color glowColor = new Color(1f, 0.92f, 0.65f, 1f); // Warm yellow/gold highlight
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private float glowDuration = 2.5f;

    private Coroutine glowCoroutine;

    private void Awake()
    {
        if (cardBackgroundImage == null)
            cardBackgroundImage = GetComponent<Image>();
    }

    public void Setup(string title, string message, string time, Sprite icon, Action onClick, Action onDismiss)
    {
        if (txtTitle != null) txtTitle.text = title;
        if (txtMessage != null) txtMessage.text = message;
        if (txtTimestamp != null) txtTimestamp.text = time;

        if (avatarOrAppIcon != null)
        {
            if (icon != null)
            {
                avatarOrAppIcon.sprite = icon;
                avatarOrAppIcon.color = Color.white;
            }
            else
            {
                avatarOrAppIcon.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        if (btnCardClick != null)
        {
            btnCardClick.onClick.RemoveAllListeners();
            btnCardClick.onClick.AddListener(() => onClick?.Invoke());
        }

        if (btnDismiss != null)
        {
            btnDismiss.onClick.RemoveAllListeners();
            btnDismiss.onClick.AddListener(() => onDismiss?.Invoke());
        }

        TriggerGlow();
    }

    public void TriggerGlow()
    {
        if (cardBackgroundImage == null) return;
        if (glowCoroutine != null) StopCoroutine(glowCoroutine);
        glowCoroutine = StartCoroutine(GlowRoutine());
    }

    private IEnumerator GlowRoutine()
    {
        cardBackgroundImage.color = glowColor;
        float elapsed = 0f;
        while (elapsed < glowDuration)
        {
            elapsed += Time.deltaTime;
            cardBackgroundImage.color = Color.Lerp(glowColor, normalColor, elapsed / glowDuration);
            yield return null;
        }
        cardBackgroundImage.color = normalColor;
        glowCoroutine = null;
    }
}