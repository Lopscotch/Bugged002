using System.Linq;
using UnityEngine;
using ThunderWire.Utility;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class TriggerHint : MonoBehaviour, ISaveable {

    public string Hint;
    public float TimeShow;
    public float ShowAfter = 0f;

    [Header("Extra")]
    public AudioClip HintSound;

    private float timer;
    private bool timedShow;
    private bool isShown;

    private HFPS_GameManager gameManager;
    private InputController inputController;
    private AudioSource soundEffects;

    void Start()
    {
        gameManager = HFPS_GameManager.Instance;
        inputController = InputController.Instance;
        soundEffects = GetComponent<AudioSource>() ? GetComponent<AudioSource>() : null;

        if (HintSound && !soundEffects)
        {
            Debug.LogError("[TriggerHint] HintSound require an a AudioSource Component!");
        }

        if (soundEffects)
        {
            soundEffects.spatialBlend = 0f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isShown && gameManager && inputController)
        {
            char[] hintChars = Hint.ToCharArray();

            if (hintChars.Contains('{') && hintChars.Contains('}'))
            {
                string key = inputController.GetInput(Hint.GetBetween('{', '}')).ToString();
                Hint = Hint.ReplacePart('{', '}', key);
            }

            if(ShowAfter > 0)
            {
                timedShow = true;
            }
            else
            {
                if (!string.IsNullOrEmpty(Hint)){ gameManager.ShowHint(Hint, TimeShow); }
                if (HintSound && soundEffects) {
                    soundEffects.clip = HintSound;
                    soundEffects.Play();
                }
                isShown = true;
            }
        }
    }

    void Update()
    {
        if (timedShow && !isShown)
        {
            timer += Time.fixedDeltaTime;

            if(timer >= ShowAfter)
            {
                if (!string.IsNullOrEmpty(Hint)) { gameManager.ShowHint(Hint, TimeShow); }
                if (HintSound && soundEffects)
                {
                    soundEffects.clip = HintSound;
                    soundEffects.Play();
                }
                isShown = true;
            }
        }
    }

    public Dictionary<string, object> OnSave()
    {
        return new Dictionary<string, object>()
        {
            { "enabled", enabled },
            { "isShown", isShown }
        };
    }

    public void OnLoad(JToken token)
    {
        enabled = token["enabled"].ToObject<bool>();
        isShown = token["isShown"].ToObject<bool>();
    }
}
