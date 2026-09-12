using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TaskData
{
    public string taskId;
    public string title;
    [TextArea(1, 3)]
    public string description;
    public bool isMainStory;
    public bool isCompleted;
}

[Serializable]
public class TaskDatabaseRoot
{
    public List<TaskData> tasks = new List<TaskData>();
}