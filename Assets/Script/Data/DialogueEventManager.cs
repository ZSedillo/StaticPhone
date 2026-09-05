using System;
using System.Collections.Generic;
using UnityEngine;

public static class DialogueEventManager
{
    private static Dictionary<string, Action<string>> eventTable = new Dictionary<string, Action<string>>();

    public static void Register(string eventName, Action<string> listener)
    {
        if (!eventTable.ContainsKey(eventName))
            eventTable[eventName] = listener;
        else
            eventTable[eventName] += listener;
    }

    public static void Unregister(string eventName, Action<string> listener)
    {
        if (eventTable.ContainsKey(eventName))
            eventTable[eventName] -= listener;
    }

    public static void TriggerEvent(string eventName, string characterName)
    {
        if (string.IsNullOrEmpty(eventName)) return;

        if (eventTable.TryGetValue(eventName, out Action<string> action))
        {
            action?.Invoke(characterName);
        }
        else
        {
            Debug.Log($"[DialogueEvent] Triggered: {eventName} for {characterName}");
        }
    }
}