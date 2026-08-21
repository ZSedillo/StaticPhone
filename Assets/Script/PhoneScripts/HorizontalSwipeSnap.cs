using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class HorizontalSwipeSnap : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Swipe Settings")]
    [Tooltip("Total number of home screen pages.")]
    public int totalPages = 3;
    
    [Tooltip("How many pixels you must drag to trigger a page turn.")]
    public float swipeThreshold = 50f; 
    
    [Tooltip("How fast the screen snaps into place after letting go.")]
    public float snapSpeed = 15f;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private float dragOffsetX;
    private int currentPage = 0;
    private float pageWidth;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent.GetComponent<RectTransform>();
        
        // The script automatically assumes the width of one page is the width of your HomeScreen mask
        pageWidth = parentRect.rect.width; 
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        StopAllCoroutines();
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPointerPosition);
            
        dragOffsetX = rectTransform.anchoredPosition.x - localPointerPosition.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPointerPosition))
        {
            // Move horizontally, keep the Y position locked
            float newX = localPointerPosition.x + dragOffsetX;
            rectTransform.anchoredPosition = new Vector2(newX, rectTransform.anchoredPosition.y);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Calculate how far we dragged from the current page's center
        float difference = rectTransform.anchoredPosition.x - (-currentPage * pageWidth);

        // If we dragged far enough to the Left (negative difference)
        if (difference < -swipeThreshold && currentPage < totalPages - 1)
        {
            currentPage++; // Move to next page
        }
        // If we dragged far enough to the Right (positive difference)
        else if (difference > swipeThreshold && currentPage > 0)
        {
            currentPage--; // Move to previous page
        }

        // Snap smoothly to whichever page is now active
        StartCoroutine(SnapToPage(currentPage));
    }

    private IEnumerator SnapToPage(int pageIndex)
    {
        // Calculate the exact X position the container should rest at
        float targetX = -pageIndex * pageWidth;
        Vector2 targetPosition = new Vector2(targetX, rectTransform.anchoredPosition.y);

        while (Mathf.Abs(rectTransform.anchoredPosition.x - targetX) > 0.5f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition, 
                targetPosition, 
                Time.deltaTime * snapSpeed
            );
            yield return null;
        }
        
        // Ensure it locks perfectly onto the pixel at the end
        rectTransform.anchoredPosition = targetPosition;
    }

    public void GoToHomePage()
    {
        // Stop any current swiping animations
        StopAllCoroutines(); 
        
        // Reset our tracker to Page 1 (which is index 0)
        currentPage = 0;
        
        // Trigger the smooth animation back to the start
        StartCoroutine(SnapToPage(currentPage));
    }
}