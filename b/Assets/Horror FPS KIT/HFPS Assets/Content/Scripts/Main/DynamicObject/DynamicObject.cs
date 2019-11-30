using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using ThunderWire.Helpers;

public enum Type_Dynamic
{
    Door,
    Drawer,
    Lever,
    Valve,
    MovableInteract
}

public enum Type_Use
{
    Normal,
    Locked,
    Jammed,
}

public enum Type_Interact
{
    Mouse,
    Animation
}

public enum Type_Key
{
    Script,
    Inventory
}

public class DynamicObject : MonoBehaviour, ISaveable {

    private RandomHelper rand = new RandomHelper();
    private Inventory inventory;
    private HingeJoint Joint;

    #region Enums
    public Type_Dynamic dynamicType = Type_Dynamic.Door;
    public Type_Use useType = Type_Use.Normal;
    public Type_Interact interactType = Type_Interact.Animation;
    public Type_Key keyType = Type_Key.Script;
    #endregion

    #region GenericSettings
    public Animation m_Animation;
    public List<Collider> IgnoreColliders = new List<Collider>();
    public UnityEvent InteractEvent;
    public UnityEvent DisabledEvent;
    public string customText;
    public int keyID;
    public string useAnim;
    public string backUseAnim;
    #endregion

    #region DoorSettings
    public bool useJammedPlanks;
    public List<Rigidbody> Planks = new List<Rigidbody>();

    public string knobAnim;

    public bool useSR;
    public Vector3 startingRotation;

    [HideInInspector] public bool isOpened;
    [HideInInspector] public float toPlayerAngle;

    private float minCloseAngle;
    private float maxCloseAngle;
    private float defaultAngle;
    private bool load;
    #endregion

    #region DrawerSettings
    [Tooltip("If true default move vector will be X, if false default vector is Z")]
    public bool moveWithX;
    public Vector2 minMaxMove;
    public bool reverseMove;
    public float InteractPos;
    #endregion

    #region LeverSettings
    public float stopAngle;
    public bool lockOnUp;

    [HideInInspector] public bool isUp;
    #endregion

    #region ValveSettings
    public AudioClip[] valveTurnSounds;
    public float valveSoundAfter;
    public float valveTurnSpeed;
    public float valveTurnTime;

    private bool turnSound;
    private bool valveInvoked;
    #endregion

    #region Sounds
    [Range(0, 1)]
    public float m_Volume = 1;
    public AudioClip Open;
    public AudioClip Close;
    public AudioClip LockedTry;
    public AudioClip UnlockSound;
    public AudioClip LeverUpSound;
    #endregion

    /* Debug */
    public bool DebugAngle;

    [HideInInspector] public float rotateValue;
    [HideInInspector] public bool isHolding;
    [HideInInspector] public bool Hold;
    [HideInInspector] public bool hasKey;
    [HideInInspector] public bool isLocked;
    [HideInInspector] public bool isInvoked;
    [HideInInspector] public float Angle;

    private bool invokeUp;
    private bool isPlayed;
    private bool onceUnlock;
    private bool loadSound;

    private Transform collisionObject;
    private Transform oldCollisionObjectParent;

    public void ParseUseType(int value)
    {
        useType = (Type_Use)value;
    }

