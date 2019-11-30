/*
 * NumberPadlock.cs - wirted by ThunderWire Games
 * ver. 1.0
*/

using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class NumberPadlock : MonoBehaviour, ISaveable
{
    private ExamineManager examineManager;
    public enum DigitPrefix { One, Two, Three }

    [Tooltip("Code must have maximum 3 digits! (999)")]
    public int UnlockCode = 123;

    [Space(10)]

    public float DigitRotateAngle;
    public float RotateSpeed;
    public float UnlockWaitTime;

    [Space(10)]

    public Animation m_animation;
    public string unlockAnimation;
    public AudioClip UnlockSound;

    [Space(10)]

    public UnityEvent UnlockEvent;

    private int CurrentCode;
    private int digits1_num = 0;
    private int digits2_num = 0;
    private int digits3_num = 0;

    private bool isPlayed;
    private bool isUnlocked;

    void Start()
    {
        if (UnlockCode > 999 || UnlockCode == 000)
        {
            Debug.LogError("Wrong UnlockCode format!");
            return;
        }

        examineManager = ScriptManager.Instance.GetComponent<ExamineManager>();

        foreach (var digit in GetComponentsInChildren<NumberPadlockDigits>())
        {
            digit.numberPadlock = this;
        }

        examineManager.onDropObject += delegate
        {
            if (isUnlocked)
            {
                gameObject.SetActive(false);
            }
        };
    }

    public void InteractDigit(DigitPrefix prefix)
    {
        if(prefix == DigitPrefix.One)
        {
            digits1_num += 1;

            if (digits1_num > 9)
            {
                digits1_num = 0;
            }
        }
        else if (prefix == DigitPrefix.Two)
        {
            digits2_num += 1;

            if (digits2_num > 9)
            {
                digits2_num = 0;
            }
        }
        else if (prefix == DigitPrefix.Three)
        {
            digits3_num += 1;

            if (digits3_num > 9)
            {
                digits3_num = 0;
            }
        }
    }

    void Update()
    {
        CurrentCode = int.Parse(digits1_num.ToString() + digits2_num.ToString() + digits3_num.ToString());

        if(CurrentCode == UnlockCode)
        {
            StartCoroutine(WaitUnlock());
        }
        else
        {
            StopAllCoroutines();
        }
    }

    IEnumerator WaitUnlock()
    {
        yield return new WaitForSeconds(UnlockWaitTime);

        if (m_animation && !string.IsNullOrEmpty(unlockAnimation))
        {
            if (!isPlayed)
            {
                if(UnlockSound) { AudioSource.PlayClipAtPoint(UnlockSound, transform.position, 0.75f); }
                m_animation.wrapMode = WrapMode.Once;
                m_animation.Play(unlockAnimation);
                isPlayed = true;
            }

            yield return new WaitUntil(() => !m_animation.isPlaying);
            yield return new WaitForSeconds(0.5f);
        }

        foreach (var digit in GetComponentsInChildren<NumberPadlockDigits>())
        {
            digit.isUsable = false;
        }

        UnlockEvent.Invoke();
        examineManager.CancelExamine();

        isUnlocked = true;
    }

    public Dictionary<string, object> OnSave()
    {
        return new Dictionary<string, object>
        {
            { "isUnlocked", isUnlocked }
        };
    }

    public void OnLoad(JToken token)
    {
        isUnlocked = token["isUnlocked"].ToObject<bool>();

        if (isUnlocked)
        {
            UnlockEvent.Invoke();
            gameObject.SetActive(false);
        }
    }
}
