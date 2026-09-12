using System;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    public event Action OnTasksUpdated;

    [Header("JSON Configuration")]
    [SerializeField] private string jsonResourcePath = "Tasks/TaskObjectives";

    [HideInInspector]
    public List<TaskData> allTasks = new List<TaskData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadTasksFromJson();
        LoadTaskProgress();
    }

    private void LoadTasksFromJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonResourcePath);
        if (jsonFile != null)
        {
            TaskDatabaseRoot root = JsonUtility.FromJson<TaskDatabaseRoot>(jsonFile.text);
            if (root != null && root.tasks != null)
            {
                allTasks = root.tasks;
                Debug.Log($"<color=#00FFB2>[TaskManager] Successfully loaded {allTasks.Count} tasks from JSON.</color>");
            }
        }
        else
        {
            Debug.LogError($"[TaskManager] Could not find Resources/{jsonResourcePath}.json! Check your folder structure.");
        }
    }

    public void CompleteTask(string taskId)
    {
        TaskData task = allTasks.Find(t => t.taskId.Equals(taskId, StringComparison.OrdinalIgnoreCase));
        if (task != null && !task.isCompleted)
        {
            task.isCompleted = true;
            SaveTaskProgress();
            OnTasksUpdated?.Invoke();
            Debug.Log($"<color=#38E54D>[TaskManager] Objective Checked: {task.title} ({task.taskId})</color>");
        }
    }

    public bool IsTaskDone(string taskId)
    {
        TaskData task = allTasks.Find(t => t.taskId.Equals(taskId, StringComparison.OrdinalIgnoreCase));
        return task != null && task.isCompleted;
    }

    private void SaveTaskProgress()
    {
        List<string> completedIds = new List<string>();
        foreach (var t in allTasks)
        {
            if (t.isCompleted) completedIds.Add(t.taskId);
        }
        PlayerPrefs.SetString("STATIC_COMPLETED_TASKS", string.Join(";", completedIds));
        PlayerPrefs.Save();
    }

    private void LoadTaskProgress()
    {
        if (!PlayerPrefs.HasKey("STATIC_COMPLETED_TASKS")) return;

        string data = PlayerPrefs.GetString("STATIC_COMPLETED_TASKS", "");
        HashSet<string> completedSet = new HashSet<string>(data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

        foreach (var t in allTasks)
        {
            if (completedSet.Contains(t.taskId))
            {
                t.isCompleted = true;
            }
        }
    }

    public void ResetAllTasks()
    {
        PlayerPrefs.DeleteKey("STATIC_COMPLETED_TASKS");
        foreach (var t in allTasks) t.isCompleted = false;
        OnTasksUpdated?.Invoke();
    }
}