    void Awake()
    {
        if(dynamicType == Type_Dynamic.Door)
        {
            if(interactType == Type_Interact.Mouse)
            {
                if (GetComponent<HingeJoint>())
                {
                    Joint = GetComponent<HingeJoint>();
                }
                else
                {                   
                    Debug.LogError(transform.parent.gameObject.name + " requires Hinge Joint Component!");
                    return;
                }
            }

            if (GetLockStatus())
            {
                if (interactType == Type_Interact.Mouse)
                {
                    GetComponent<Rigidbody>().freezeRotation = true;
                    Joint.useLimits = false;
                }
                isLocked = true;
            }
            else if(useType == Type_Use.Normal)
            {
                if (interactType == Type_Interact.Mouse)
                {
                    GetComponent<Rigidbody>().freezeRotation = false;
                    Joint.useLimits = true;
                }
                isLocked = false;
            }
        }
        else if(dynamicType == Type_Dynamic.Drawer)
        {
            IgnoreColliders.Add(PlayerController.Instance.gameObject.GetComponent<Collider>());
            isLocked = useType != Type_Use.Normal;
        }
        else if(dynamicType == Type_Dynamic.Lever)
        {
            isLocked = false;
        }
        else if(dynamicType == Type_Dynamic.MovableInteract)
        {
            useType = Type_Use.Normal;
            interactType = Type_Interact.Mouse;
            isLocked = false;
        }

        if (IgnoreColliders.Count > 0)
        {
            for (int i = 0; i < IgnoreColliders.Count; i++)
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), IgnoreColliders[i]);
            }
        }

        defaultAngle = transform.eulerAngles.y;
        minCloseAngle = defaultAngle - 10f;
        maxCloseAngle = defaultAngle + 10f;

        inventory = Inventory.Instance;
    }

    void Start()
    {
        Invoke("LateStart", 0.1f);
    }

    void LateStart()
    {
        if (useSR) { isOpened = true; transform.localEulerAngles = startingRotation; }
        load = false;
    }

    void Update()
    {
        if (CanLockType())
        {
            if (!isLocked)
            {
                useType = Type_Use.Normal;
                if (interactType == Type_Interact.Mouse && Joint)
                {
                    Joint.useLimits = true;
                    GetComponent<Rigidbody>().freezeRotation = false;
                }
            }
        }

        if (dynamicType == Type_Dynamic.Door)
        {
            Angle = transform.eulerAngles.y;

            if (GetLockStatus())
            {
                if (useJammedPlanks)
                {
                    CheckJammed();

                    if (Planks.Count < 1)
                    {
                        isLocked = false;
                    }
                }
            }

            if (interactType == Type_Interact.Mouse)
            {
                Joint.useMotor = isHolding;

                if (DebugAngle)
                {
                    if (Joint.limits.min < -1)
                    {
                        Debug.Log("Angle: " + Angle + " , Door Close: " + (defaultAngle - 0.5f));
                    }
                    else
                    {
                        Debug.Log("Angle: " + Angle + " , Door Close: " + (defaultAngle + 0.5));
                    }
                }

                if (Angle > 1f)
                {
                    if (Joint.limits.min < -1)
                    {
                        if (!load)
                        {
                            if (Angle <= (defaultAngle - 2f) && !isOpened)
                            {
                                if (Open)
                                {
                                    AudioSource.PlayClipAtPoint(Open, transform.position);
                                }
                                isOpened = true;
                            }

                            if (Angle > minCloseAngle && Angle < maxCloseAngle && Angle >= (defaultAngle - 0.5f) && isOpened)
                            {
                                if (Close)
                                {
                                    AudioSource.PlayClipAtPoint(Close, transform.position);
                                }
                                isOpened = false;
                            }
                        }
                    }
                    else
                    {
                        if (Angle >= (defaultAngle + 0.5f) && !isOpened)
                        {
                            if (Open)
                            {
                                AudioSource.PlayClipAtPoint(Open, transform.position);
                            }
                            isOpened = true;
                        }

                        if (Angle <= (defaultAngle + 0.2f) && isOpened)
                        {
                            if (Close)
                            {
                                AudioSource.PlayClipAtPoint(Close, transform.position);
                            }
                            isOpened = false;
                        }
                    }
                }
            }
        }
        else if (dynamicType == Type_Dynamic.Lever)
        {
            if (reverseMove)
            {
                Angle = transform.localEulerAngles.x;
            }
            else
            {
                Angle = transform.localEulerAngles.y;
            }

            float minAngle = Angle - 2f;
            float maxAngle = Angle + 2f;

            if (interactType == Type_Interact.Mouse)
            {
                if (DebugAngle)
                {
                    Debug.Log("Angle: " + Mathf.Round(Angle) + " Min: " + minAngle + " Max: " + maxAngle);
                }

                if (lockOnUp)
                {
                    if (Hold)
                    {
                        GetComponent<Rigidbody>().isKinematic = true;
                        GetComponent<Rigidbody>().useGravity = false;
                    }
                    else
                    {
                        GetComponent<Rigidbody>().isKinematic = false;
                        GetComponent<Rigidbody>().useGravity = true;
                    }
                }
                else
                {
                    if (isHolding)
                    {
                        GetComponent<Rigidbody>().isKinematic = false;
                        GetComponent<Rigidbody>().useGravity = false;
                    }

                    if (!isHolding && Hold)
                    {
                        GetComponent<Rigidbody>().isKinematic = true;
                        GetComponent<Rigidbody>().useGravity = false;
                    }
                    else if (!Hold)
                    {
                        GetComponent<Rigidbody>().isKinematic = false;
                        GetComponent<Rigidbody>().useGravity = true;
                    }
                }

                if (!DebugAngle)
                {
                    if (Angle > minAngle && Angle < maxAngle && Angle >= stopAngle)
                    {
                        InteractEvent?.Invoke();
                        if (!isPlayed && !loadSound && LeverUpSound)
                        {
                            Debug.Log("Lever Up: " + transform.parent.parent.gameObject.name);
                            AudioSource.PlayClipAtPoint(LeverUpSound, transform.position, 1f);
                            isPlayed = true;
                        }
                        invokeUp = true;
                        isInvoked = true;
                        isUp = true;
                        Hold = true;
                    }
                    else
                    {
                        DisabledEvent?.Invoke();
                        invokeUp = false;
                        isInvoked = true;
                        isUp = false;
                        isPlayed = false;
                        Hold = false;
                    }
                }
            }
            else
            {
                if (isUp && !isInvoked && !isOpened)
                {
                    StartCoroutine(WaitEventInvoke());
                    isOpened = true;
                }
            }
        }
        else if (dynamicType == Type_Dynamic.Valve)
        {
            if (rotateValue >= 1f && !valveInvoked)
            {
                InteractEvent?.Invoke();
                valveInvoked = true;
            }
            else if (turnSound)
            {
                StartCoroutine(ValveSounds());
                turnSound = false;
            }
        }
        else if (dynamicType == Type_Dynamic.MovableInteract)
        {
            if (!isInvoked)
            {
                if (moveWithX)
                {
                    if (minMaxMove.x < minMaxMove.y)
                    {
                        if (transform.localPosition.x <= InteractPos)
                        {
                            InteractEvent?.Invoke();
                            isInvoked = true;
                        }
                    }
                    else if(minMaxMove.y > minMaxMove.x)
                    {
                        if (transform.localPosition.x >= InteractPos)
                        {
                            InteractEvent?.Invoke();
                            isInvoked = true;
                        }
                    }
                }
                else
                {
                    if (minMaxMove.x < minMaxMove.y)
                    {
                        if (transform.localPosition.z >= InteractPos)
                        {
                            InteractEvent?.Invoke();
                            isInvoked = true;
                        }
                    }
                    else if (minMaxMove.y > minMaxMove.x)
                    {
                        if (transform.localPosition.z <= InteractPos)
                        {
                            InteractEvent?.Invoke();
                            isInvoked = true;
                        }
                    }
                }
            }
        }

        if (!isHolding)
        {
            turnSound = true;
        }
    }

    public void UseObject()
    {
        if (CanLockType())
        {
            if (!onceUnlock && isLocked)
            {
                if (LockedTry && !CheckHasKey())
                {
                    AudioSource.PlayClipAtPoint(LockedTry, transform.position, m_Volume);
                }

                TryUnlock();
            }
        }

        if(dynamicType == Type_Dynamic.Door && !string.IsNullOrEmpty(knobAnim))
        {
            if (!isOpened && !m_Animation.isPlaying && !hasKey && isLocked)
            {
                m_Animation.Play(knobAnim);
            }
        }

        if (interactType == Type_Interact.Animation && !isLocked)
        {
            if (dynamicType == Type_Dynamic.Door || dynamicType == Type_Dynamic.Drawer || dynamicType == Type_Dynamic.Lever)
            {
                if (!m_Animation.isPlaying && !Hold)
                {
                    if (!isOpened)
                    {
                        m_Animation.Play(useAnim);
                        if (Open) { AudioSource.PlayClipAtPoint(Open, transform.position, m_Volume); }
                        if (dynamicType == Type_Dynamic.Lever) { StartCoroutine(LeverSound()); }
                        isOpened = true;
                    }
                    else
                    {
                        m_Animation.Play(backUseAnim);
                        if (Close && dynamicType == Type_Dynamic.Drawer) { AudioSource.PlayClipAtPoint(Close, transform.position, m_Volume); }
                        if (dynamicType == Type_Dynamic.Lever) { StartCoroutine(LeverSound()); }
                        isOpened = false;
                    }
                }

                if (Hold) return;

                if (dynamicType == Type_Dynamic.Lever)
                {
                    StartCoroutine(WaitEventInvoke());
                    Hold = lockOnUp;
                }
            }
        }
    }

    private void TryUnlock()
    {
        if (keyType == Type_Key.Inventory)
        {
            if (inventory && keyID != -1)
            {
                if (inventory.CheckItemInventory(keyID))
                {
                    hasKey = true;
                    if (UnlockSound) { AudioSource.PlayClipAtPoint(UnlockSound, transform.position, m_Volume); }
                    StartCoroutine(WaitUnlock());
                    inventory.RemoveItem(keyID);
                    onceUnlock = true;
                }
            }
            else if (!inventory)
            {
                Debug.LogError("Inventory script is not set!");
            }
        }
        else
        {
            if (hasKey)
            {
                if (UnlockSound) { AudioSource.PlayClipAtPoint(UnlockSound, transform.position, m_Volume); }
                StartCoroutine(WaitUnlock());
                onceUnlock = true;
            }
        }
    }

    IEnumerator WaitEventInvoke()
    {
        yield return new WaitUntil(() => !m_Animation.isPlaying);

        if (!invokeUp)
        {
            InteractEvent?.Invoke();
            isInvoked = true;
            isUp = true;
            invokeUp = true;
            yield return null;
        }
        else
        {
            DisabledEvent?.Invoke();
            isInvoked = true;
            isUp = false;
            invokeUp = false;
            yield return null;
        }
    }

    IEnumerator ValveSounds()
    {
        while (isHolding && !valveInvoked)
        {
            int soundID = rand.Range(0, valveTurnSounds.Length);
            AudioSource.PlayClipAtPoint(valveTurnSounds[soundID], transform.position, m_Volume);
            yield return new WaitForSeconds(valveSoundAfter);
        }

        yield return null;
    }

    IEnumerator LeverSound()
    {
        yield return new WaitUntil(() => !m_Animation.isPlaying);
        if (LeverUpSound) { AudioSource.PlayClipAtPoint(LeverUpSound, transform.position, m_Volume); }
    }

    IEnumerator WaitUnlock()
    {
        if (UnlockSound)
        {
            yield return new WaitForSeconds(UnlockSound.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        isLocked = false;
    }

    public bool GetLockStatus()
    {
        return useType == Type_Use.Jammed || useType == Type_Use.Locked;
    }

    public bool CheckHasKey()
    {
        if (keyType == Type_Key.Inventory)
        {
            if (inventory)
            {
                return inventory.CheckItemInventory(keyID);
            }
        }
        else if (hasKey)
        {
            return true;
        }

        return false;
    }

    private void CheckJammed()
    {
        for (int i = 0; i < Planks.Count; i++)
        {
            if (!Planks[i].isKinematic)
            {
                Planks.RemoveAt(i);
            }
        }
    }

    private bool GetJointType()
    {
        return dynamicType == Type_Dynamic.Door || dynamicType == Type_Dynamic.Lever;
    }

    private bool CanLockType()
    {
        return dynamicType == Type_Dynamic.Door || dynamicType == Type_Dynamic.Drawer;
    }

    public void UnlockDoor()
    {
        if (isLocked)
        {
            if (UnlockSound) { AudioSource.PlayClipAtPoint(UnlockSound, transform.position, m_Volume); }
            isLocked = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<Rigidbody>() && dynamicType == Type_Dynamic.Drawer)
        {
            collisionObject = collision.transform;
            oldCollisionObjectParent = collisionObject.transform.parent;
            collisionObject.transform.SetParent(transform);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.GetComponent<Rigidbody>() && dynamicType == Type_Dynamic.Drawer)
        {
            collisionObject.transform.SetParent(oldCollisionObjectParent);
            collisionObject = null;
        }
    }

    public void DoorCloseEvent()
    {
        if (Close) { AudioSource.PlayClipAtPoint(Close, transform.position, m_Volume); }
    }

    void OnDrawGizmos()
    {
        if (dynamicType == Type_Dynamic.Door && interactType == Type_Interact.Mouse)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.1f);
        }
    }

    public Dictionary<string, object> OnSave()
    {
        Dictionary<string, object> SaveData = new Dictionary<string, object>();

        if (dynamicType == Type_Dynamic.Door)
        {
            SaveData.Add("use_type", useType);
            SaveData.Add("door_angle", Angle);
            SaveData.Add("is_opened", isOpened);
            SaveData.Add("is_locked", isLocked);
        }
        else if (dynamicType == Type_Dynamic.Drawer)
        {
            SaveData.Add("use_type", useType);
            SaveData.Add("position", new Vector2(transform.position.x, transform.position.z));
            SaveData.Add("is_opened", isOpened);
            SaveData.Add("is_locked", isLocked);
        }
        else if (dynamicType == Type_Dynamic.Lever)
        {
            SaveData.Add("use_type", useType);
            SaveData.Add("lever_angle", Angle);
            SaveData.Add("is_up", isUp);
            SaveData.Add("is_holding", isHolding);
            SaveData.Add("hold", Hold);
            SaveData.Add("invokeUp", invokeUp);
            SaveData.Add("isInvoked", isInvoked);
        }
        else if (dynamicType == Type_Dynamic.Valve)
        {
            SaveData.Add("rotate_value", rotateValue);
        }
        else if (dynamicType == Type_Dynamic.MovableInteract)
        {
            SaveData.Add("is_invoked", isInvoked);
        }

        return SaveData;
    }

    public void OnLoad(JToken token)
    {
        if (dynamicType == Type_Dynamic.Door)
        {
            load = true;
            useSR = false;
            ParseUseType((int)token["use_type"]);
            Vector3 rot = new Vector3(transform.eulerAngles.x, (float)token["door_angle"], transform.eulerAngles.z);
            transform.eulerAngles = rot;
            Angle = (float)token["door_angle"];
            isOpened = (bool)token["is_opened"];
            isLocked = (bool)token["is_locked"];
        }
        else if (dynamicType == Type_Dynamic.Drawer)
        {
            ParseUseType((int)token["use_type"]);
            Vector3 pos = new Vector3((float)token["position"]["x"], transform.position.y, (float)token["position"]["y"]);
            transform.position = pos;
            isOpened = (bool)token["is_opened"];
            isLocked = (bool)token["is_locked"];
        }
        else if (dynamicType == Type_Dynamic.Lever)
        {
            ParseUseType((int)token["use_type"]);

            isUp = (bool)token["is_up"];
            isHolding = (bool)token["is_holding"];
            Hold = (bool)token["hold"];

            invokeUp = (bool)token["invokeUp"];
            isInvoked = (bool)token["isInvoked"];
            loadSound = isUp;

            if (reverseMove)
            {
                Vector3 rot = new Vector3(transform.localEulerAngles.y, (float)token["lever_angle"], transform.localEulerAngles.z);
                transform.localEulerAngles = rot;
            }
            else
            {
                Vector3 rot1 = new Vector3(transform.localEulerAngles.x, (float)token["lever_angle"], transform.localEulerAngles.z);
                transform.localEulerAngles = rot1;
            }

            if (isInvoked)
            {
                if (invokeUp)
                {
                    InteractEvent?.Invoke();
                }
                else
                {
                    DisabledEvent?.Invoke();
                }
            }
        }
        else if (dynamicType == Type_Dynamic.Valve)
        {
            rotateValue = (float)token["rotate_value"];
        }
        else if (dynamicType == Type_Dynamic.MovableInteract)
        {
            isInvoked = (bool)token["is_invoked"];
        }
    }
}
