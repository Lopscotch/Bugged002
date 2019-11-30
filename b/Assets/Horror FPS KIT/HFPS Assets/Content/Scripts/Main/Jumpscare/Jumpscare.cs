/* Jumpscare.cs - Jumpscare Manager */

using UnityEngine;
using ThunderWire.Utility;

public class Jumpscare : MonoBehaviour {

	private JumpscareEffects effects;

	[Header("Jumpscare Setup")]
	public Animation AnimationObject;
	public AudioClip AnimationSound;
	public float SoundVolume = 0.5f;

	[Tooltip("Value sets how long will be player scared.")]
	public float ScareLevelSec = 33f;

    [SaveableField, HideInInspector]
	public bool isPlayed;

	void Start()
	{
		effects = ScriptManager.Instance.gameObject.GetComponent<JumpscareEffects> ();
	}

	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player" && !isPlayed) {
			AnimationObject.Play ();
			if(AnimationSound){Tools.PlayOneShot2D(transform.position, AnimationSound, SoundVolume);}
			effects.Scare (ScareLevelSec);
			isPlayed = true;
		}
	}
}
