using UnityEngine;
using UnityEngine.Events;

public class InteractEvents : MonoBehaviour {

    public enum Type { Interact, Animation, Event }
    public enum Repeat { Once, MoreTimes, OnOff }

    public Type InteractType = Type.Interact;
    public Repeat RepeatMode = Repeat.MoreTimes;

    [Space(7)]
    public GameObject InteractObject;
    public string InteractMessage = "UseObject";

    [Header("Animation")]
    public string AnimationName;
    public float AnimationSpeed = 1.0f;

    [Header("Event")]
    public UnityEvent InteractEvent;
    public UnityEvent InteractBackEvent;

    [Space(7)]
    public AudioClip InteractSound;

    private bool isInteracted;

    private void Start()
    {
        if (!InteractObject)
        {
            InteractObject = gameObject;
        }
    }

    public void Interact()
    {
        UseObject();
    }

    public void UseObject()
    {
        if(InteractType == Type.Interact)
        {
            InteractObject.SendMessage(InteractMessage, SendMessageOptions.DontRequireReceiver);
        }
        else if(InteractType == Type.Animation)
        {
            if(RepeatMode == Repeat.Once)
            {
                if (!isInteracted)
                {
                    InteractObject.GetComponent<Animation>()[AnimationName].speed = AnimationSpeed;
                    InteractObject.GetComponent<Animation>().Play(AnimationName);
                    if (InteractSound) { AudioSource.PlayClipAtPoint(InteractSound, transform.position, 0.75f); }
                    isInteracted = true;
                }
            }
            else
            {
                if (!InteractObject.GetComponent<Animation>().isPlaying)
                {
                    InteractObject.GetComponent<Animation>()[AnimationName].speed = AnimationSpeed;
                    InteractObject.GetComponent<Animation>().Play(AnimationName);
                    if (InteractSound) { AudioSource.PlayClipAtPoint(InteractSound, transform.position, 0.75f); }
                }
            }
        }
        else if (InteractType == Type.Event)
        {
            if (RepeatMode == Repeat.Once)
            {
                if (!isInteracted)
                {
                    InteractEvent.Invoke();
                    isInteracted = true;
                }
            }
            else if (RepeatMode == Repeat.MoreTimes)
            {
                InteractEvent.Invoke();
            }
            else if (RepeatMode == Repeat.OnOff)
            {
                if (!isInteracted)
                {
                    InteractEvent.Invoke();
                    isInteracted = true;
                }
                else
                {
                    InteractBackEvent.Invoke();
                    isInteracted = false;
                }
            }
            if (InteractSound) { AudioSource.PlayClipAtPoint(InteractSound, transform.position, 0.75f); }
        }
    }
}
