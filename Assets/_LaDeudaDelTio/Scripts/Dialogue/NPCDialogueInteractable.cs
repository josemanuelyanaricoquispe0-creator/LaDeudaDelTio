using System.Collections.Generic;
using UnityEngine;

/// <summary>World-space proximity, independent of tags, colliders and NPC sprite scale.</summary>
[DisallowMultipleComponent]
public sealed class NPCDialogueInteractable : MonoBehaviour
{
    [SerializeField] private DialogueData dialogue;
    [SerializeField, Min(0.1f)] private float interactionRadius = 1.3f;
    [SerializeField] private Transform interactionOrigin;
    [SerializeField] private Vector2 interactionOffset;

    private static readonly List<NPCDialogueInteractable> activeNPCs =
        new List<NPCDialogueInteractable>();
    private SpriteRenderer spriteRenderer;

    public static IReadOnlyCollection<NPCDialogueInteractable> ActiveNPCs => activeNPCs;
    public DialogueData Dialogue => dialogue;
    public float InteractionRadius => Mathf.Max(0.1f, interactionRadius);
    public bool IsAvailable => isActiveAndEnabled && dialogue != null;
    public Vector2 InteractionPosition
    {
        get
        {
            if (interactionOrigin != null)
                return (Vector2)interactionOrigin.position + interactionOffset;
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            Vector2 position = spriteRenderer != null ? spriteRenderer.bounds.center : transform.position;
            return position + interactionOffset;
        }
    }

    public bool IsInRange(Vector2 playerPosition) => IsAvailable &&
        (InteractionPosition - playerPosition).sqrMagnitude <= InteractionRadius * InteractionRadius;

    private void OnEnable()
    {
        if (!activeNPCs.Contains(this))
            activeNPCs.Add(this);
    }
    private void OnDisable() => activeNPCs.Remove(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => activeNPCs.Clear();

    // Also supports Enter Play Mode with domain and/or scene reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RestoreRegistry()
    {
        foreach (var npc in FindObjectsByType<NPCDialogueInteractable>())
            if (npc.isActiveAndEnabled && !activeNPCs.Contains(npc))
                activeNPCs.Add(npc);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.95f, 0.75f, 0.35f, 0.8f);
        Gizmos.DrawWireSphere(InteractionPosition, InteractionRadius);
    }
}
