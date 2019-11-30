/*
 * MeleeController.cs - written by ThunderWire Studio
 * version 1.0
*/

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeController : MonoBehaviour, ISwitcher, ISwitcherWallHit
{
    [System.Serializable]
    public class Hitmark
    {
        public string Tag;
        public GameObject HitMark;
        public AudioClip HitSound;
        [Range(0,1)] public float HitVolume = 1f;
    }

    private ScriptManager scriptManager;
    private InputController inputControl;
    private PlayerController playerControl;
    private Inventory Inventory;
    private Animation anim;
    private Camera cam;

    public List<Hitmark> hitmarks = new List<Hitmark>();

    [Header("Main")]
    public LayerMask HitLayer;
    public float HitDistance;
    public float HitForce;
    public float HitWaitDelay;
    public Vector2Int AttackDamage;

    [Header("Inventory")]
    public int ItemInventoryID;

    [Header("Kickback")]
    public Vector3 SwayKickback;
    public float SwaySpeed = 0.1f;

    [Header("Animation")]
    public GameObject MeleeGO;
    public string DrawAnim;
    [Range(0, 5)] public float DrawSpeed = 1f;
    public string HideAnim;
    [Range(0, 5)] public float HideSpeed = 1f;
    public string AttackAnim;
    [Range(0, 5)] public float AttackSpeed = 1f;

    [Header("Sounds")]
    public AudioSource audioSource;

    [Space(5)]
    public AudioClip DrawSound;
    [Range(0, 1)] public float DrawVolume = 1f;

    [Space(5)]
    public AudioClip HideSound;
    [Range(0, 1)] public float HideVolume = 1f;

    [Space(5)]
    public AudioClip SwaySound;
    [Range(0, 1)] public float SwayVolume = 1f;

    private KeyCode AttackKey;

    private bool isSelected;
    private bool isBlocked;
    public bool wallHit = false;

    void Awake()
    {
        anim = MeleeGO.GetComponent<Animation>();
        scriptManager = transform.root.GetComponentInChildren<ScriptManager>();
        playerControl = transform.root.GetComponent<PlayerController>(); 
    }

    void Start()
    {
        inputControl = InputController.Instance;
        Inventory = Inventory.Instance;
        cam = ScriptManager.Instance.MainCamera;

        anim[DrawAnim].speed = DrawSpeed;
        anim[HideAnim].speed = HideSpeed;
        anim[AttackAnim].speed = AttackSpeed;
    }

    public void Select()
    {
        if (anim.isPlaying) return;

        MeleeGO.SetActive(true);
        anim.Play(DrawAnim);
        PlaySound(DrawSound, DrawVolume);

        StartCoroutine(SelectCoroutine());
    }

    IEnumerator SelectCoroutine()
    {
        yield return new WaitUntil(() => !anim.isPlaying);
        isSelected = true;
    }

    public void Deselect()
    {
        if (anim.isPlaying) return;

        anim.Play(HideAnim);
        PlaySound(HideSound, HideVolume);
        StartCoroutine(DeselectCoroutine());
    }

    IEnumerator DeselectCoroutine()
    {
        yield return new WaitUntil(() => !anim.isPlaying);
        isSelected = false;
        MeleeGO.SetActive(false);
    }

    public void Disable()
    {
        isSelected = false;
        MeleeGO.SetActive(false);
    }

    public void EnableItem()
    {
        MeleeGO.SetActive(true);
        isSelected = true;
    }

    public void OnWallHit(bool Hit)
    {
        wallHit = Hit;
    }

    public void OnItemBlock(bool blocked)
    {
        isBlocked = blocked;
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if (clip)
        {
            audioSource.volume = volume;
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    void Update()
    {
        if (inputControl.HasInputs())
        {
            AttackKey = inputControl.GetInput("Fire");
        }

        if (Inventory.CheckItemInventory(ItemInventoryID) && scriptManager.ScriptGlobalState && isSelected && !wallHit && !isBlocked)
        {
            if (Input.GetKeyDown(AttackKey) && !anim.isPlaying)
            {
                PlaySound(SwaySound, SwayVolume);
                anim.Play(AttackAnim);
                StartCoroutine(SwayMelee(SwayKickback, SwaySpeed));

                Ray playerAim = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

                if (Physics.Raycast(playerAim, out RaycastHit hit, HitDistance, HitLayer))
                {
                    StartCoroutine(Hit(hit, playerAim.direction));
                }
            }
        }
    }

    IEnumerator Hit(RaycastHit hit, Vector3 dir)
    {
        yield return new WaitForSeconds(HitWaitDelay);

        hit.collider.SendMessageUpwards("ApplyDamage", Random.Range(AttackDamage.x, AttackDamage.y), SendMessageOptions.DontRequireReceiver);

        if (hit.rigidbody)
        {
            hit.rigidbody.AddForceAtPosition(dir * (HitForce * 10), hit.point);
        }

        if(hitmarks.Any(mark => mark.Tag == hit.collider.tag))
        {
            SpawnHitmark(hitmarks.SingleOrDefault(mark => mark.Tag == hit.collider.tag), hit);
        }
        else
        {
            if (hitmarks.Count > 0)
            {
                SpawnHitmark(hitmarks[0], hit);
            }
        }
    }

    void SpawnHitmark(Hitmark hitmark, RaycastHit hit)
    {
        bool canSpawn = true;

        if (hit.collider.GetComponent<InteractiveItem>() && hit.collider.GetComponent<InteractiveItem>().examineType != InteractiveItem.ExamineType.None)
        {
            canSpawn = false;
        }

        if (canSpawn)
        {
            GameObject mark = Instantiate(hitmark.HitMark, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
            mark.transform.SetParent(hit.collider.transform);

            Vector3 relative = mark.transform.InverseTransformPoint(cam.transform.position);
            int angle = Mathf.RoundToInt(Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg);

            mark.transform.RotateAround(hit.point, hit.normal, angle);

            if (hitmark.HitSound && mark.GetComponent<AudioSource>())
            {
                AudioSource markAS = mark.GetComponent<AudioSource>();
                markAS.clip = hitmark.HitSound;
                markAS.volume = hitmark.HitVolume;
                markAS.Play();
            }
        }
        else
        {
            AudioSource.PlayClipAtPoint(hitmark.HitSound, hit.transform.position, hitmark.HitVolume);
        }
    }

    IEnumerator SwayMelee(Vector3 pos, float time)
    {
        Quaternion s = playerControl.fallEffect.localRotation;
        Quaternion sw = playerControl.fallEffectWeap.localRotation;
        Quaternion e = playerControl.fallEffect.localRotation * Quaternion.Euler(pos);
        float r = 1.0f / time;
        float t = 0.0f;
        while (t < 1.0f)
        {
            t += Time.deltaTime * r;
            playerControl.fallEffect.localRotation = Quaternion.Slerp(s, e, t);
            playerControl.fallEffectWeap.localRotation = Quaternion.Slerp(sw, e, t);
            yield return null;
        }
    }
}
