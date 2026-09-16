using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtDescription;
    [SerializeField] private Image checkboxImage;

    [Header("Colors")]
    [SerializeField] private Color completedTextColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    [SerializeField] private Color activeTitleColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color completedBoxColor = new Color(0.2f, 0.85f, 0.3f, 1f); // Green
    [SerializeField] private Color uncompletedBoxColor = new Color(0.25f, 0.25f, 0.25f, 1f); // Dark Gray

    public void Setup(TaskData task)
    {
        if (txtTitle != null)
        {
            txtTitle.text = task.title;
            if (task.isCompleted)
            {
                txtTitle.color = completedTextColor;
                txtTitle.fontStyle = FontStyles.Strikethrough;
            }
            else
            {
                txtTitle.color = task.isMainStory ? activeTitleColor : Color.white;
                txtTitle.fontStyle = FontStyles.Bold;
            }
        }

        if (txtDescription != null)
        {
            txtDescription.text = task.description;
            txtDescription.color = task.isCompleted ? completedTextColor : new Color(0.85f, 0.85f, 0.85f, 1f);
        }

        if (checkboxImage != null)
        {
            checkboxImage.color = task.isCompleted ? completedBoxColor : uncompletedBoxColor;
        }

        // --- FORCE LAYOUT RECALCULATION FOR MULTI-LINE DESCRIPTIONS ---
        if (txtDescription != null)
        {
            txtDescription.ForceMeshUpdate();
        }

        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}