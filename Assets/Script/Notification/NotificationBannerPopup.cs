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
    [SerializeField] private float hiddenPosY = 200f; // Above the screen
    [SerializeField] private float visiblePosY = -60f; // Dropped into view
    [SerializeField] private float animSpeed = 8f;
    [SerializeField] private float autoDismissSeconds = 4f;

    private RectTransform rectTransform;
    private Action onClickAction;
    private Coroutine dismissCoroutine;
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

    private void OnBannerClicked()
    {
        onClickAction?.Invoke();
        HideBanner();
    }

    public void Show(string title, string message, Sprite icon, Action onClick)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        if (txtSender != null) txtSender.text = title;
        if (txtPreview != null) txtPreview.text = message;
        if (avatarOrIcon != null && icon != null) avatarOrIcon.sprite = icon;

        onClickAction = onClick;
        
        // Make sure it's active and in front of other panels
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        targetY = visiblePosY;

        if (dismissCoroutine != null) StopCoroutine(dismissCoroutine);
        dismissCoroutine = StartCoroutine(AutoDismissTimer());
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
        if (!isDragging)
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

    // --- Drag Up to Dismiss ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        if (dismissCoroutine != null) StopCoroutine(dismissCoroutine);
    }

    public void OnDrag(PointerEventData eventData)
    {
        float newY = rectTransform.anchoredPosition.y + eventData.delta.y;
        if (newY < visiblePosY) newY = visiblePosY; // Don't drag lower than visible
        SetY(newY);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        // If dragged upward past threshold, dismiss it
        if (rectTransform.anchoredPosition.y > visiblePosY + 30f)
        {
            HideBanner();
        }
        else
        {
            targetY = visiblePosY;
        }
    }

    private void SetY(float y)
    {
        Vector2 pos = rectTransform.anchoredPosition;
        pos.y = y;
        rectTransform.anchoredPosition = pos;
    }
}