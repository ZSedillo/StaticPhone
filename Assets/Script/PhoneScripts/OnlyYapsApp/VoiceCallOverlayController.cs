using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoiceCallOverlayController : MonoBehaviour
{
    public static VoiceCallOverlayController Instance { get; private set; }

    [Header("Main Overlay Panel")]
    [SerializeField] private GameObject callOverlayRoot;

    [Header("Caller Info")]
    [SerializeField] private Image callerAvatarImage;
    [SerializeField] private TextMeshProUGUI txtCallerName;
    [SerializeField] private TextMeshProUGUI txtCallStatus;

    [Header("Incoming Call Controls")]
    [SerializeField] private GameObject incomingControlsGroup;
    [SerializeField] private Button btnAccept;
    [SerializeField] private Button btnReject;

    [Header("Active / Outgoing Call Controls")]
    [SerializeField] private GameObject activeControlsGroup;
    [SerializeField] private Button btnEndCall;

    private Coroutine callRoutine;
    private string activeCaller = "";
    private string activeStartNodeId = "";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        if (btnAccept != null) btnAccept.onClick.AddListener(AcceptCall);
        if (btnReject != null) btnReject.onClick.AddListener(EndOrRejectCall);
        if (btnEndCall != null) btnEndCall.onClick.AddListener(EndOrRejectCall);
    }

    public void StartOutgoingCall(string contactName, Sprite avatar, string startNodeId = "")
    {
        activeCaller = contactName;
        activeStartNodeId = startNodeId;

        gameObject.SetActive(true);
        if (callOverlayRoot != null) callOverlayRoot.SetActive(true);
        transform.SetAsLastSibling();

        SetupCallVisuals(contactName, avatar);

        if (incomingControlsGroup != null) incomingControlsGroup.SetActive(false);
        if (activeControlsGroup != null) activeControlsGroup.SetActive(true);
        if (txtCallStatus != null) txtCallStatus.text = "Calling...";

        if (callRoutine != null) StopCoroutine(callRoutine);
        callRoutine = StartCoroutine(OutgoingCallRoutine());
    }

    public void TriggerIncomingCall(string contactName, Sprite avatar, string startNodeId = "")
    {
        activeCaller = contactName;
        activeStartNodeId = startNodeId;

        gameObject.SetActive(true);
        if (callOverlayRoot != null) callOverlayRoot.SetActive(true);
        transform.SetAsLastSibling();

        SetupCallVisuals(contactName, avatar);

        if (incomingControlsGroup != null) incomingControlsGroup.SetActive(true);
        if (activeControlsGroup != null) activeControlsGroup.SetActive(false);
        if (txtCallStatus != null) txtCallStatus.text = "Incoming Call...";

        if (callRoutine != null) StopCoroutine(callRoutine);
        callRoutine = StartCoroutine(IncomingCallTimeoutRoutine());
    }

    private void SetupCallVisuals(string name, Sprite avatar)
    {
        if (txtCallerName != null)
        {
            txtCallerName.text = name;
            txtCallerName.color = Color.white;
        }

        if (txtCallStatus != null)
        {
            txtCallStatus.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        }

        if (callerAvatarImage != null)
        {
            if (avatar != null)
            {
                callerAvatarImage.sprite = avatar;
                callerAvatarImage.color = Color.white;
            }
            else
            {
                callerAvatarImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }
        }
    }

    private IEnumerator OutgoingCallRoutine()
    {
        yield return new WaitForSeconds(2.5f);

        if (incomingControlsGroup != null) incomingControlsGroup.SetActive(false);
        if (activeControlsGroup != null) activeControlsGroup.SetActive(true);

        if (VoiceCallDialogueUI.Instance != null)
        {
            Sprite av = callerAvatarImage != null ? callerAvatarImage.sprite : null;
            VoiceCallDialogueUI.Instance.StartCallDialogue(activeCaller, av, activeStartNodeId);
        }

        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime;
            int minutes = Mathf.FloorToInt(timer / 60f);
            int seconds = Mathf.FloorToInt(timer % 60f);
            if (txtCallStatus != null) txtCallStatus.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            yield return null;
        }
    }

    private IEnumerator IncomingCallTimeoutRoutine()
    {
        yield return new WaitForSeconds(15f);

        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.CompleteTaskByEvent("LOST_FOR_WORDS");
        }

        EndOrRejectCall();
    }

    public void AcceptCall()
    {
        if (callRoutine != null) StopCoroutine(callRoutine);

        if (incomingControlsGroup != null) incomingControlsGroup.SetActive(false);
        if (activeControlsGroup != null) activeControlsGroup.SetActive(true);

        callRoutine = StartCoroutine(ActiveCallTimerRoutine());

        if (VoiceCallDialogueUI.Instance != null)
        {
            Sprite av = callerAvatarImage != null ? callerAvatarImage.sprite : null;
            VoiceCallDialogueUI.Instance.StartCallDialogue(activeCaller, av, activeStartNodeId);
        }
    }

    private IEnumerator ActiveCallTimerRoutine()
    {
        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime;
            int minutes = Mathf.FloorToInt(timer / 60f);
            int seconds = Mathf.FloorToInt(timer % 60f);
            if (txtCallStatus != null) txtCallStatus.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            yield return null;
        }
    }

    public void EndOrRejectCall()
    {
        if (callRoutine != null)
        {
            StopCoroutine(callRoutine);
            callRoutine = null;
        }

        if (VoiceCallDialogueUI.Instance != null)
        {
            VoiceCallDialogueUI.Instance.gameObject.SetActive(false);
        }

        if (!string.IsNullOrEmpty(activeCaller))
        {
            DialogueEventManager.TriggerEvent("CALL_COMPLETED", activeCaller);
            DialogueEventManager.TriggerEvent("RESET_CALL_DIALOGUE", activeCaller);
        }

        if (callOverlayRoot != null) callOverlayRoot.SetActive(false);
        gameObject.SetActive(false);
    }
}