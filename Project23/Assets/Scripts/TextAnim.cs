using System.Collections;
using UnityEngine;
using TMPro;

public class TextAnim : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _textMeshPro;

    public string[] stringArray;

    [SerializeField] float timeBtwnChars = 0.05f;

    [Header("Typewriter Sound")]
    [SerializeField] AudioSource typewriterAudio;
    [SerializeField] AudioClip typewriterSound;

    [Header("Narration")]
    [SerializeField] GameObject narrationPanel;

    private int i = 0;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    void Start()
    {
        
        Time.timeScale = 0f;

        EndCheck();
    }

    void EndCheck()
    {
        if (i <= stringArray.Length - 1)
        {
            _textMeshPro.text = stringArray[i];

            typingCoroutine = StartCoroutine(TextVisible());
        }
    }

    private IEnumerator TextVisible()
    {
        isTyping = true;

        _textMeshPro.ForceMeshUpdate();

        int totalVisibleCharacters = _textMeshPro.textInfo.characterCount;
        int counter = 0;

        _textMeshPro.maxVisibleCharacters = 0;

     
        if (typewriterAudio != null)
        {
            typewriterAudio.loop = true;
            typewriterAudio.Play();
        }

        while (counter <= totalVisibleCharacters)
        {
            _textMeshPro.maxVisibleCharacters = counter;

            counter++;

            yield return new WaitForSecondsRealtime(timeBtwnChars);
        }

        
        if (typewriterAudio != null)
        {
            typewriterAudio.Stop();
        }

        isTyping = false;
        typingCoroutine = null;
    }

    public void NextButton()
    {
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            _textMeshPro.maxVisibleCharacters =
                _textMeshPro.textInfo.characterCount;

            if (typewriterAudio != null)
            {
                typewriterAudio.Stop();
            }

            isTyping = false;
            typingCoroutine = null;

            return;
        }

        
        if (i == stringArray.Length - 1)
        {
            StartGame();
            return;
        }

        i++;

        EndCheck();
    }

    void StartGame()
    {
        if (typewriterAudio != null)
        {
            typewriterAudio.Stop();
        }

       
        _textMeshPro.text = "";

        
        if (narrationPanel != null)
        {
            narrationPanel.SetActive(false);
        }

        
        Time.timeScale = 1f;
    }
}