using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation only. Keep this component outside the panel hidden by Hide().</summary>
[DisallowMultipleComponent]
public sealed class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject screenRoot;
    [SerializeField] private Image leftPortrait;
    [SerializeField] private Image rightPortrait;
    [SerializeField] private TMP_Text speakerName;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text advanceHint;

    public bool IsConfigured => screenRoot != null &&
        transform.IsChildOf(screenRoot.transform) == false &&
        leftPortrait != null && rightPortrait != null &&
        speakerName != null && dialogueText != null;

    private void Awake() => Hide();

    public int ShowLine(DialogueLine line)
    {
        screenRoot.SetActive(true);
        SetPortrait(leftPortrait, line.leftPortrait);
        SetPortrait(rightPortrait, line.rightPortrait);
        speakerName.richText = false;
        speakerName.text = line.speakerName ?? string.Empty;
        dialogueText.richText = false;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.text = line.text ?? string.Empty;
        // Count parsed TMP characters rather than UTF-16 string units or markup.
        dialogueText.ForceMeshUpdate();
        return dialogueText.textInfo.characterCount;
    }

    public void SetVisibleCharacters(int count, bool revealing)
    {
        dialogueText.maxVisibleCharacters = Mathf.Max(0, count);
        if (advanceHint != null)
            advanceHint.text = revealing ? "ESPACIO  ·  Completar frase" : "ESPACIO  ·  Continuar";
    }

    public void Hide()
    {
        if (screenRoot != null && !transform.IsChildOf(screenRoot.transform))
            screenRoot.SetActive(false);
    }

    private static void SetPortrait(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.preserveAspect = true;
        image.enabled = sprite != null;
    }
}
