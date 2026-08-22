using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;

[RequireComponent(typeof(LayoutElement))]
public class SwipeToCloseCard : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("How fast you must flick UP to kill the app (Velocity).")]
    public float flickVelocityThreshold = 10f; 
    [Tooltip("How far you must drag UP to kill the app (Distance backup).")]
    public float swipeDistanceThreshold = 150f;

    private Action killAppAction;
    private RectTransform rect;
    private ScrollRect parentScrollRect;
    private LayoutElement layoutElement;

    private bool isSwipingUp = false;
    private float startY;
    private float lastDragDeltaY;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        parentScrollRect = GetComponentInParent<ScrollRect>();
        layoutElement = GetComponent<LayoutElement>();
    }

    public void Setup(Action onKill)
    {
        killAppAction = onKill;
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null) parentScrollRect.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // If dragging mostly UP
        if (eventData.delta.y > 0 && eventData.delta.y > Mathf.Abs(eventData.delta.x))
        {
            isSwipingUp = true;
            
            // --- THE PERMANENT FIX ---
            // 1. Save the exact anchored position (e.g. X: 90) before touching the layout
            Vector2 lockedPosition = rect.anchoredPosition;
            
            // 2. Detach from layout
            layoutElement.ignoreLayout = true; 
            
            // 3. Instantly force it back to X: 90 so it never snaps to 0!
            rect.anchoredPosition = lockedPosition;
            
            startY = rect.anchoredPosition.y;
            lastDragDeltaY = 0f;
        }
        else
        {
            isSwipingUp = false;
            if (parentScrollRect != null) parentScrollRect.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isSwipingUp)
        {
            float newY = rect.anchoredPosition.y + eventData.delta.y;
            if (newY > startY) 
            {
                // Keep X strictly locked to whatever position it started at (e.g., 90)
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, newY);
                lastDragDeltaY = eventData.delta.y;
            }
        }
        else
        {
            if (parentScrollRect != null) parentScrollRect.OnDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isSwipingUp)
        {
            if (lastDragDeltaY > flickVelocityThreshold || (rect.anchoredPosition.y - startY) > swipeDistanceThreshold)
            {
                if (killAppAction != null) killAppAction.Invoke();
                Destroy(gameObject);
            }
            else
            {
                StartCoroutine(SnapBack());
            }
        }
        else
        {
            if (parentScrollRect != null) parentScrollRect.OnEndDrag(eventData);
        }
    }

    private IEnumerator SnapBack()
    {
        Vector2 targetPosition = new Vector2(rect.anchoredPosition.x, startY);
        while (Vector2.Distance(rect.anchoredPosition, targetPosition) > 0.5f)
        {
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, Time.deltaTime * 15f);
            yield return null;
        }
        rect.anchoredPosition = targetPosition;
        layoutElement.ignoreLayout = false; 
    }
}