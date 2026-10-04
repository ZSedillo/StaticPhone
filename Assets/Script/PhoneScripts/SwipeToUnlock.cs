using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class SwipeToUnlock : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Swipe Settings")]
    public float unlockThreshold = 300f; 
    public float snapSpeed = 15f; 
    public float unlockSpeed = 20f;

    [Header("UI Elements")]
    [Tooltip("Drag the Text (TMP) object that says 'SWIPE UP' here.")]
    public CanvasGroup swipeTextGroup;

    private RectTransform rectTransform;
    private RectTransform parentRect; 
    private Vector2 initialPosition;
    private bool isUnlocked = false;
    private float dragOffsetY; 
    
    // Animation variables
    private float pulseSpeed = 2f;
    private float minAlpha = 0.3f;
    private bool isDragging = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent.GetComponent<RectTransform>();
        
        if (parentRect == null)
        {
            Debug.LogError("SwipeToUnlock Error: The parent object does not have a RectTransform!");
        }

        initialPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // Creates a smooth breathing/pulsing effect while idle
        if (!isUnlocked && !isDragging && swipeTextGroup != null)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f - minAlpha) + minAlpha;
            swipeTextGroup.alpha = pulse;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isUnlocked || parentRect == null) return;
        
        isDragging = true;
        StopAllCoroutines(); 

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPointerPosition);

        dragOffsetY = rectTransform.anchoredPosition.y - localPointerPosition.y;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isUnlocked || parentRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPointerPosition))
        {
            float newY = localPointerPosition.y + dragOffsetY;

            if (newY < initialPosition.y)
            {
                newY = initialPosition.y;
            }

            rectTransform.anchoredPosition = new Vector2(initialPosition.x, newY);

            // Fade out the text based on drag distance
            if (swipeTextGroup != null)
            {
                float draggedDistance = newY - initialPosition.y;
                float fadeRatio = 1f - (draggedDistance / (unlockThreshold * 0.75f));
                swipeTextGroup.alpha = Mathf.Clamp01(fadeRatio);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isUnlocked || parentRect == null) return;
        
        isDragging = false;

        float draggedDistance = rectTransform.anchoredPosition.y - initialPosition.y;
        
        if (draggedDistance >= unlockThreshold)
        {
            StartCoroutine(AnimateUnlock());
        }
        else
        {
            StartCoroutine(SnapBack());
        }
    }

    private IEnumerator SnapBack()
    {
        while (rectTransform.anchoredPosition.y > initialPosition.y + 1f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition, 
                initialPosition, 
                Time.deltaTime * snapSpeed
            );
            yield return null;
        }
        rectTransform.anchoredPosition = initialPosition;
    }

    private IEnumerator AnimateUnlock()
    {
        isUnlocked = true;
        
        // Ensure text is completely invisible upon unlock
        if (swipeTextGroup != null) swipeTextGroup.alpha = 0f;
        
        float targetY = initialPosition.y + 1500f; 
        Vector2 targetPosition = new Vector2(initialPosition.x, targetY);

        while (rectTransform.anchoredPosition.y < targetY - 10f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition, 
                targetPosition, 
                Time.deltaTime * unlockSpeed
            );
            yield return null;
        }
        
        gameObject.SetActive(false);
    }
}