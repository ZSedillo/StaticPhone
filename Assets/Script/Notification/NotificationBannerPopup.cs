using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class NotificationBannerPopup : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Elements")]
    [SerializeField] private Image avatarOrIcon;
    [SerializeField] private TextMeshProUGUI txtSender;
    [SerializeField] private TextMeshProUGUI txtPreview;
    [SerializeField] private Button btnBanner;

    [Header("Animation Settings")]
    [SerializeField] private float hiddenPosY = 200f;  // Above the screen/bezel
    [SerializeField] private float visiblePosY = -60f; // Dropped into view
    [SerializeField] private float animSpeed = 12f;
    [SerializeField] private float autoDismissSeconds = 3.5f;

    private RectTransform rectTransform;
    private Action onClickAction;
    private Coroutine dismissCoroutine;
    private Coroutine transitionCoroutine;
    private bool isDragging = false;
    private float targetY;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetY = hiddenPosY;
        SetY(hiddenPosY);

        if (btnBanner == null) btnBanner = GetComponent<Button>();
        if (btnBanner != null)
        {
            btnBanner.onClick.RemoveAllListeners();
            btnBanner.onClick.AddListener(OnBannerClicked);
        }
    }

public void Show(string title, string message, Sprite icon, Action onClick)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        onClickAction = onClick;
        gameObject.SetActive(true);

        if (dismissCoroutine != null)
        {
            StopCoroutine(dismissCoroutine);
            dismissCoroutine = null;
        }

        // If the banner is already displayed, animate it up quickly before dropping the new one
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        if (Mathf.Abs(rectTransform.anchoredPosition.y - visiblePosY) < 30f)
        {
            transitionCoroutine = StartCoroutine(CycleNewNotification(title, message, icon));
        }
        else
        {
            ApplyContent(title, message, icon);
            targetY = visiblePosY;
            dismissCoroutine = StartCoroutine(AutoDismissTimer());
        }
    }
    private IEnumerator CycleNewNotification(string title, string message, Sprite icon)
    {
        // 1. Slide back up out of sight
        targetY = hiddenPosY;
        while (Mathf.Abs(rectTransform.anchoredPosition.y - hiddenPosY) > 10f)
        {
            yield return null;
        }

        // 2. Change the text and avatar while hidden off-screen
        ApplyContent(title, message, icon);
        yield return new WaitForSeconds(0.05f);

        // 3. Drop down cleanly as a fresh notification
        targetY = visiblePosY;
        transitionCoroutine = null;
        dismissCoroutine = StartCoroutine(AutoDismissTimer());
    }

    private void ApplyContent(string title, string message, Sprite icon)
    {
        if (txtSender != null) txtSender.text = title;
        if (txtPreview != null) txtPreview.text = message;

        if (avatarOrIcon != null)
        {
            if (icon != null)
            {
                avatarOrIcon.sprite = icon;
                avatarOrIcon.color = Color.white;
            }
            else
            {
                avatarOrIcon.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    public void HideBanner()
    {
        targetY = hiddenPosY;
        if (dismissCoroutine != null)
        {
            StopCoroutine(dismissCoroutine);
            dismissCoroutine = null;
        }
    }

    private void Update()
    {
        if (!isDragging && rectTransform != null)
        {
            float currentY = rectTransform.anchoredPosition.y;
            if (Mathf.Abs(currentY - targetY) > 0.5f)
            {
                SetY(Mathf.Lerp(currentY, targetY, Time.deltaTime * animSpeed));
            }
            else
            {
                SetY(targetY);
                if (targetY == hiddenPosY && !isDragging)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }

    private IEnumerator AutoDismissTimer()
    {
        yield return new WaitForSeconds(autoDismissSeconds);
        HideBanner();
    }

    // --- Drag to dismiss ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        if (dismissCoroutine != null) StopCoroutine(dismissCoroutine);
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
    }

    public void OnDrag(PointerEventData eventData)
    {
        float newY = rectTransform.anchoredPosition.y + eventData.delta.y;
        if (newY < visiblePosY) newY = visiblePosY;
        SetY(newY);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        if (rectTransform.anchoredPosition.y > visiblePosY + 35f)
        {
            HideBanner();
        }
        else
        {
            targetY = visiblePosY;
            dismissCoroutine = StartCoroutine(AutoDismissTimer());
        }
    }

    private void SetY(float y)
    {
        Vector2 pos = rectTransform.anchoredPosition;
        pos.y = y;
        rectTransform.anchoredPosition = pos;
    }

    private void OnBannerClicked()
    {
        onClickAction?.Invoke();
        HideBanner();
    }
}