using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomCallScheduler : MonoBehaviour
{
    public static RandomCallScheduler Instance { get; private set; }

    [Header("Interval Range (Seconds)")]
    [SerializeField] private float minInterval = 60f;
    [SerializeField] private float maxInterval = 180f;

    [Header("Sprite Pool for Overlay Avatar")]
    [SerializeField] private List<Sprite> characterAvatars = new List<Sprite>();

    private Coroutine schedulerCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartCallLoop();
    }

    public void StartCallLoop()
    {
        if (schedulerCoroutine != null) StopCoroutine(schedulerCoroutine);
        schedulerCoroutine = StartCoroutine(RandomCallLoopRoutine());
    }

    public void StopCallLoop()
    {
        if (schedulerCoroutine != null) StopCoroutine(schedulerCoroutine);
    }

    private IEnumerator RandomCallLoopRoutine()
    {
        while (true)
        {
            float waitSeconds = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitSeconds);

            // Fetch any contact unlocked in OnlyYaps
            List<SavedContactData> eligibleCallers = new List<SavedContactData>();
            foreach (var contact in ChatSaveSystem.DB.savedContacts)
            {
                if (!contact.contactName.StartsWith("OY_") && contact.isUnlockedInOnlyYaps && contact.canVoiceCall)
                {
                    eligibleCallers.Add(contact);
                }
            }

            if (eligibleCallers.Count > 0)
            {
                SavedContactData randomContact = eligibleCallers[Random.Range(0, eligibleCallers.Count)];
                Sprite avatar = (randomContact.avatarIndex >= 0 && randomContact.avatarIndex < characterAvatars.Count)
                    ? characterAvatars[randomContact.avatarIndex]
                    : null;

                if (VoiceCallOverlayController.Instance != null)
                {
                    VoiceCallOverlayController.Instance.TriggerIncomingCall(randomContact.contactName, avatar);
                }
            }
        }
    }
}