using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;

[RequireComponent(typeof(LayoutElement))]
public class SwipeToCloseCard : MonoBehaviour, 
    IInitializePotentialDragHandler, 
    IPointerDownHandler, 
    IPointerUpHandler, 
    IBeginDragHandler, 
    IDragHandler, 
    IEndDragHandler
{
    [Header("Swipe Settings")]
    public float flickVelocityThreshold = 10f; 
    public float swipeDistanceThreshold = 150f;
    public float clickMaxMovement = 15f;

    private Action killAppAction;
    private Action clickAction;

    private RectTransform rect;
    private ScrollRect parentScrollRect;
    private LayoutElement layoutElement;

    private bool isSwipingUp = false;
    private bool isDraggingHorizontally = false;
    private Vector2 pointerDownPosition;
    private float startY;
    private float lastDragDeltaY;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        parentScrollRect = GetComponentInParent<ScrollRect>();
        layoutElement = GetComponent<LayoutElement>();
    }

    public void Setup(Action onKill, Action onClick)
    {
        killAppAction = onKill;
        clickAction = onClick;
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null)
        {
            ExecuteEvents.Execute(parentScrollRect.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPosition = eventData.position;
        isSwipingUp = false;
        isDraggingHorizontally = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // If neither vertical nor horizontal drag took place and distance was small, it's a tap
        if (!isSwipingUp && !isDraggingHorizontally)
        {
            if (Vector2.Distance(pointerDownPosition, eventData.position) < clickMaxMovement)
            {
                clickAction?.Invoke();
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Check if movement is primarily UPWARDS
        if (eventData.delta.y > 0 && Mathf.Abs(eventData.delta.y) > Mathf.Abs(eventData.delta.x))
        {
            isSwipingUp = true;
            isDraggingHorizontally = false;

            Vector2 lockedPosition = rect.anchoredPosition;
            layoutElement.ignoreLayout = true; 
            rect.anchoredPosition = lockedPosition;

            startY = rect.anchoredPosition.y;
            lastDragDeltaY = 0f;
        }
        else
        {
            // Horizontal scroll across the ScrollRect
            isSwipingUp = false;
            isDraggingHorizontally = true;

            if (parentScrollRect != null)
            {
                ExecuteEvents.Execute(parentScrollRect.gameObject, eventData, ExecuteEvents.beginDragHandler);
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isSwipingUp)
        {
            float newY = rect.anchoredPosition.y + eventData.delta.y;
            if (newY > startY)
            {
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, newY);
                lastDragDeltaY = eventData.delta.y;
            }
        }
        else if (isDraggingHorizontally)
        {
            if (parentScrollRect != null)
            {
                ExecuteEvents.Execute(parentScrollRect.gameObject, eventData, ExecuteEvents.dragHandler);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isSwipingUp)
        {
            if (lastDragDeltaY > flickVelocityThreshold || (rect.anchoredPosition.y - startY) > swipeDistanceThreshold)
            {
                killAppAction?.Invoke();
                Destroy(gameObject);
            }
            else
            {
                StartCoroutine(SnapBack());
            }
        }
        else if (isDraggingHorizontally)
        {
            if (parentScrollRect != null)
            {
                ExecuteEvents.Execute(parentScrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
            }
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