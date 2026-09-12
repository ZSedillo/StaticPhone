using UnityEngine;

public class TaskTriggerHook : MonoBehaviour
{
    [SerializeField] private string taskIdToComplete;
    [SerializeField] private bool triggerOnEnable = true;

    private void OnEnable()
    {
        if (triggerOnEnable && !string.IsNullOrEmpty(taskIdToComplete))
        {
            TriggerTask();
        }
    }

    public void TriggerTask()
    {
        TaskManager.Instance?.CompleteTask(taskIdToComplete);
    }
}