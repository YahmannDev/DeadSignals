using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentArea;
    public GameObject textPrefab;
    public GameObject buttonPrefab;
    public ScrollRect scrollRect;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip typingSound;
    [Range(0f, 1f)] public float volumeScale = 0.5f;

    [Header("Dialogue Settings")]
    public float typingSpeed = 0.02f;
    public float buttonFadeDuration = 0.6f;
    public float delayBeforeButtons = 0.5f;
    public Color playerChoiceColor = Color.yellow;
    public DialogueNode startNode;

    private bool isTyping = false;
    private bool skipTyping = false;
    private bool canSkip = false;

    // Initializes the dialogue system and starts the first node
    void Start()
    {
        if (startNode != null) RunNode(startNode);
        else UnityEngine.Debug.LogError("DIALOUGE SYSTEM: No start node assigned in the inspector.");
    }

    // Handles input detection for skipping the typing animation
    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;
        bool interactPressed = Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;

        if (interactPressed && isTyping && canSkip)
        {
            skipTyping = true;
        }
    }

    // Clears active processes and begins execution of a specific dialogue node
    public void RunNode(DialogueNode node)
    {
        if (node == null) return;
        StopAllCoroutines();

        if (audioSource != null) audioSource.Stop();

        StartCoroutine(TypeNodeText(node));
    }

    // Manages the typewriter effect and character-by-character audio playback
    IEnumerator TypeNodeText(DialogueNode node)
    {
        isTyping = true;
        skipTyping = false;
        canSkip = false;

        GameObject newLine = Instantiate(textPrefab, contentArea);
        TextMeshProUGUI tmpText = newLine.GetComponent<TextMeshProUGUI>();

        tmpText.text = node.dialogueText;
        tmpText.maxVisibleCharacters = 0;

        yield return new WaitForEndOfFrame();
        tmpText.ForceMeshUpdate();

        int totalChars = tmpText.textInfo.characterCount;
        if (totalChars <= 0) totalChars = node.dialogueText.Length;

        canSkip = true;

        for (int i = 0; i <= totalChars; i++)
        {
            if (skipTyping)
            {
                tmpText.maxVisibleCharacters = 9999;
                if (audioSource != null) audioSource.Stop();
                break;
            }

            tmpText.maxVisibleCharacters = i;

            if (i < node.dialogueText.Length && !char.IsWhiteSpace(node.dialogueText[i]))
            {
                if (audioSource != null && typingSound != null && !audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(typingSound, volumeScale);
                }
            }

            scrollRect.verticalNormalizedPosition = 0f;
            yield return new WaitForSeconds(typingSpeed);
        }

        tmpText.maxVisibleCharacters = 9999;
        isTyping = false;

        if (audioSource != null) audioSource.Stop();

        yield return new WaitForSeconds(delayBeforeButtons);

        ShowChoices(node);
    }

    // Instantiates choice buttons and configures their visual states and listeners
    void ShowChoices(DialogueNode node)
    {
        if (node.choices == null || node.choices.Count == 0) return;

        foreach (var choice in node.choices)
        {
            GameObject btnObj = Instantiate(buttonPrefab, contentArea);

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = choice.optionText;

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectChoice(choice.optionText, choice.nextNode));

                btn.transition = Selectable.Transition.ColorTint;
                ColorBlock cb = btn.colors;
                cb.highlightedColor = playerChoiceColor;
                cb.pressedColor = playerChoiceColor * 0.8f;
                cb.selectedColor = playerChoiceColor;
                btn.colors = cb;
            }

            CanvasGroup cg = btnObj.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0;
                cg.interactable = true;
                cg.blocksRaycasts = true;
                StartCoroutine(FadeInCanvasGroup(cg));
            }
            else
            {
                btnObj.SetActive(true);
            }
        }

        Canvas.ForceUpdateCanvases();
        StartCoroutine(ScrollToBottomAfterDelay(0.05f));
    }

    // Interpolates the alpha value of a CanvasGroup for a smooth entrance effect
    IEnumerator FadeInCanvasGroup(CanvasGroup cg)
    {
        float timer = 0;
        while (timer < buttonFadeDuration)
        {
            if (cg == null) yield break;
            timer += Time.deltaTime;
            cg.alpha = timer / buttonFadeDuration;
            yield return null;
        }
        if (cg != null) cg.alpha = 1f;
    }

    // Forces the ScrollRect to the bottom after layout calculations are complete
    IEnumerator ScrollToBottomAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // Processes player selection by clearing UI buttons and advancing to the next node
    void SelectChoice(string text, DialogueNode next)
    {
        foreach (Transform child in contentArea)
        {
            if (child.GetComponent<Button>() != null) Destroy(child.gameObject);
        }

        SpawnStaticText(text, playerChoiceColor);

        if (next != null) RunNode(next);
    }

    // Instantiates a non-animated text element to display player choices in history
    void SpawnStaticText(string content, Color col)
    {
        GameObject newLine = Instantiate(textPrefab, contentArea);
        var tmp = newLine.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.color = col;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}