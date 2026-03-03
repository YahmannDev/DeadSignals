using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

    [Header("Audio - Effects")]
    public AudioSource sfxSource;
    public AudioClip typingSound;
    [Range(0f, 1f)] public float sfxVolume = 0.5f;

    [Header("Audio - Music")]
    public AudioSource musicSource;
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.3f;

    [Header("Dialogue Settings")]
    public float typingSpeed = 0.02f;
    public float buttonFadeDuration = 0.6f;
    public float delayBeforeButtons = 0.5f;
    public Color playerChoiceColor = Color.yellow;
    public DialogueNode startNode;

    private bool isTyping = false;
    private bool skipTyping = false;
    private bool canSkip = false;

    // Initializes audio components and starts the initial dialogue node
    void Start()
    {
        InitializeMusic();

        if (startNode != null)
        {
            RunNode(startNode);
        }
        else
        {
            UnityEngine.Debug.LogError("DialogueManager: No start node assigned.");
        }
    }

    // Configures the music source and begins playback with looping enabled
    private void InitializeMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    // Processes input to allow skipping of the typewriter effect
    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        bool interactPressed = Mouse.current.leftButton.wasPressedThisFrame ||
                               Keyboard.current.spaceKey.wasPressedThisFrame;

        if (interactPressed && isTyping && canSkip)
        {
            skipTyping = true;
        }
    }

    // Prepares the manager for a new dialogue node and stops active playback
    public void RunNode(DialogueNode node)
    {
        if (node == null) return;
        StopAllCoroutines();

        if (sfxSource != null) sfxSource.Stop();

        StartCoroutine(TypeNodeText(node));
    }

    // Handles the character-by-character display of text and associated audio cues
    IEnumerator TypeNodeText(DialogueNode node)
    {
        isTyping = true;
        skipTyping = false;
        canSkip = true;

        GameObject newLine = Instantiate(textPrefab, contentArea);
        TextMeshProUGUI tmpText = newLine.GetComponent<TextMeshProUGUI>();

        tmpText.text = node.dialogueText;
        tmpText.maxVisibleCharacters = 0;

        yield return new WaitForEndOfFrame();
        tmpText.ForceMeshUpdate();

        int totalChars = tmpText.textInfo.characterCount;

        for (int i = 0; i <= totalChars; i++)
        {
            if (skipTyping)
            {
                tmpText.maxVisibleCharacters = 9999;
                if (sfxSource != null) sfxSource.Stop(); // Added: Stop sound on skip
                break;
            }

            tmpText.maxVisibleCharacters = i;

            if (i < node.dialogueText.Length && !char.IsWhiteSpace(node.dialogueText[i]))
            {
                if (sfxSource != null && typingSound != null && !sfxSource.isPlaying)
                {
                    sfxSource.PlayOneShot(typingSound, sfxVolume);
                }
            }

            scrollRect.verticalNormalizedPosition = 0f;
            yield return new WaitForSeconds(typingSpeed);
        }

        tmpText.maxVisibleCharacters = 9999;
        if (sfxSource != null) sfxSource.Stop(); // Added: Stop sound when finished normally
        isTyping = false;

        yield return new WaitForSeconds(delayBeforeButtons);
        ShowChoices(node);
    }

    // Instantiates interaction buttons based on node choices and configures their behavior
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
                StartCoroutine(FadeInCanvasGroup(cg));
            }
        }

        Canvas.ForceUpdateCanvases();
        StartCoroutine(ScrollToBottomAfterDelay(0.05f));
    }

    // Smoothly increases the alpha of a CanvasGroup
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

    // Adjusts scroll position after a slight delay to account for UI layout updates
    IEnumerator ScrollToBottomAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // Clears existing buttons and proceeds to the next selected dialogue node
    void SelectChoice(string text, DialogueNode next)
    {
        foreach (Transform child in contentArea)
        {
            if (child.GetComponent<Button>() != null) Destroy(child.gameObject);
        }

        SpawnStaticText(text, playerChoiceColor);

        if (next != null) RunNode(next);
    }

    // Displays a static line of text in the dialogue history
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