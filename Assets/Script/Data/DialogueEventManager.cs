using System;
using System.Collections.Generic;
using UnityEngine;

public static class DialogueEventManager
{
    // Table for events taking 0 arguments (e.g. START_CALL_RINGTONE)
    private static Dictionary<string, Action> parameterlessTable = new Dictionary<string, Action>();

    // Table for events taking 1 string (e.g. CALL_COMPLETED with characterName)
    private static Dictionary<string, Action<string>> singleParamTable = new Dictionary<string, Action<string>>();

    // Table for events taking 2 strings (e.g. TRIGGER_CALL with characterName and startNodeId)
    private static Dictionary<string, Action<string, string>> dualParamTable = new Dictionary<string, Action<string, string>>();

    // =========================================================
    // 0-PARAMETER OVERLOADS
    // =========================================================
    public static void Register(string eventName, Action listener)
    {
        if (string.IsNullOrEmpty(eventName) || listener == null) return;

        if (!parameterlessTable.ContainsKey(eventName))
            parameterlessTable[eventName] = listener;
        else
            parameterlessTable[eventName] += listener;
    }

    public static void Unregister(string eventName, Action listener)
    {
        if (string.IsNullOrEmpty(eventName) || listener == null) return;

        if (parameterlessTable.ContainsKey(eventName))
        {
            parameterlessTable[eventName] -= listener;
            if (parameterlessTable[eventName] == null)
                parameterlessTable.Remove(eventName);
        }
    }

    public static void TriggerEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName)) return;

        if (parameterlessTable.TryGetValue(eventName, out Action action))
        {
            action?.Invoke();
        }
        else
        {
            Debug.Log($"[DialogueEvent] Triggered (No Params): {eventName}");
        }
    }

    // =========================================================
    // 1-PARAMETER OVERLOADS (Backwards-Compatible with your code)
    // =========================================================
    public static void Register(string eventName, Action<string> listener)
    {
        if (string.IsNullOrEmpty(eventName) || listener == null) return;

        if (!singleParamTable.ContainsKey(eventName))
            singleParamTable[eventName] = listener;
        else
            singleParamTable[eventName] += listener;
    }

    public static void Unregister(string eventName, Action<string> listener)
    {
        if (string.IsNullOrEmpty(eventName) || listener == null) return;

        if (singleParamTable.ContainsKey(eventName))
        {
            singleParamTable[eventName] -= listener;
            if (singleParamTable[eventName] == null)
                singleParamTable.Remove(eventName);
        }
    }

    public static void TriggerEvent(string eventName, string characterName)
    {
        if (string.IsNullOrEmpty(eventName)) return;

        if (singleParamTable.TryGetValue(eventName, out Action<string> action))
        {
            action?.Invoke(characterName);
        }
        else
        {
            // Also check if someone registered a parameterless callback for this event name
            if (parameterlessTable.TryGetValue(eventName, out Action noParamAction))
            {
                noParamAction?.Invoke();
            }
            else
            {
                Debug.Log($"[DialogueEvent] Triggered: {eventName} for {characterName}");
            }
        }
    }

    // =========================================================
    // 2-PARAMETER OVERLOADS (e.g., characterName + specific call node ID)
    // =========================================================
    public static void Register(string eventName, Action<string, string> listener)
    {
        if (string.IsNullOrEmpty(eventName) || listener == null) return;

        if (!dualParamTable.ContainsKey(eventName))
            dualParamTable[eventName] = listener;
        else
            dualParamTable[eventName] += listener;
    }

    public static void Unregister(string eventName, Action<string, string> listener)
    {
        if (string.IsNullOrEmpty(eventName) || listener == null) return;

        if (dualParamTable.ContainsKey(eventName))
        {
            dualParamTable[eventName] -= listener;
            if (dualParamTable[eventName] == null)
                dualParamTable.Remove(eventName);
        }
    }

    public static void TriggerEvent(string eventName, string param1, string param2)
    {
        if (string.IsNullOrEmpty(eventName)) return;

        if (dualParamTable.TryGetValue(eventName, out Action<string, string> action))
        {
            action?.Invoke(param1, param2);
        }
        else
        {
            Debug.Log($"[DialogueEvent] Triggered (Dual): {eventName} ({param1}, {param2})");
        }
    }

    // =========================================================
    // CLEANUP (Prevents dead listeners on scene reloads)
    // =========================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ClearAll()
    {
        parameterlessTable.Clear();
        singleParamTable.Clear();
        dualParamTable.Clear();
    }
}