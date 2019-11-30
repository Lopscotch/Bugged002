using UnityEngine;
using ThunderWire.Utility;

[RequireComponent(typeof(Rigidbody), typeof(AudioSource))]
public class DynamicObjectPlank : MonoBehaviour {

    public float strenght;
    public AudioClip[] woodCrack;

    private Rigidbody plankRB;
    private GameObject player;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        plankRB = GetComponent<Rigidbody>();
        plankRB.isKinematic = true;
        plankRB.useGravity = false;
    }

    void Start()
    {
        player = HFPS_GameManager.Instance.Player;
        Physics.IgnoreCollision(GetComponent<Collider>(), player.GetComponent<Collider>());
    }

    public void UseObject()
    {
        if (!plankRB) return;

        plankRB.isKinematic = false;
        plankRB.useGravity = true;

        if (woodCrack.Length > 0)
        {
            audioSource.PlayOneShot(woodCrack[Random.Range(0, woodCrack.Length)]);
        }

        plankRB.AddForce(-Tools.MainCamera().transform.forward * strenght * 10, ForceMode.Force);
        gameObject.tag = "Untagged";
        gameObject.layer = 0;

        Destroy(this);
    }
}
