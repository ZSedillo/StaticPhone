using UnityEngine;
using UnityEngine.UI;

public class RecentsCardScroller : MonoBehaviour
{
    [Header("Deck Stacking Visuals")]
    public float minScale = 0.85f;
    public float maxScale = 1.0f;
    public float scaleDistance = 200f;

    private RectTransform viewportRect;
    private ScrollRect parentScrollRect;

    void Awake()
    {
        parentScrollRect = GetComponentInParent<ScrollRect>();
        if (parentScrollRect != null && parentScrollRect.viewport != null)
        {
            viewportRect = parentScrollRect.viewport;
            parentScrollRect.onValueChanged.AddListener(OnScroll);
        }
    }

    void OnDestroy()
    {
        if (parentScrollRect != null)
        {
            parentScrollRect.onValueChanged.RemoveListener(OnScroll);
        }
    }

    public void RefreshCardScales()
    {
        OnScroll(Vector2.zero);
    }

    private void OnScroll(Vector2 scrollPos)
    {
        if (viewportRect == null || transform.childCount == 0) return;

        Vector3 viewportCenter = viewportRect.position;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform card = transform.GetChild(i);
            
            // Distance from card center to viewport center along X
            float distance = Mathf.Abs(card.position.x - viewportCenter.x);
            float t = Mathf.Clamp01(distance / scaleDistance);
            float targetScale = Mathf.Lerp(maxScale, minScale, t);

            if (Mathf.Abs(card.localScale.x - targetScale) > 0.005f)
            {
                card.localScale = new Vector3(targetScale, targetScale, 1f);
            }
        }
    }
}