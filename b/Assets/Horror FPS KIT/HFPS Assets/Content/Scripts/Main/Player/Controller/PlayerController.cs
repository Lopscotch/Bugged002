/*
 * PlayerController.cs - wirted by ThunderWire Games
 * ver. 2.0
*/

using UnityEngine;
using System.Collections;
using ThunderWire.Helpers;

[RequireComponent(typeof(Footsteps))]
public class PlayerController : Singleton<PlayerController>
{
    [Header("Main Setup")]
    public CharacterController controller;
    public ScriptManager scriptManager;

    private ItemSwitcher switcher;
    private InputController inputManager;
    private HealthManager hm;
    private Footsteps footsteps;

    [Header("Movement Speed")]
    public float walkSpeed = 4;
    public float runSpeed = 7;
    public float crouchSpeed = 2;
    public float proneSpeed = 1;
    public float inWaterSpeed = 2;
    public float inWaterJumpSpeed = 5;
    public float jumpSpeed = 7;
    public float climbSpeed = 1.5f;
    public float climbRate = 0.5f;
    public float pushSpeed = 0.2f;
    public float startWalkSpeed = 2.0f;
    public float stopWalkSpeed = 2.2f;
    public float directionChangeSpeed = 2.0f;
    public int stateTransitionSpeed = 3;

    [Header("Player")]
    public float baseGravity = 24;
    public float fallDamageMultiplier = 5.0f;
    public float standFallTreshold = 8;
    public float crouchFallTreshold = 4;
    public float slideLimit = 45.0f;
    public bool airControl = false;

    [Header("Player Adjustments")]
    public float playerNormalHeight = 2.0f;
    public float playerCrouchHeight = 1.4f;
    public float playerProneHeight = 0.6f;

    [Header("Camera Adjustments")]
    public float camNormalHeight = 0.9f;
    public float camCrouchHeight = 0.2f;
    public float camProneHeight = -0.4f;

    [Header("Controller Adjustments")]
    public Vector3 normalCenter = Vector3.zero;
    public Vector3 crouchCenter = new Vector3(0, -0.3f, 0);
    public Vector3 proneCenter = new Vector3(0, -0.7f, 0);

    [Header("Distance Settings")]
    public float groundCheckOffset;
    public float groundCheckRadius;

    private float rayDistance;
    private float gravity = 24;
    private float fallingDamageThreshold;
    private float fallDistance;
    private float slideSpeed = 8.0f;
    private float antiBumpFactor = .75f;
    private float antiBunnyHopFactor = 1;
    private float spamWaitTime = 0.5f;
    private bool antiSpam;
    private bool sliding = false;
    private bool falling = false;

    [Header("Animations Camera")]
    public Animation cameraAnimations;
    [SerializeField] private string runAnimation = "CameraRun";
    [Range(0, 5)] public float runAnimSpeed;
    [SerializeField] private string walkAnimation = "CameraWalk";
    [Range(0, 5)] public float walkAnimSpeed;
    [SerializeField] private string idleAnimation = "CameraIdle";

    [Header("Animations Arms")]
    public Animation armsAnimations;
    [SerializeField] private string armsRunAnimation = "ArmsRun";
    [SerializeField] private string armsWalkAnimation = "ArmsWalk";
    [SerializeField] private string armsIdleAnimation = "ArmsIdle";
    public float adjustAnimSpeed = 7.0f;

    [Header("In Water")]
    public ParticleSystem waterFoam;
    [HideInInspector] public ParticleSystem emiter;

    [Header("Other")]
    public LayerMask checkDistanceMask;
    public Transform playerWeapGO;
    public Transform cameraGO;
    public Transform fallEffect;
    public Transform fallEffectWeap;
    public AudioSource aSource;

    [HideInInspector]
    public int state = 0;

    [HideInInspector]
    public bool onLadder = false;

    [HideInInspector]
    Vector3 moveDirection = Vector3.zero;

    [HideInInspector]
    public bool run;

    [HideInInspector]
    public bool canRun = true;

    [HideInInspector]
    public bool grounded = false;

    [HideInInspector]
    public bool controllable = true;

    [HideInInspector]
    public bool shakeCamera = false;

    [HideInInspector]
    public float speed;

    [HideInInspector]
    public float velMagnitude;

