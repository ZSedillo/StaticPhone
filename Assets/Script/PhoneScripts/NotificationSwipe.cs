using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class NotificationSwipe : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Swipe Settings")]
    [Tooltip("Pixels you must drag DOWN to open the panel.")]
    public float openThreshold = 250f; 
    
    [Tooltip("Pixels you must drag UP to close the panel.")]
    public float closeThreshold = 200f; 
    
    [Tooltip("How fast you must flick up/down to trigger it instantly (Velocity).")]
    public float flickThreshold = 15f; 
    
    public float snapSpeed = 15f;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private float dragOffsetY;
    
    private float hiddenY; 
    private float openY = 0f; 
    public bool isOpen = false;
    
    // We use this to measure how fast your mouse is moving
    private float lastDragDeltaY; 

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent.GetComponent<RectTransform>();
        hiddenY = rectTransform.anchoredPosition.y; 
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        StopAllCoroutines();
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPointerPosition);
            
        dragOffsetY = rectTransform.anchoredPosition.y - localPointerPosition.y;
        lastDragDeltaY = 0f; // Reset speed when we grab it
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPointerPosition))
        {
            float newY = localPointerPosition.y + dragOffsetY;
            newY = Mathf.Clamp(newY, openY, hiddenY);
            
            // Track the velocity: Calculate the difference between the new position and the old position
            lastDragDeltaY = newY - rectTransform.anchoredPosition.y; 
            
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, newY);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isOpen)
        {
            // --- WE ARE TRYING TO CLOSE IT ---
            // Calculate how far UP we dragged it from the fully open position
            float draggedUpDistance = rectTransform.anchoredPosition.y - openY;

            // If we flicked it up really fast, OR we dragged it up past the pixel threshold
            if (lastDragDeltaY > flickThreshold || draggedUpDistance >= closeThreshold)
            {
                CloseNotification();
            }
            else
            {
                // Snap back down (stay open) because the user didn't swipe hard/far enough
                StartCoroutine(SnapTo(openY));
            }
        }
        else
        {
            // --- WE ARE TRYING TO OPEN IT ---
            // Calculate how far DOWN we dragged it from the hidden position
            float draggedDownDistance = hiddenY - rectTransform.anchoredPosition.y;

            // If we flicked it down really fast, OR we dragged it down past the pixel threshold
            if (lastDragDeltaY < -flickThreshold || draggedDownDistance >= openThreshold)
            {
                OpenNotification();
            }
            else
            {
                // Snap back up (stay closed)
                CloseNotification();
            }
        }
    }

    private IEnumerator SnapTo(float targetY)
    {
        Vector2 targetPosition = new Vector2(rectTransform.anchoredPosition.x, targetY);
        
        while (Mathf.Abs(rectTransform.anchoredPosition.y - targetY) > 0.5f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition, 
                targetPosition, 
                Time.deltaTime * snapSpeed
            );
            yield return null;
        }
        
        rectTransform.anchoredPosition = targetPosition;
    }

    // Extracted into its own function so the script can easily call it
    public void OpenNotification()
    {
        isOpen = true;
        StopAllCoroutines();
        StartCoroutine(SnapTo(openY));
    }

    // Your Nav Bar Manager still calls this to force it closed!
    public void CloseNotification()
    {
        isOpen = false;
        StopAllCoroutines();
        StartCoroutine(SnapTo(hiddenY));
    }
}