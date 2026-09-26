using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskPanelController : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject taskPanelRoot;

    [Header("Scroll Area")]
    [Tooltip("Drag the 'Scroll View' GameObject here")]
    [SerializeField] private ScrollRect taskScrollRect;

    [Header("Buttons")]
    [Tooltip("The Task icon button inside Page 2 of the phone")]
    [SerializeField] private Button btnOpenTaskPanel;

    [Tooltip("The 'X' or back button to close the panel")]
    [SerializeField] private Button btnCloseTaskPanel;

    [Header("List Parents")]
    [SerializeField] private RectTransform mainStoryContentParent;
    [SerializeField] private RectTransform optionalContentParent;
    [SerializeField] private GameObject taskItemPrefab;

    [Header("Font Customization")]
    [Tooltip("Drag your TMP Font Asset here to override the task text font.")]
    [SerializeField] private TMP_FontAsset customTaskFont;
    [Tooltip("Leave at 0 to keep the prefab's default font size.")]
    [SerializeField] private float customFontSize = 0f;
    [Tooltip("Check this if you also want the static headers (Objectives, Main Story, etc.) on the panel to use this font.")]
    [SerializeField] private bool applyFontToPanelHeaders = false;

    private Coroutine refreshCoroutine;

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

        if (applyFontToPanelHeaders && taskPanelRoot != null)
        {
            ApplyCustomFont(taskPanelRoot, applySize: false);
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

        // 1. Immediately disable and unparent old clones so layout calculations ignore them
        if (mainStoryContentParent != null)
        {
            for (int i = mainStoryContentParent.childCount - 1; i >= 0; i--)
            {
                Transform child = mainStoryContentParent.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        if (optionalContentParent != null)
        {
            for (int i = optionalContentParent.childCount - 1; i >= 0; i--)
            {
                Transform child = optionalContentParent.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null);
                Destroy(child.gameObject);
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
                ui.Setup(task);
            }

            // Apply custom font and size to the newly spawned task item
            ApplyCustomFont(itemObj, applySize: true);
        }

        // 3. Rebuild layout and snap scroll position to top
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
        }

        if (gameObject.activeInHierarchy)
        {
            refreshCoroutine = StartCoroutine(RebuildAndResetScroll());
        }
    }

    private void ApplyCustomFont(GameObject targetObj, bool applySize)
    {
        if (targetObj == null) return;

        TMP_Text[] tmpTexts = targetObj.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in tmpTexts)
        {
            if (customTaskFont != null)
            {
                t.font = customTaskFont;
            }
            if (applySize && customFontSize > 0f)
            {
                t.fontSize = customFontSize;
            }
        }
    }

    private IEnumerator RebuildAndResetScroll()
    {
        // Wait until Unity finishes processing the newly spawned cards
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        if (mainStoryContentParent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(mainStoryContentParent);

        if (optionalContentParent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(optionalContentParent);

        if (taskScrollRect != null && taskScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(taskScrollRect.content);

            // 1f = snap to top, preventing intermediate gaps when reopening
            taskScrollRect.verticalNormalizedPosition = 1f;
        }

        refreshCoroutine = null;
    }
}