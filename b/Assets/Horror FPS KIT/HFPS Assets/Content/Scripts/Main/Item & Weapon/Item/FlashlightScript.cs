/*
 * FlashlightScript.cs - written by ThunderWire Studio
 * Script for Flashlight Controls
 * version 1.3
*/

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class FlashlightScript : MonoBehaviour, ISwitcher, ISaveableArmsItem {

    private ScriptManager scriptManager;
    private InputController inputManager;
    private ItemSwitcher switcher;
    private HFPS_GameManager gameManager;
    private Inventory inventory;
    private Animation AnimationComp;

    [Header("Setup")]
    public int FlashlightInventoryID;
    public Light LightObject;
    public AudioSource audioSource;

    [Header("Flashlight Settings")]
    public AudioClip ClickSound;
    public float batteryLifeInSec = 300f;
    public float batteryPercentage = 100;
    public float canReloadPercent;

    [Header("Animation")]
    public GameObject FlashlightGO;
    public string DrawAnim;
    [Range(0, 5)] public float DrawSpeed = 1f;
    public string HideAnim;
    [Range(0, 5)] public float HideSpeed = 1f;
    public string ReloadAnim;
    [Range(0, 5)] public float ReloadSpeed = 1f;
    public string IdleAnim;

    [Header("Extra Animations")]
    public string ScareAnim;
    [Range(0, 5)] public float ScareAnimSpeed = 1f;
    public string NoPowerAnim;
    [Range(0, 5)] public float NoPowerAnimSpeed = 1f;

    private KeyCode UseItemKey;

    [HideInInspector]
    public bool CanReload;

    private bool isOn;
    private bool isSelected;
    private bool isReloading;
    private bool isPressed;
    private bool isBlocked;
    private bool noPower;
    private bool scare;

    private float defaultLightPercentagle;
    private int switcherID;

    void Awake()
    {
        AnimationComp = FlashlightGO.GetComponent<Animation>();
        scriptManager = transform.root.GetComponentInChildren<ScriptManager>();
        switcher = transform.root.GetComponentInChildren<ItemSwitcher>();

        FlashlightGO.SetActive(false);
    }

    void Start()
    {    
        inputManager = scriptManager.GetScript<InputController>();
        gameManager = scriptManager.GetScript<HFPS_GameManager>();
        inventory = scriptManager.GetScript<Inventory>();
        switcherID = switcher.GetIDByObject(gameObject);

        AnimationComp[DrawAnim].speed = DrawSpeed;
        AnimationComp[HideAnim].speed = HideSpeed;
        AnimationComp[ReloadAnim].speed = ReloadSpeed;

        AnimationComp[ScareAnim].speed = ScareAnimSpeed;
        AnimationComp[NoPowerAnim].speed = NoPowerAnimSpeed;

        defaultLightPercentagle = batteryPercentage;
    }

    public void Reload()
    {
        if (FlashlightGO.activeSelf)
        {
            if (batteryPercentage < canReloadPercent)
            {
                StartCoroutine(ReloadCorountine());
                isReloading = true;
            }
        }
    }

    IEnumerator ReloadCorountine()
    {
        AnimationComp.Play(ReloadAnim);

        isOn = false;
        if (ClickSound)
        {
            audioSource.clip = ClickSound;
            audioSource.Play();
        }

        yield return new WaitUntil(() => !AnimationComp.isPlaying);

        batteryPercentage = defaultLightPercentagle;
        noPower = false;
        isReloading = false;
    }

    public void Select()
    {
        gameManager.ShowLightPercentagle(batteryPercentage);
        FlashlightGO.SetActive(true);
        AnimationComp.Play(DrawAnim);
        isSelected = true;
    }

    public void Deselect()
    {
        if (FlashlightGO.activeSelf && !isReloading)
        {
            gameManager.ShowLightPercentagle(batteryPercentage, false);
            StartCoroutine(DeselectCorountine());
        }
    }

    public void Disable()
    {
        isOn = false;
        isSelected = false;
        gameManager.ShowLightPercentagle(batteryPercentage, false);
        FlashlightGO.SetActive(false);   
    }

    IEnumerator DeselectCorountine()
    {
        AnimationComp.Play(HideAnim);

        isOn = false;
        if (ClickSound)
        {
            audioSource.clip = ClickSound;
            audioSource.Play();
        }

        yield return new WaitUntil(() => !AnimationComp.isPlaying);

        FlashlightGO.SetActive(false);
        isSelected = false;
    }

    public void Event_FlashlightOn()
    {
        isOn = !isOn;
        if (ClickSound)
        {
            audioSource.clip = ClickSound;
            audioSource.Play();
        }
    }

    public void Event_Scare()
    {
        AnimationComp.Play(ScareAnim);
        scare = true;
    }

    public void EnableItem()
    {
        FlashlightGO.SetActive(true);
        AnimationComp.Play(IdleAnim);
        isSelected = true;
        isOn = true;
    }

    public void OnItemBlock(bool blocked)
    {
        isBlocked = blocked;
    }

    void Update()
    {
        if (inputManager.HasInputs())
        {
            UseItemKey = inputManager.GetInput("Flashlight");
        }

        CanReload = batteryPercentage < canReloadPercent;

        if (inventory.CheckItemInventory(FlashlightInventoryID) && !isReloading && switcher.currentLightObject == switcherID && !isBlocked)
        {
            if (Input.GetKeyDown(UseItemKey) && !AnimationComp.isPlaying && !isPressed)
            {
                if (!isSelected && switcher.currentItem != switcherID)
                {
                    switcher.SelectItem(switcherID);
                }
                else
                {
                    Deselect();
                }

                isPressed = true;
            }
            else if (isPressed)
            {
                isPressed = false;
            }
        }

        if (scare && !AnimationComp.isPlaying)
        {
            scare = false;
        }

        if (isSelected && !noPower && !scare)
        {
            gameManager.UpdateLightPercent(batteryPercentage);
        }

        if (isOn)
        {
            LightObject.enabled = true;
            batteryPercentage -= Time.deltaTime * (100 / batteryLifeInSec);
        }
        else
        {
            LightObject.enabled = false;
        }

        batteryPercentage = Mathf.Clamp(batteryPercentage, 0, 100);

        if (batteryPercentage > 95.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 1, Time.deltaTime);
        }
        else if (batteryPercentage <= 95.0f && batteryPercentage > 90.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.95f, Time.deltaTime);
        }
        else if (batteryPercentage <= 90.0f && batteryPercentage > 85.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.9f, Time.deltaTime);
        }
        else if (batteryPercentage <= 85.0f && batteryPercentage > 80.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.85f, Time.deltaTime);
        }
        else if (batteryPercentage <= 80.0f && batteryPercentage > 75.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.8f, Time.deltaTime);
        }
        else if (batteryPercentage <= 75.0f && batteryPercentage > 70.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.75f, Time.deltaTime);
        }
        else if (batteryPercentage <= 70.0f && batteryPercentage > 65.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.7f, Time.deltaTime);
        }
        else if (batteryPercentage <= 65.0f && batteryPercentage > 60.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.65f, Time.deltaTime);
        }
        else if (batteryPercentage <= 60.0f && batteryPercentage > 55.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.6f, Time.deltaTime);
        }
        else if (batteryPercentage <= 55.0f && batteryPercentage > 50.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.55f, Time.deltaTime);
        }
        else if (batteryPercentage <= 50.0f && batteryPercentage > 45.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.5f, Time.deltaTime);
        }
        else if (batteryPercentage <= 45.0f && batteryPercentage > 40.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.45f, Time.deltaTime);
        }
        else if (batteryPercentage <= 40.0f && batteryPercentage > 35.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.4f, Time.deltaTime);
        }
        else if (batteryPercentage <= 35.0f && batteryPercentage > 30.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.35f, Time.deltaTime);
        }
        else if (batteryPercentage <= 30.0f && batteryPercentage > 25.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.3f, Time.deltaTime);
        }
        else if (batteryPercentage <= 25.0f && batteryPercentage > 20.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.25f, Time.deltaTime);
        }
        else if (batteryPercentage <= 20.0f && batteryPercentage > 15.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.2f, Time.deltaTime);
        }
        else if (batteryPercentage <= 15.0f && batteryPercentage > 10.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.15f, Time.deltaTime);
        }
        else if (batteryPercentage <= 10.0f && batteryPercentage > 5.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.1f, Time.deltaTime);
        }
        else if (batteryPercentage <= 5.0f && batteryPercentage > 1.0f)
        {
            LightObject.intensity = Mathf.Lerp(LightObject.intensity, 0.05f, Time.deltaTime);
        }
        else if (batteryPercentage <= 1.0f)
        {
            if (!AnimationComp.isPlaying && !noPower)
            {
                LightObject.intensity = 0f;
                AnimationComp.Play(NoPowerAnim);
                noPower = true;
            }
        }
    }

    public Dictionary<string, object> OnSave()
    {
        return new Dictionary<string, object>
        {
            {"batteryPercentage", batteryPercentage}
        };
    }

    public void OnLoad(Newtonsoft.Json.Linq.JToken token)
    {
        batteryPercentage = (float)token["batteryPercentage"];
    }
}