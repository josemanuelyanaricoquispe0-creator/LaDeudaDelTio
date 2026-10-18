
using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string speakerName;

    [TextArea(3, 5)]
    public string text;

    public Sprite leftPortrait;
    public Sprite rightPortrait;
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "La Deuda del Tio/Dialogue")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines;
}
