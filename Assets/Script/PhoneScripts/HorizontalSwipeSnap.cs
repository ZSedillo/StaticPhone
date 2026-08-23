using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class HorizontalSwipeSnap : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    [Header("Swipe Settings")]
    [Tooltip("Total number of home screen pages.")]
    public int totalPages = 3;
    
    [Tooltip("How many pixels you must drag horizontally to trigger a page turn.")]
    public float swipeThreshold = 50f; 
    
    [Tooltip("How fast the screen snaps into place after letting go.")]
    public float snapSpeed = 15f;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private float dragOffsetX;
    private int currentPage = 0;
    private float pageWidth;
    
    // Tracks if we are currently dragging horizontally
    private bool isDraggingHorizontal = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent.GetComponent<RectTransform>();
        pageWidth = parentRect.rect.width; 
    }

    // Crucial: Tells Unity's event system to initialize drag smoothly without blocking clicks
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        StopAllCoroutines();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Check if the user is swiping more horizontally than vertically
        if (Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y))
        {
            isDraggingHorizontal = true;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, 
                eventData.position, 
                eventData.pressEventCamera, 
                out Vector2 localPointerPosition);
                
            dragOffsetX = rectTransform.anchoredPosition.x - localPointerPosition.x;
        }
        else
        {
            isDraggingHorizontal = false; // Let the click/long-press pass through to the app icons!
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingHorizontal) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPointerPosition))
        {
            float newX = localPointerPosition.x + dragOffsetX;
            rectTransform.anchoredPosition = new Vector2(newX, rectTransform.anchoredPosition.y);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingHorizontal) return;

        float difference = rectTransform.anchoredPosition.x - (-currentPage * pageWidth);

        if (difference < -swipeThreshold && currentPage < totalPages - 1)
        {
            currentPage++; 
        }
        else if (difference > swipeThreshold && currentPage > 0)
        {
            currentPage--; 
        }

        StartCoroutine(SnapToPage(currentPage));
    }

    private IEnumerator SnapToPage(int pageIndex)
    {
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
        
        rectTransform.anchoredPosition = targetPosition;
    }

    public void GoToHomePage()
    {
        StopAllCoroutines(); 
        currentPage = 0;
        StartCoroutine(SnapToPage(currentPage));
    }
}