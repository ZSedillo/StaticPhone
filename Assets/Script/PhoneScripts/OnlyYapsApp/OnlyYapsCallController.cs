using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OnlyYapsCallController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject callOverlay;
    [SerializeField] private TextMeshProUGUI txtCallerName;
    [SerializeField] private TextMeshProUGUI txtCallStatus;
    [SerializeField] private Button btnVoiceCall;
    [SerializeField] private Button btnEndCall;

    [Header("Call Audio Settings")]
    [SerializeField] private AudioSource ringAudioSource;
    [SerializeField] private AudioSource voiceAudioSource;

    private Coroutine callTimerCoroutine;
    private bool isInCall = false;

    private void Awake()
    {
        if (btnVoiceCall != null) btnVoiceCall.onClick.AddListener(StartCall);
        if (btnEndCall != null) btnEndCall.onClick.AddListener(EndCall);
    }

    public void StartCall()
    {
        callOverlay.SetActive(true);
        isInCall = true;
        txtCallStatus.text = "Calling...";
        
        if (callTimerCoroutine != null) StopCoroutine(callTimerCoroutine);
        callTimerCoroutine = StartCoroutine(CallLifecycleRoutine());
    }

    private IEnumerator CallLifecycleRoutine()
    {
        // 1. Ringing sound simulation
        if (ringAudioSource != null) ringAudioSource.Play();
        yield return new WaitForSeconds(3.5f);
        if (ringAudioSource != null) ringAudioSource.Stop();

        // 2. Connected state (Voice audio starts here)
        txtCallStatus.text = "Connected";
        if (voiceAudioSource != null) voiceAudioSource.Play();

        float seconds = 0f;
        while (isInCall)
        {
            seconds += Time.deltaTime;
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            txtCallStatus.text = $"{mins:00}:{secs:00}";
            yield return null;
        }
    }

    public void EndCall()
    {
        isInCall = false;
        if (callTimerCoroutine != null) StopCoroutine(callTimerCoroutine);
        if (ringAudioSource != null) ringAudioSource.Stop();
        if (voiceAudioSource != null) voiceAudioSource.Stop();
        callOverlay.SetActive(false);
    }
}