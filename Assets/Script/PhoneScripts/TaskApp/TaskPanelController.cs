using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskPanelController : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject taskPanelRoot;

    [Header("Buttons")]
    [Tooltip("The Task icon button inside Page 2 of the phone")]
    [SerializeField] private Button btnOpenTaskPanel;

    [Tooltip("The 'X' or back button to close the panel")]
    [SerializeField] private Button btnCloseTaskPanel;

    [Header("List Parents")]
    [SerializeField] private RectTransform mainStoryContentParent;
    [SerializeField] private RectTransform optionalContentParent;
    [SerializeField] private GameObject taskItemPrefab;

    private void Awake()
    {
        if (btnOpenTaskPanel != null)
        {
            btnOpenTaskPanel.onClick.AddListener(OpenTaskPanel);
        }

        if (btnCloseTaskPanel != null)
        {
            btnCloseTaskPanel.onClick.AddListener(CloseTaskPanel);
        }
    }

    private void Start()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnTasksUpdated += RefreshTaskList;
        }

        if (taskPanelRoot != null)
        {
            taskPanelRoot.SetActive(false); // Starts hidden
        }

        RefreshTaskList();
    }

    private void OnDestroy()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnTasksUpdated -= RefreshTaskList;
        }
    }

    public void OpenTaskPanel()
    {
        if (taskPanelRoot != null)
        {
            taskPanelRoot.SetActive(true);
            RefreshTaskList();
        }
    }

    public void CloseTaskPanel()
    {
        if (taskPanelRoot != null)
        {
            taskPanelRoot.SetActive(false);
        }
    }

    public void RefreshTaskList()
    {
        if (TaskManager.Instance == null || taskItemPrefab == null) return;

        // 1. Destroy old clones
        if (mainStoryContentParent != null)
        {
            for (int i = mainStoryContentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(mainStoryContentParent.GetChild(i).gameObject);
            }
        }

        if (optionalContentParent != null)
        {
            for (int i = optionalContentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(optionalContentParent.GetChild(i).gameObject);
            }
        }

        // 2. Instantiate new task cards
        foreach (TaskData task in TaskManager.Instance.allTasks)
        {
            RectTransform parent = task.isMainStory ? mainStoryContentParent : optionalContentParent;
            if (parent == null) continue;

            GameObject itemObj = Instantiate(taskItemPrefab, parent);
            TaskItemUI ui = itemObj.GetComponent<TaskItemUI>();
            if (ui != null)
            {
                // Support either Setup or Bind method depending on your TaskItemUI implementation
                ui.Setup(task);
            }
        }

        // 3. Immediately rebuild layout calculations to prevent visual jumping / squished text
        Canvas.ForceUpdateCanvases();

        if (mainStoryContentParent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(mainStoryContentParent);

        if (optionalContentParent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(optionalContentParent);

        if (mainStoryContentParent != null && mainStoryContentParent.parent != null)
        {
            RectTransform scrollContent = mainStoryContentParent.parent as RectTransform;
            if (scrollContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
            }
        }
    }
}