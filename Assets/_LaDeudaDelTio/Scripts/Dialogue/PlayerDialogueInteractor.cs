using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Selects one nearby NPC and bridges dialogue events to the player's control lock.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerControlLock))]
[RequireComponent(typeof(SpriteCookPlayerMovement))]
[DefaultExecutionOrder(-100)]
public sealed class PlayerDialogueInteractor : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text interactionPrompt;

    private PlayerControlLock controlLock;
    private SpriteCookPlayerMovement movement;
    private Collider2D playerCollider;
    private SpriteCookAnimationController animations;

    public NPCDialogueInteractable CurrentCandidate { get; private set; }
    public DialogueManager Manager => dialogueManager;
    public Vector2 InteractionPosition => playerCollider != null && playerCollider.enabled
        ? (Vector2)playerCollider.bounds.center : (Vector2)transform.position;

    private void Awake()
    {
        controlLock = GetComponent<PlayerControlLock>();
        movement = GetComponent<SpriteCookPlayerMovement>();
        playerCollider = GetComponent<Collider2D>();
        animations = GetComponent<SpriteCookAnimationController>();
    }

    private void OnEnable()
    {
        SetPrompt(false);
        if (dialogueManager == null)
            return;
        dialogueManager.DialogueStarted += OnDialogueStarted;
        dialogueManager.DialogueEnded += OnDialogueEnded;
        if (dialogueManager.IsOpen)
            OnDialogueStarted();
    }

    private bool CanInteract => dialogueManager != null && dialogueManager.isActiveAndEnabled &&
        !dialogueManager.IsOpen && controlLock != null && controlLock.isActiveAndEnabled &&
        !controlLock.BlocksInput && movement != null && movement.isActiveAndEnabled;

    private void Update()
    {
        CurrentCandidate = CanInteract ? FindNearestNPC() : null;
        SetPrompt(CurrentCandidate != null);
        if (CurrentCandidate != null && EPressed())
            TryInteract();
    }

    public bool TryInteract()
    {
        if (!isActiveAndEnabled || !CanInteract)
            return false;
        // Revalidate proximity so a cached, disabled NPC cannot start a conversation.
        CurrentCandidate = FindNearestNPC();
        return CurrentCandidate != null && dialogueManager.TryStartDialogue(CurrentCandidate.Dialogue);
    }

    private NPCDialogueInteractable FindNearestNPC()
    {
        NPCDialogueInteractable nearest = null;
        float nearestDistance = float.PositiveInfinity;
        Vector2 position = InteractionPosition;
        foreach (var npc in NPCDialogueInteractable.ActiveNPCs)
        {
            if (npc == null || !npc.IsInRange(position))
                continue;
            float distance = (npc.InteractionPosition - position).sqrMagnitude;
            // Equal distances retain registration order; no deprecated instance IDs needed.
            if (distance < nearestDistance)
            {
                nearest = npc;
                nearestDistance = distance;
            }
        }
        return nearest;
    }

    private void OnDialogueStarted()
    {
        if (controlLock != null)
            controlLock.Acquire(this);
        if (animations != null)
            animations.SetIdle();
        CurrentCandidate = null;
        SetPrompt(false);
    }

    private void OnDialogueEnded()
    {
        if (controlLock != null)
            controlLock.Release(this);
        CurrentCandidate = null;
        SetPrompt(false);
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
        {
            // Do not leave an open conversation behind after removing its player bridge.
            if (dialogueManager.IsOpen)
                dialogueManager.CloseDialogue();
            dialogueManager.DialogueStarted -= OnDialogueStarted;
            dialogueManager.DialogueEnded -= OnDialogueEnded;
        }
        if (controlLock != null)
            controlLock.Release(this);
        CurrentCandidate = null;
        SetPrompt(false);
    }

    private void SetPrompt(bool visible)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.text = "[E] HABLAR";
            interactionPrompt.enabled = visible;
        }
        if (promptRoot != null)
            promptRoot.SetActive(visible);
    }

    private static bool EPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            return Keyboard.current.eKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }
}
