using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using ThunderWire.Camera.Shaker;

public class JumpscareEffects : MonoBehaviour {

    private PostProcessVolume postProcessing;
    private ChromaticAberration chromatic;
    private Vignette vignette;

    private ItemSwitcher itemSwitcher;

    [Header("Scare Effects Settings")]
	public float scareChromaticAberration;
	public float scareVignette;

    [Header("Lerps")]
    public float scareLerp;
    public float chromaticLerp;
    public float vignetteLerp;

    [Header("Scare Shake")]
    public bool shakePreset;
    public float magnitude = 3f;
    public float roughness = 3f;
    public float startTime = 0.1f;
    public float durationTime = 3f;

    [Space(10)]
    public Vector3 PositionInfluence = new Vector3(0.15f, 0.15f, 0f);
    public Vector3 RotationInfluence = Vector3.one;

    private float LerpSpeed = 1f;
	private float ScareWaitSec;
	private float defaultVolume;

	private AudioSource PlayerBreath;
	private bool isFeelingBetter;
	private bool Effects;


	void Start () {
        if (GetComponent<ScriptManager>().ArmsCamera.GetComponent<PostProcessVolume>())
        {
            postProcessing = GetComponent<ScriptManager>().ArmsCamera.GetComponent<PostProcessVolume>();

            if (postProcessing.profile.HasSettings<ChromaticAberration>())
            {
                chromatic = postProcessing.profile.GetSetting<ChromaticAberration>();
            }
            else
            {
                Debug.LogError($"[PostProcessing] Please add Chromatic Aberration Effect to a {postProcessing.profile.name} profile in order to use Jumpscare Effects!");
            }

            if (postProcessing.profile.HasSettings<Vignette>())
            {
                vignette = postProcessing.profile.GetSetting<Vignette>();
            }
            else
            {
                Debug.LogError($"[PostProcessing] Please add Vignette Effect to a {postProcessing.profile.name} profile in order to use Jumpscare Effects!");
            }
        }
        else
        {
            Debug.LogError($"[PostProcessing] There is no PostProcessVolume script added to a {GetComponent<ScriptManager>().ArmsCamera.gameObject.name}!");
        }

        itemSwitcher = GetComponentInChildren<ItemSwitcher>();

		PlayerBreath = transform.root.gameObject.GetComponentInChildren<PlayerController>().transform.GetChild(1).transform.GetChild(1).gameObject.GetComponent<AudioSource>();
		defaultVolume = PlayerBreath.volume;
	}

	void Update()
	{
        if (isFeelingBetter) {
			if (PlayerBreath.volume > 0.01f) {
				PlayerBreath.volume = Mathf.Lerp (PlayerBreath.volume, 0f, LerpSpeed * Time.deltaTime);
			}
			if(PlayerBreath.volume <= 0.01f){
				PlayerBreath.Stop ();
				StopCoroutine (ScareBreath ());
				StopCoroutine (WaitEffects ());
				isFeelingBetter = false;
			}
		}

        if (Effects)
        {
            if (chromatic.intensity.value <= scareChromaticAberration)
            {
                chromatic.intensity.value = Mathf.Lerp(chromatic.intensity.value, scareChromaticAberration, scareLerp * Time.deltaTime);
            }
            if (vignette.intensity.value <= scareVignette)
            {
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, scareVignette, scareLerp * Time.deltaTime);
            }
        }
        else
        {
            if (chromatic.intensity.value >= 0.015f)
            {
                chromatic.intensity.value = Mathf.Lerp(chromatic.intensity.value, 0f, chromaticLerp * Time.deltaTime);
            }
            else
            {
                chromatic.intensity.Override(0);
            }
            if (vignette.intensity.value >= 0.015f)
            {
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, 0f, vignetteLerp * Time.deltaTime);
            }
            else
            {
                vignette.intensity.Override(0);
            }
        }
	}

	public void Scare(float sec)
	{
        if (shakePreset)
        {
            CameraShaker.Instance.Shake(CameraShakePresets.Scare);
        }
        else
        {
            CameraShakeInstance c = new CameraShakeInstance(magnitude, roughness, startTime, durationTime);
            c.PositionInfluence = PositionInfluence;
            c.RotationInfluence = RotationInfluence;
            CameraShaker.Instance.Shake(c);
        }

        ScareWaitSec = sec;

        if (itemSwitcher.currentItem != -1 && itemSwitcher.GetCurrentItem().GetComponent<FlashlightScript>())
        {
            itemSwitcher.GetCurrentItem().GetComponent<FlashlightScript>().Event_Scare();
        }

        Effects = true;
        StartCoroutine (ScareBreath ());
		StartCoroutine (WaitEffects ());
	}

    IEnumerator WaitEffects()
	{
		yield return new WaitForSeconds (5f);
		Effects = false;
	}

	IEnumerator ScareBreath()
	{
		PlayerBreath.volume = defaultVolume;
		PlayerBreath.Play();
		yield return new WaitForSeconds (ScareWaitSec);
		isFeelingBetter = true;
	}
}
