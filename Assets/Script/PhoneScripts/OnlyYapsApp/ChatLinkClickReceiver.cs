using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ChatLinkClickReceiver : MonoBehaviour, IPointerClickHandler
{
    private string targetLink;
    private Action onLinkClicked;

    public void Initialize(string link, Action onClick)
    {
        targetLink = link;
        onLinkClicked = onClick;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TMP_Text text = GetComponent<TMP_Text>();
        if (text == null) return;

        // Check if the click intersected with any <link> tag inside the TextMeshPro component
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, eventData.pressEventCamera);
        if (linkIndex != -1)
        {
            onLinkClicked?.Invoke();
        }
    }
}