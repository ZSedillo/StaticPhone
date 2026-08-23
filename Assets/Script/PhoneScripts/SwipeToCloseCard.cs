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
    private float startLocalY;
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
        // Detect vertical swipe intent
        if (eventData.delta.y > 0 && Mathf.Abs(eventData.delta.y) > Mathf.Abs(eventData.delta.x))
        {
            isSwipingUp = true;
            isDraggingHorizontally = false;
            startLocalY = rect.localPosition.y;
            lastDragDeltaY = 0f;
        }
        else
        {
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
            // Move card via localPosition so HorizontalLayoutGroup spacing stays intact while dragging
            float newY = rect.localPosition.y + eventData.delta.y;
            if (newY >= startLocalY)
            {
                rect.localPosition = new Vector3(rect.localPosition.x, newY, rect.localPosition.z);
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
            float totalDisplacement = rect.localPosition.y - startLocalY;

            if (lastDragDeltaY > flickVelocityThreshold || totalDisplacement > swipeDistanceThreshold)
            {
                StartCoroutine(DismissAndKill());
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

    private IEnumerator DismissAndKill()
    {
        // Animate offscreen upwards before destroying
        Vector3 targetPos = new Vector3(rect.localPosition.x, rect.localPosition.y + 600f, rect.localPosition.z);
        while (Vector3.Distance(rect.localPosition, targetPos) > 10f)
        {
            rect.localPosition = Vector3.Lerp(rect.localPosition, targetPos, Time.deltaTime * 20f);
            yield return null;
        }

        killAppAction?.Invoke();
        Destroy(gameObject);
    }

    private IEnumerator SnapBack()
    {
        Vector3 targetPos = new Vector3(rect.localPosition.x, startLocalY, rect.localPosition.z);
        while (Vector3.Distance(rect.localPosition, targetPos) > 1f)
        {
            rect.localPosition = Vector3.Lerp(rect.localPosition, targetPos, Time.deltaTime * 15f);
            yield return null;
        }
        rect.localPosition = targetPos;
    }
}