using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Runs a conversation without taking ownership of player controls or timeScale.</summary>
[DisallowMultipleComponent]
public sealed class DialogueManager : MonoBehaviour
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField, Min(1f)] private float charactersPerSecond = 35f;
    [SerializeField] private bool readSpaceInput = true;

    private DialogueData currentDialogue;
    private int characterCount;
    private float revealProgress;
    private int lineStartedFrame;

    public bool IsOpen { get; private set; }
    public bool IsRevealing => IsOpen && revealProgress < characterCount;
    public int CurrentLineIndex { get; private set; } = -1;

    // Phase 2 can subscribe to these events to acquire/release a player control lock.
    public event Action DialogueStarted;
    public event Action DialogueEnded;
    public event Action<int, DialogueLine> LineChanged;

    public bool TryStartDialogue(DialogueData dialogue)
    {
        if (IsOpen || !isActiveAndEnabled)
            return false;

        if (dialogueUI == null || !dialogueUI.IsConfigured ||
            dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
        {
            Debug.LogWarning("DialogueManager: missing UI references or empty conversation.", this);
            return false;
        }

        foreach (DialogueLine line in dialogue.lines)
        {
            if (line == null)
            {
                Debug.LogWarning("DialogueManager: the conversation contains a null line.", dialogue);
                return false;
            }
        }

        currentDialogue = dialogue;
        IsOpen = true;
        CurrentLineIndex = 0;
        PresentCurrentLine();
        DialogueStarted?.Invoke();
        return true;
    }

    /// <summary>Completes the current text, advances a line, or closes the conversation.</summary>
    public void Advance()
    {
        if (!IsOpen)
            return;

        if (IsRevealing)
        {
            revealProgress = characterCount;
            dialogueUI.SetVisibleCharacters(characterCount, false);
            return;
        }

        if (CurrentLineIndex + 1 >= currentDialogue.lines.Length)
        {
            CloseDialogue();
            return;
        }

        CurrentLineIndex++;
        PresentCurrentLine();
    }

    public void CloseDialogue()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        CurrentLineIndex = -1;
        currentDialogue = null;
        characterCount = 0;
        revealProgress = 0f;
        if (dialogueUI != null)
            dialogueUI.Hide();
        DialogueEnded?.Invoke();
    }

    private void PresentCurrentLine()
    {
        revealProgress = 0f;
        lineStartedFrame = Time.frameCount;
        DialogueLine line = currentDialogue.lines[CurrentLineIndex];
        characterCount = dialogueUI.ShowLine(line);
        dialogueUI.SetVisibleCharacters(0, characterCount > 0);
        LineChanged?.Invoke(CurrentLineIndex, line);
    }

    private void Update()
    {
        if (!IsOpen || Time.frameCount == lineStartedFrame)
            return;

        // Read first: a press while writing always completes this line, even near its end.
        if (readSpaceInput && SpacePressed())
        {
            Advance();
            return;
        }

        if (IsRevealing)
        {
            revealProgress = Mathf.Min(characterCount,
                revealProgress + Mathf.Max(1f, charactersPerSecond) * Time.unscaledDeltaTime);
            dialogueUI.SetVisibleCharacters(Mathf.FloorToInt(revealProgress), IsRevealing);
        }
    }

    private void OnDisable() => CloseDialogue();

    private static bool SpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            return Keyboard.current.spaceKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }
}
