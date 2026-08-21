using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class SwipeToUnlock : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Swipe Settings")]
    public float unlockThreshold = 300f; 
    public float snapSpeed = 15f; 
    public float unlockSpeed = 20f;

    private RectTransform rectTransform;
    private RectTransform parentRect; // Cached parent RectTransform
    private Vector2 initialPosition;
    private bool isUnlocked = false;
    private float dragOffsetY; 

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Safely grab the parent's RectTransform without a hard cast
        parentRect = rectTransform.parent.GetComponent<RectTransform>();
        
        if (parentRect == null)
        {
            Debug.LogError("SwipeToUnlock Error: The parent object does not have a RectTransform! Please ensure the parent is a UI element.");
        }

        initialPosition = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isUnlocked || parentRect == null) return;
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
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isUnlocked || parentRect == null) return;

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