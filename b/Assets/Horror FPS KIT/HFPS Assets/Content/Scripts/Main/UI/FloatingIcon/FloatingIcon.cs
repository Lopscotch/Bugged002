using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ThunderWire.Utility;

public class FloatingIcon : MonoBehaviour
{
    private UIFader fader = new UIFader();
    private Image icon;
    private Rect rect;

    public GameObject FollowObject;

    private float smooth;

    private bool outOfDistance = false;
    private bool isVisible = true;
    private bool fadeOut = true;

    void OnEnable()
    {
        icon = GetComponent<Image>();
        rect = new Rect(0f, 0f, Screen.width, Screen.height);
        smooth = FloatingIconManager.Instance.followSmooth;
    }

    void Update()
    {
        if (isVisible)
        {
            if (!outOfDistance)
            {
                if (rect.Contains(icon.transform.position))
                {
                    if (!fadeOut)
                    {
                        icon.enabled = true;
                    }
                    else
                    {
                        StartCoroutine(fader.StartFadeIO(icon.color.a, 2.5f, fadeOutSpeed: 4f, fadeOutAfter: UIFader.FadeOutAfter.Bool));
                        fadeOut = false;
                    }
                }
                else
                {
                    fader.fadeOut = true;
                    StartCoroutine(FadeOut());
                }
            }

            if (!fader.fadeCompleted)
            {
                Color color = icon.color;
                color.a = fader.GetFadeAlpha();
                icon.color = color;
            }
        }
        else
        {
            icon.enabled = false;
        }

        Vector3 screenPos = Tools.MainCamera().WorldToScreenPoint(FollowObject.transform.position);
        icon.transform.position = Vector3.Lerp(icon.transform.position, screenPos, Time.deltaTime * (smooth * 10));
    }

    public void SetIconVisible(bool state)
    {
        isVisible = state;
    }

    public void OutOfDIstance(bool isOut)
    {
        outOfDistance = isOut;

        if (isOut)
        {
            fader.fadeOut = true;
            StartCoroutine(FadeOut());
        }
    }

    IEnumerator FadeOut()
    {
        yield return new WaitUntil(() => fader.fadeCompleted);
        icon.enabled = false;
        fadeOut = true;
    }
}
