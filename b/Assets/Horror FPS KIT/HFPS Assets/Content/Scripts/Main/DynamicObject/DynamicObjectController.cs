using UnityEngine;

public class DynamicObjectController : MonoBehaviour
{
    private InputController inputController;
    private HFPS_GameManager gameManager;
    private DynamicObject dynamicObj;
    private DelayEffect delay;
    private Camera mainCamera;

    [Header("Raycast")]
    public LayerMask CullLayers;
    public float MaxHoldDistance;

    [Header("Dynamic Settings")]
    public float doorMoveSpeed;
    public float drawerMoveSpeed;
    public float leverMoveSpeed;

    private float RayLength;
    private string InteractLayer;

    private KeyCode UseKey;
    private GameObject raycastObject;

    private float mouseX;
    private float mouseY;

    private bool isOutOfDistance;
    private bool isOtherHolding;
    private bool isHolding;
    private bool isDynamic;
    private bool firstPass;
    private bool alrLock;

    void Awake()
    {
        mainCamera = ScriptManager.Instance.MainCamera;
    }

    void Start()
    {
        inputController = InputController.Instance;
        gameManager = HFPS_GameManager.Instance;
        delay = transform.root.GetComponentInChildren<DelayEffect>(true);
        InteractLayer = GetComponent<InteractManager>().InteractLayer;
        RayLength = GetComponent<InteractManager>().RaycastRange;
    }

    void Update()
    {
        if (inputController.HasInputs())
        {
            UseKey = inputController.GetInput("Use");
        }

        //Prevent Interact Dynamic Object when player is holding other object
        isOtherHolding = GetComponent<DragRigidbody>().CheckHold();

        if (raycastObject && !isOtherHolding && isDynamic && !isOutOfDistance)
        {
            if (Input.GetKey(UseKey))
            {
                if (!firstPass)
                {
                    FirstPass();
                }
                else
                {
                    UseDynamicObject(dynamicObj.dynamicType);
                    isHolding = true;
                }
            }
            else if (isHolding)
            {
                ReleaseObject();
                isHolding = false;
            }
        }

        if (isHolding)
        {
            gameManager.LockScript<MouseLook>(false);
            delay.isEnabled = false;
            alrLock = false;

            if (raycastObject && raycastObject.GetComponent<DynamicObject>())
            {
                raycastObject.GetComponent<DynamicObject>().isHolding = true;
            }

            isOutOfDistance = Vector3.Distance(raycastObject.transform.position, transform.root.position) >= MaxHoldDistance;
        }
        else
        {
            if (!alrLock)
            {
                gameManager.LockScript<MouseLook>(true);
                alrLock = true;
            }

            if (raycastObject && raycastObject.GetComponent<DynamicObject>())
            {
                raycastObject.GetComponent<DynamicObject>().isHolding = false;
            }
        }

        if (isOutOfDistance)
        {
            ReleaseObject();
        }

        Ray playerAim = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(playerAim, out RaycastHit hit, RayLength, CullLayers) && !isHolding)
        {
            if (IsDynamicObject(hit))
            {
                raycastObject = hit.collider.gameObject;
                dynamicObj = raycastObject.GetComponent<DynamicObject>();
                isDynamic = true;
            }
            else if(raycastObject && !isHolding)
            {
                raycastObject = null;
                isDynamic = false;
            }
        }
        else if (raycastObject && !isHolding)
        {
            raycastObject = null;
        }

        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
    }

    private bool IsDynamicObject(RaycastHit hit)
    {
        GameObject raycastObj = hit.collider.gameObject;
        return hit.collider.gameObject.layer == LayerMask.NameToLayer(InteractLayer) && raycastObj.GetComponent<DynamicObject>();
    }

    private void ReleaseObject()
    {
        StopAllCoroutines();
        isHolding = false;
        delay.isEnabled = true;
        isOutOfDistance = false;
        firstPass = false;
        dynamicObj = null;
        raycastObject = null;
        gameManager.DisableValveSlider();
    }

    void FirstPass()
    {
        if (dynamicObj.dynamicType == Type_Dynamic.Valve) { ValvePass(); }
        firstPass = true;
    }

    private void UseDynamicObject(Type_Dynamic DynamicType)
    {
        switch (DynamicType)
        {
            case Type_Dynamic.Door:             IsDoor();   break;
            case Type_Dynamic.Drawer:           IsDrawer(); break;
            case Type_Dynamic.Lever:            IsLever();  break;
            case Type_Dynamic.Valve:            IsValve();  break;
            case Type_Dynamic.MovableInteract:  IsDrawer(); break;
        }
    }

    private void IsDoor()
    {
        if (!GetDynamicUseType()) return;

        if (dynamicObj.interactType == Type_Interact.Mouse)
        {
            HingeJoint joint = raycastObject.GetComponent<HingeJoint>();
            JointMotor motor = joint.motor;
            float mouseForce = mouseX;

            //TODO: Door mouse rotate direction by look angle

            motor.targetVelocity = mouseForce * (doorMoveSpeed * 10);
            motor.force = (doorMoveSpeed * 10);
            joint.motor = motor;
            joint.useMotor = true;
        }
    }

    private void IsDrawer()
    {
        if (!GetDynamicUseType()) return;

        if (dynamicObj.interactType == Type_Interact.Mouse)
        {
            if (dynamicObj.reverseMove)
            {
                mouseY = -mouseY;
            }

            mouseY *= drawerMoveSpeed;

            if (!dynamicObj.moveWithX)
            {
                Vector3 pos = raycastObject.transform.localPosition;
                pos.z += mouseY * 0.1f;
                raycastObject.transform.localPosition = new Vector3(pos.x, pos.y, Mathf.Clamp(pos.z, dynamicObj.minMaxMove.x, dynamicObj.minMaxMove.y));
            }
            else
            {
                Vector3 pos = raycastObject.transform.localPosition;
                pos.x += mouseY * 0.1f;
                raycastObject.transform.localPosition = new Vector3(Mathf.Clamp(pos.x, dynamicObj.minMaxMove.x, dynamicObj.minMaxMove.y), pos.y, pos.z);
            }
        }
    }

    private void IsLever()
    {
        if (dynamicObj.interactType == Type_Interact.Mouse)
        {
            HingeJoint joint = raycastObject.GetComponent<HingeJoint>();
            JointMotor motor = joint.motor;
            motor.targetVelocity = mouseY * (leverMoveSpeed * 10);
            motor.force = (leverMoveSpeed * 10);
            joint.motor = motor;
            joint.useMotor = true;
        }
    }

    private void IsValve()
    {
        if (!(dynamicObj.rotateValue >= 1f))
        {
            float z = dynamicObj.valveTurnSpeed;
            Vector3 rotation = new Vector3(0, 0, -z);
            raycastObject.transform.Rotate(rotation);
            dynamicObj.rotateValue = gameManager.ValveSlider.value;
        }
    }

    void ValvePass()
    {
        if (!(dynamicObj.rotateValue >= 1f))
        {
            gameManager.NewValveSlider(dynamicObj.rotateValue, dynamicObj.valveTurnTime);
        }
    }

    private bool GetDynamicUseType()
    {
        if (dynamicObj && !dynamicObj.hasKey)
        {
            if (dynamicObj.useType == Type_Use.Locked || dynamicObj.useType == Type_Use.Jammed)
            {
                if (!string.IsNullOrEmpty(dynamicObj.customText))
                {
                    gameManager.ShowHint(dynamicObj.customText);
                }

                return false;
            }
        }

        return true;
    }
}