    private RaycastHit hit;
    private bool useLadder = true;
    private bool ladderJump = false;
    private bool walkStarted = false;

    private float stateVelocity;
    private float playTime = 0.0f;
    private float climbDownThreshold = -0.4f;
    private float highestPoint;
    private int jumpTimer;

    private Vector3 climbDirection = Vector3.up;
    private Vector3 lateralMove = Vector3.zero;
    private Vector3 ladderMovement = Vector3.zero;
    private Vector3 contactPoint;
    private Vector3 currentPosition;
    private Vector3 lastPosition;

    [HideInInspector] public KeyCode ForwardKey;
    [HideInInspector] public KeyCode BackwardKey;
    [HideInInspector] public KeyCode LeftKey;
    [HideInInspector] public KeyCode RightKey;

    private KeyCode JumpKey;
    private KeyCode RunKey;
    private KeyCode CrouchKey;
    private KeyCode ProneKey;
    private KeyCode ZoomKey;

    /// <summary>
    /// Vertical Movement
    /// Up, Down
    /// </summary>
    [HideInInspector] public float inputY;

    /// <summary>
    /// Horizontal Movement
    /// Left, Right
    /// </summary>
	[HideInInspector] public float inputX;

    void Awake()
    {
        inputManager = scriptManager.GetScript<InputController>();
        hm = GetComponent<HealthManager>();
        footsteps = GetComponent<Footsteps>();
    }

    void Start()
    {
        rayDistance = controller.height / 2 + 1.1f;
        slideLimit = controller.slopeLimit - .2f;

        cameraAnimations.wrapMode = WrapMode.Loop;
        armsAnimations.wrapMode = WrapMode.Loop;
        armsAnimations.Stop();

        cameraAnimations[runAnimation].speed = runAnimSpeed;
        cameraAnimations[walkAnimation].speed = walkAnimSpeed;

        switcher = scriptManager.GetScript<ItemSwitcher>();
    }

