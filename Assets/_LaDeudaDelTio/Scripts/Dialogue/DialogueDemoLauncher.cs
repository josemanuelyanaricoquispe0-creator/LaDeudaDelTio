using TMPro;
using UnityEngine;

/// <summary>Isolated demonstration only; deliberately contains no exploration controls.</summary>
[DisallowMultipleComponent]
public sealed class DialogueDemoLauncher : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueData demoDialogue;
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {
        if (dialogueManager == null)
        {
            Debug.LogError("Dialogue demo: assign the DialogueManager.", this);
            return;
        }

        dialogueManager.DialogueEnded += OnDialogueEnded;
        if (!dialogueManager.TryStartDialogue(demoDialogue) && statusText != null)
            statusText.text = "No se pudo iniciar la demo. Revisa las referencias y la Console.";
    }

    private void OnDialogueEnded()
    {
        if (statusText != null)
            statusText.text = "Conversación finalizada\n\nLa interfaz se cerró correctamente.\nDetén Play y vuelve a iniciarlo para repetir la prueba.\n\nDEMO PROVISIONAL · No pertenece al guion oficial.";
    }

    private void OnDestroy()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded -= OnDialogueEnded;
    }
}
