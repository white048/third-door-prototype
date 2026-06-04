using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text lineText;
    [SerializeField] private Text resultText;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private Text[] optionLabels;
    [SerializeField] private Text statusText;
    [SerializeField] private Text endingText;
    [SerializeField] private PlayerController player;
    [SerializeField] private Image speakerColorBar;
    [SerializeField] private Image speakerUnderline;
    [SerializeField] private Image panelOutline;

    private Coroutine closeRoutine;
    private Coroutine endingRoutine;
    private DialogueActor currentActor;
    private bool dialogueOpen;

    private static readonly Color RedDoorColor = new Color(1f, 0.08f, 0.10f);
    private static readonly Color BlueDoorColor = new Color(0.06f, 0.36f, 1f);
    private static readonly Color ThirdDoorColor = new Color(0.96f, 0.96f, 0.96f);

    public bool IsDialogueOpen => dialogueOpen;

    public void Configure(
        CanvasGroup group,
        Text speakerLabel,
        Text lineLabel,
        Text resultLabel,
        Button[] buttons,
        Text[] labels,
        Text statusLabel,
        Text endingLabel,
        PlayerController playerController,
        Image colorBar = null,
        Image underline = null,
        Image outline = null)
    {
        dialogueGroup = group;
        speakerText = speakerLabel;
        lineText = lineLabel;
        resultText = resultLabel;
        optionButtons = buttons;
        optionLabels = labels;
        statusText = statusLabel;
        endingText = endingLabel;
        player = playerController;
        speakerColorBar = colorBar;
        speakerUnderline = underline;
        panelOutline = outline;
        HideDialogueImmediate();
        ShowEndingText(string.Empty);
    }

    private void ApplySpeakerColor(Color color)
    {
        if (speakerColorBar != null) speakerColorBar.color = color;
        if (speakerUnderline != null) speakerUnderline.color = color;
        if (panelOutline != null)
        {
            Color o = color;
            o.a = 0.6f;
            panelOutline.color = o;
        }
    }

    private Color ColorForActor(DialogueActor actor)
    {
        if (actor == DialogueActor.RedDoor) return RedDoorColor;
        if (actor == DialogueActor.BlueDoor) return BlueDoorColor;
        return ThirdDoorColor;
    }

    private Color ColorForSpeakerName(string name)
    {
        if (name == "红门") return RedDoorColor;
        if (name == "蓝门") return BlueDoorColor;
        return ThirdDoorColor;
    }

    private void Awake()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
        }

        HideDialogueImmediate();
        ShowEndingText(string.Empty);
    }

    public void ShowDialogue(DialogueActor actor)
    {
        currentActor = actor;
        dialogueOpen = true;
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }

        if (player != null)
        {
            player.SetControlsEnabled(false);
        }

        dialogueGroup.alpha = 1f;
        dialogueGroup.interactable = true;
        dialogueGroup.blocksRaycasts = true;
        if (resultText != null)
        {
            resultText.text = string.Empty;
        }

        ApplySpeakerColor(ColorForActor(actor));

        if (actor == DialogueActor.RedDoor)
        {
            SetText(speakerText, "红门");
            SetText(lineText, "告诉他，这样下去不行。");
            SetOptions(
                "我会把话带给他。",
                "你究竟想让他听见什么？",
                "你自己跟他说。");
        }
        else if (actor == DialogueActor.BlueDoor)
        {
            SetText(speakerText, "蓝门");
            SetText(lineText, "让她别再伸手过来。");
            SetOptions(
                "我会替你挡住她。",
                "你是想她离开，还是想要空间？",
                "你自己跟她说。");
        }
        else
        {
            SetText(speakerText, "第三扇门");
            SetText(lineText, "他们都在等你。");
            SetOptions(
                "我会继续替她和他传话。",
                "让他们直接说。我退出。",
                null);
        }
    }

    public void ShowOneShotLine(string speaker, string line, float closeDelay = 1.8f)
    {
        dialogueOpen = false;
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }

        if (player != null)
        {
            player.SetControlsEnabled(false);
        }

        dialogueGroup.alpha = 1f;
        dialogueGroup.interactable = false;
        dialogueGroup.blocksRaycasts = true;
        ApplySpeakerColor(ColorForSpeakerName(speaker));
        SetText(speakerText, speaker);
        SetText(lineText, line);
        SetText(resultText, string.Empty);

        foreach (Button button in optionButtons)
        {
            button.gameObject.SetActive(false);
        }

        closeRoutine = StartCoroutine(CloseAfterDelay(closeDelay));
    }

    private void Update()
    {
        if (!dialogueOpen || dialogueGroup == null || !dialogueGroup.interactable)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            Choose(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            Choose(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            Choose(2);
        }
    }

    public void ShowEndingText(string text)
    {
        if (endingText == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            endingText.text = string.Empty;
            endingText.gameObject.SetActive(false);
            return;
        }

        if (endingRoutine != null)
        {
            StopCoroutine(endingRoutine);
        }

        endingRoutine = StartCoroutine(EndingFadeIn(text));
    }

    private IEnumerator EndingFadeIn(string text)
    {
        // Hold while the doors finish their open/close animation and the dialogue panel closes.
        yield return new WaitForSeconds(2.0f);

        // Player gets control back so they can wander the open doorway / dimmed room while the words appear.
        if (player != null)
        {
            player.SetControlsEnabled(true);
        }

        endingText.text = text;
        endingText.gameObject.SetActive(true);
        Color baseColor = endingText.color;
        baseColor.a = 0f;
        endingText.color = baseColor;

        const float fadeDuration = 2.2f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            Color c = endingText.color;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            endingText.color = c;
            yield return null;
        }

        // After the line lands, fade in the restart hint as a subtle second beat.
        yield return new WaitForSeconds(1.4f);
        endingText.text = text + "\n\n按 R 重新开始";
    }

    public void SetStatusText(string text)
    {
        SetText(statusText, text);
        if (statusText != null)
        {
            statusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
        }
    }

    private void SetOptions(string first, string second, string third)
    {
        string[] labels = { first, second, third };
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool enabled = !string.IsNullOrEmpty(labels[i]);
            optionButtons[i].gameObject.SetActive(enabled);
            optionButtons[i].onClick.RemoveAllListeners();

            int optionIndex = i;
            optionButtons[i].onClick.AddListener(() => Choose(optionIndex));
            SetText(optionLabels[i], enabled ? "▸  " + (i + 1) + "    " + labels[i] : string.Empty);
        }
    }

    private void Choose(int optionIndex)
    {
        if (!dialogueOpen || optionButtons == null || optionIndex < 0 || optionIndex >= optionButtons.Length || !optionButtons[optionIndex].gameObject.activeSelf)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play(GameSound.ChoiceClick, 0.7f);
        }

        dialogueOpen = false;
        string result;
        GameStateManager state = GameStateManager.Instance;
        if (state == null)
        {
            SetText(resultText, "场景还没有准备好。请退出 Play Mode 后重新进入。");
            closeRoutine = StartCoroutine(CloseAfterDelay(1.8f));
            return;
        }

        if (currentActor == DialogueActor.RedDoor)
        {
            result = state.ApplyRedDoorChoice(optionIndex);
        }
        else if (currentActor == DialogueActor.BlueDoor)
        {
            result = state.ApplyBlueDoorChoice(optionIndex);
        }
        else
        {
            result = state.ApplyFinalChoice(optionIndex);
        }

        foreach (Button button in optionButtons)
        {
            button.gameObject.SetActive(false);
        }

        SetText(resultText, result);
        closeRoutine = StartCoroutine(CloseAfterDelay(1.8f));
    }

    private IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        HideDialogueImmediate();

        if (player != null && !GameStateManager.Instance.gameEnded)
        {
            player.SetControlsEnabled(true);
        }
    }

    private void HideDialogueImmediate()
    {
        if (dialogueGroup == null)
        {
            return;
        }

        dialogueGroup.alpha = 0f;
        dialogueGroup.interactable = false;
        dialogueGroup.blocksRaycasts = false;
        dialogueOpen = false;
    }

    private static void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