    void Update()
    {
        transform.SetSiblingIndex(1);

        if (inputManager && inputManager.HasInputs())
        {
            ForwardKey = inputManager.GetInput("Forward");
            BackwardKey = inputManager.GetInput("Backward");
            LeftKey = inputManager.GetInput("Left");
            RightKey = inputManager.GetInput("Right");
            JumpKey = inputManager.GetInput("Jump");
            RunKey = inputManager.GetInput("Run");
            CrouchKey = inputManager.GetInput("Crouch");
            ProneKey = inputManager.GetInput("Prone");
            ZoomKey = inputManager.GetInput("Zoom");
        }

        velMagnitude = controller.velocity.magnitude;

        /* For Console Controller
        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxis("Vertical");
        */

        if (controllable)
        {
            if (Input.GetKey(ForwardKey))
            {
                inputY = InputHelper.GetKeyAxis(Axis.In, inputY, true, walkStarted ? directionChangeSpeed : startWalkSpeed);
            }
            else if (inputY > 0)
            {
                inputY = InputHelper.GetKeyAxis(Axis.In, inputY, false, walkStarted ? directionChangeSpeed : stopWalkSpeed);
            }
            else if (Input.GetKey(BackwardKey))
            {
                inputY = InputHelper.GetKeyAxis(Axis.Out, inputY, true, walkStarted ? directionChangeSpeed : startWalkSpeed);
            }
            else if (inputY < 0)
            {
                inputY = InputHelper.GetKeyAxis(Axis.Out, inputY, false, walkStarted ? directionChangeSpeed : stopWalkSpeed);
            }

            if (Input.GetKey(LeftKey))
            {
                inputX = InputHelper.GetKeyAxis(Axis.Out, inputX, true, walkStarted ? directionChangeSpeed : startWalkSpeed);
            }
            else if (inputX < 0)
            {
                inputX = InputHelper.GetKeyAxis(Axis.Out, inputX, false, walkStarted ? directionChangeSpeed : stopWalkSpeed);
            }
            else if (Input.GetKey(RightKey))
            {
                inputX = InputHelper.GetKeyAxis(Axis.In, inputX, true, walkStarted ? directionChangeSpeed : startWalkSpeed);
            }
            else if (inputX > 0)
            {
                inputX = InputHelper.GetKeyAxis(Axis.In, inputX, false, walkStarted ? directionChangeSpeed : stopWalkSpeed);
            }

            if(inputY >= 1 || inputY <= -1 || inputX >= 1 || inputX <= -1)
            {
                walkStarted = true;
            }

            if(inputY == 0 || inputX == 0)
            {
                walkStarted = false;
            }
        }
        else
        {
            inputX = 0f;
            inputY = 0f;
        }

        float inputModifyFactor = (inputX != 0.0f && inputY != 0.0f) ? .7071f : 1.0f;

        if (onLadder)
        {
            LadderUpdate();
            highestPoint = transform.position.y;
            run = false;
            fallDistance = 0.0f;
            grounded = false;
            armsAnimations.CrossFade(armsIdleAnimation);
            cameraAnimations.CrossFade(idleAnimation);
            return;
        }

        if (grounded)
        {
            gravity = baseGravity;

            if (Physics.Raycast(transform.position, -Vector3.up, out hit, rayDistance))
            {
                float hitangle = Vector3.Angle(hit.normal, Vector3.up);
                if (hitangle > slideLimit)
                {
                    sliding = true;
                }
                else
                {
                    sliding = false;
                }
            }

            if (canRun && state == 0)
            {
                if (Input.GetKey(RunKey) && Input.GetKey(ForwardKey) && !Input.GetKey(ZoomKey) && !footsteps.inWater && controllable)
                {
                    run = true;
                }
                else
                {
                    run = false;
                }
            }

            if (falling)
            {
                falling = false;
                fallDistance = highestPoint - currentPosition.y;

                if (fallDistance > fallingDamageThreshold)
                {
                    ApplyFallingDamage(fallDistance);
                }

                if (fallDistance < fallingDamageThreshold && fallDistance > 0.1f)
                {
                    if (state < 2) footsteps.JumpLand();
                    StartCoroutine(FallCamera(new Vector3(7, Random.Range(-1.0f, 1.0f), 0), new Vector3(3, Random.Range(-0.5f, 0.5f), 0), 0.15f));
                }
            }

            if (sliding)
            {
                Vector3 hitNormal = hit.normal;
                moveDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
                Vector3.OrthoNormalize(ref hitNormal, ref moveDirection);
                moveDirection *= slideSpeed;
                sliding = false;
            }
            else
            {
                if (state == 0)
                {
                    if (run)
                    {
                        speed = runSpeed;
                    }
                    else if (Input.GetKey(ZoomKey))
                    {
                        speed = crouchSpeed;
                    }
                    else if (!footsteps.inWater)
                    {
                        speed = walkSpeed;
                    }
                    else
                    {
                        speed = inWaterSpeed;
                    }
                }
                else if (state == 1)
                {
                    speed = crouchSpeed;
                    run = false;
                }
                else if (state == 2)
                {
                    speed = proneSpeed;
                    run = false;
                }

                moveDirection = new Vector3(inputX * inputModifyFactor, -antiBumpFactor, inputY * inputModifyFactor);
                moveDirection = transform.TransformDirection(moveDirection);
                moveDirection *= speed;

                if (controllable)
                {
                    if (!Input.GetKey(JumpKey))
                    {
                        jumpTimer++;
                    }
                    else if (jumpTimer >= antiBunnyHopFactor)
                    {
                        jumpTimer = 0;
                        if (state == 0)
                        {
                            if (!footsteps.inWater)
                            {
                                moveDirection.y = jumpSpeed;
                            }
                            else
                            {
                                moveDirection.y = inWaterJumpSpeed;
                            }
                        }

                        if (state > 0)
                        {
                            if (CheckDistance() > 1.6f)
                            {
                                state = 0;
                                StartCoroutine(AntiSpam());
                            }
                        }
                    }
                }
            }
        }
        else
        {
            currentPosition = transform.position;
            if (currentPosition.y > lastPosition.y)
            {
                highestPoint = transform.position.y;
                falling = true;
            }

            if (!falling)
            {
                highestPoint = transform.position.y;
                falling = true;
            }

            if (airControl)
            {
                moveDirection.x = inputX * speed;
                moveDirection.z = inputY * speed;
                moveDirection = transform.TransformDirection(moveDirection);
            }
        }

        if (grounded)
        {
            if (!shakeCamera)
            {
                if (!run && velMagnitude > crouchSpeed)
                {
                    armsAnimations[armsWalkAnimation].speed = velMagnitude / adjustAnimSpeed;
                    armsAnimations.CrossFade(armsWalkAnimation);
                    cameraAnimations.CrossFade(walkAnimation);
                }
                else if (run && velMagnitude > walkSpeed)
                {
                    armsAnimations.CrossFade(armsRunAnimation);
                    cameraAnimations.CrossFade(runAnimation);
                }
                else if (velMagnitude < crouchSpeed)
                {
                    armsAnimations.CrossFade(armsIdleAnimation);
                    cameraAnimations.CrossFade(idleAnimation);
                }
            }
        }
        else
        {
            if (!shakeCamera)
            {
                armsAnimations.CrossFade(armsIdleAnimation);
                cameraAnimations.CrossFade(idleAnimation);
            }

            run = false;
        }

        if (!footsteps.inWater && controllable && !antiSpam)
        {
            if (Input.GetKeyDown(CrouchKey))
            {
                if (state == 0 || state == 2)
                {
                    if (CheckDistance() > 1.6f)
                    {
                        state = 1;
                    }
                }
                else if (state > 0)
                {
                    if (CheckDistance() > 1.6f)
                    {
                        state = 0;
                    }
                }

                StartCoroutine(AntiSpam());
            }

            if (Input.GetKeyDown(ProneKey))
            {
                if (state < 2)
                {
                    state = 2;
                }
                else if (state == 2)
                {
                    if (CheckDistance() > 1.6f)
                    {
                        state = 0;
                    }
                }

                StartCoroutine(AntiSpam());
            }
        }

        if (state == 0)
        { 
            //Stand Position
            controller.height = playerNormalHeight;
            controller.center = normalCenter;
            fallingDamageThreshold = standFallTreshold;

            if(cameraGO.localPosition.y < camNormalHeight)
            {
                float smooth = Mathf.Lerp(cameraGO.localPosition.y, camNormalHeight, Time.deltaTime * stateTransitionSpeed);
                cameraGO.localPosition = new Vector3(cameraGO.localPosition.x, smooth, cameraGO.localPosition.z);
            }
            else
            {
                cameraGO.localPosition = new Vector3(cameraGO.localPosition.x, camNormalHeight, cameraGO.localPosition.z);
            }
        }
        else if (state == 1)
        { 
            //Crouch Position
            controller.height = playerCrouchHeight;
            controller.center = crouchCenter;
            fallingDamageThreshold = crouchFallTreshold;

            if (cameraGO.localPosition.y < camCrouchHeight || cameraGO.localPosition.y > camCrouchHeight)
            {
                float smooth = Mathf.Lerp(cameraGO.localPosition.y, camCrouchHeight, Time.deltaTime * stateTransitionSpeed);
                cameraGO.localPosition = new Vector3(cameraGO.localPosition.x, smooth, cameraGO.localPosition.z);
            }
            else
            {
                cameraGO.localPosition = new Vector3(cameraGO.localPosition.x, camCrouchHeight, cameraGO.localPosition.z);
            }
        }
        else if (state == 2)
        { 
            //Prone Position
            controller.height = playerProneHeight;
            controller.center = proneCenter;
            fallingDamageThreshold = crouchFallTreshold;

            if (cameraGO.localPosition.y > camProneHeight)
            {
                float smooth = Mathf.Lerp(cameraGO.localPosition.y, camProneHeight, Time.deltaTime * stateTransitionSpeed);
                cameraGO.localPosition = new Vector3(cameraGO.localPosition.x, smooth, cameraGO.localPosition.z);
            }
            else
            {
                cameraGO.localPosition = new Vector3(cameraGO.localPosition.x, camProneHeight, cameraGO.localPosition.z);
            }
        }

        moveDirection.y -= gravity * Time.deltaTime;
        grounded = (controller.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
    }

    public bool MoveKeyPressed()
    {
        if (Input.GetKey(ForwardKey) || Input.GetKey(BackwardKey) || Input.GetKey(LeftKey) || Input.GetKey(RightKey))
        {
            return true;
        }

        return false;
    }

    float CheckDistance()
    {
        Vector3 pos = transform.position + controller.center - new Vector3(0, controller.height / 2, 0);

        if (Physics.SphereCast(pos, controller.radius, transform.up, out RaycastHit hit, 10, checkDistanceMask))
        {
            Debug.DrawLine(pos, hit.point, Color.yellow, 2.0f);
            return hit.distance;
        }
        else
        {
            Debug.DrawLine(pos, hit.point, Color.yellow, 2.0f);
            return 3;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        //dont move the rigidbody if the character is on top of it
        if (controller.collisionFlags == CollisionFlags.Below)
        {
            return;
        }

        if (body == null || body.isKinematic)
        {
            return;
        }

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        body.velocity = pushDir * pushSpeed;
    }

    void LateUpdate()
    {
        lastPosition = currentPosition;
    }

    public bool IsGrounded()
    {
        Vector3 pos = transform.position + controller.center - new Vector3(0, (controller.height / 2f) + groundCheckOffset, 0);

        if (Physics.OverlapSphere(pos, groundCheckRadius, checkDistanceMask).Length > 0 || grounded)
        {
            return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Vector3 pos = transform.position + controller.center - new Vector3(0, (controller.height / 2f) + groundCheckOffset, 0);
        Gizmos.DrawWireSphere(pos, groundCheckRadius);
    }

    IEnumerator FallCamera(Vector3 d, Vector3 dw, float ta)
    {
        Quaternion s = fallEffect.localRotation;
        Quaternion sw = fallEffectWeap.localRotation;
        Quaternion e = fallEffect.localRotation * Quaternion.Euler(d);
        // Quaternion ew = fallEffectWeap.localRotation * Quaternion.Euler(dw);
        float r = 1.0f / ta;
        float t = 0.0f;
        while (t < 1.0f)
        {
            t += Time.deltaTime * r;
            fallEffect.localRotation = Quaternion.Slerp(s, e, t);
            fallEffectWeap.localRotation = Quaternion.Slerp(sw, e, t);
            yield return null;
        }
    }

    void ApplyFallingDamage(float fallDistance)
    {
        hm.ApplyDamage(fallDistance * fallDamageMultiplier);
        if (state < 2) StartCoroutine(footsteps.JumpLand());
        StartCoroutine(FallCamera(new Vector3(12, Random.Range(-2.0f, 2.0f), 0), new Vector3(4, Random.Range(-1.0f, 1.0f), 0), 0.1f));
    }

    public void PlayerInWater(float top)
    {
        Vector3 foamPos = transform.position;
        foamPos.y = top;

        if (emiter == null)
        {
            emiter = Instantiate(waterFoam, foamPos, transform.rotation) as ParticleSystem;
        }

        emiter.transform.position = foamPos;
    }

    void OnTriggerStay(Collider collider)
    {
        if (collider.tag == "Ladder" && useLadder && !ladderJump)
        {
            switcher.FreeHands(true);
            onLadder = true;
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (collider.tag == "Ladder")
        {
            switcher.FreeHands(false);
            onLadder = false;
            useLadder = true;
            ladderJump = false;
        }
    }

    //Ladder Movement
    private void LadderUpdate()
    {
        float cameraRotation = scriptManager.MainCamera.transform.forward.y;

        if (onLadder)
        {
            Vector3 verticalMove;
            verticalMove = climbDirection.normalized;
            verticalMove *= inputY;
            verticalMove *= (cameraRotation > climbDownThreshold) ? 1 : -1;
            lateralMove = new Vector3(inputX, 0, inputY);
            lateralMove = transform.TransformDirection(lateralMove);
            ladderMovement = verticalMove + lateralMove;
            controller.Move(ladderMovement * climbSpeed * Time.deltaTime);

            if (inputY == 1 && !(aSource.isPlaying) && Time.time >= playTime)
            {
                PlayLadderSound();
            }

            if (Input.GetKey(JumpKey))
            {
                useLadder = false;
                onLadder = false;
                ladderJump = true;
            }
        }
    }

    //Ladder Footsteps
    void PlayLadderSound()
    {
        footsteps.PlayLadderSound();
        playTime = Time.time + climbRate;
    }

    IEnumerator AntiSpam()
    {
        antiSpam = true;
        yield return new WaitForSeconds(spamWaitTime);
        antiSpam = false;
    }
}