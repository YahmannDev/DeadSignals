using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NeuerDialog", menuName = "DeadSignals/DialogNode")]
public class DialogueNode : ScriptableObject
{
    [TextArea(3, 10)]
    public string dialogueText; // Was wird gesagt?

    public List<Choice> choices; // Welche Optionen gibt es?

    [System.Serializable]
    public struct Choice
    {
        public string optionText;      // Text auf dem Button
        public DialogueNode nextNode; // Wohin führt diese Wahl?
    }
}