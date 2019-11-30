/*
 * BodyAnimator.cs - wirted by ThunderWire Games
 * ver. 1.0
*/

using System.Collections;
using UnityEngine;

public class BodyAnimator : MonoBehaviour
{
    private HFPS_GameManager gameManager;
    private PlayerController controller;
    private Animator anim;
    private Transform cam;

    [Header("Main")]
    public Transform MiddleSpine;
    public LayerMask InvisibleMask;

    public float TurnSmooth;
    public float AdjustSmooth;

    public float OverrideSmooth;
    public float BackOverrideSmooth;

    [Header("Right Strafe")]
    public float RightAngle;
    public float UpRightAngle;
    public float BackRightAngle;

    [Header("Left Strafe")]
    public float LeftAngle;
    public float UpLeftAngle;
    public float BackLeftAngle;

    [Header("Speed")]
    public float SlowWalkVelocity;
    public float NormalWalkSpeed;
    public float SlowWalkSpeed;

    [Header("Misc")]
    public float animStartVelocity = 0.2f;
    public float blockStopVelocity = 0.1f;
    public float turnLeftRightDelay = 0.2f;
    public bool enableShadows = true;
    public bool visibleToCamera = true;
    public bool proneDisableBody;

    [Header("Body Adjustment")]
    [Space(10)]
    public Vector3 originalOffset;
    [Space(5)]
    public Vector3 runningOffset;
    [Space(5)]
    public Vector3 crouchOffset;
    [Space(5)]
    public Vector3 jumpOffset;
    [Space(5)]
    public Vector3 proneOffset;
    [Space(5)]
    public Vector3 turnOffset;
    [Space(10)]
    public Vector3 bodyAngle;
    [Space(5)]
    public Vector2 spineMaxRotation;

    private Vector3 localEuler;
    private float tempArmsWeight = 0;

    private float mouseRotation;
    private float spineAngle;

    private bool m_fwd;
    private bool m_bwd;
    private bool m_lt;
    private bool m_rt;

    private Vector3 yRotation;
    private float walkSpeed;

    private bool waitFixed = false;
    private bool blockWalk = false;

    void Awake()
    {
        gameManager = FindObjectOfType<HFPS_GameManager>();
        controller = transform.root.gameObject.GetComponentInChildren<PlayerController>();
        cam = transform.root.gameObject.GetComponentInChildren<ScriptManager>().MainCamera.transform;
        anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        walkSpeed = NormalWalkSpeed;
        anim.SetBool("Idle", true);
        localEuler = transform.localEulerAngles;
        originalOffset = transform.localPosition;

        if (!enableShadows)
        {
            foreach(SkinnedMeshRenderer renderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        if (!visibleToCamera)
        {
            foreach (SkinnedMeshRenderer renderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.gameObject.layer = InvisibleMask;
            }
        }
    }

    void Update()
    {
        mouseRotation = Input.GetAxis("Mouse X");

        m_fwd = Input.GetKey(controller.ForwardKey);
        m_bwd = Input.GetKey(controller.BackwardKey);
        m_lt = Input.GetKey(controller.LeftKey);
        m_rt = Input.GetKey(controller.RightKey);

        anim.SetBool("Crouch", controller.state == 1 || controller.state == 2);

        if (controller.controllable)
        {
            if (!waitFixed)
            {
                StartCoroutine(WaitAfterControllable());
            }
            else
            {
                /* POSITIONING */
                if (controller.run && controller.state != 1 && Input.GetKey(controller.ForwardKey))
                {
                    if (controller.velMagnitude >= animStartVelocity)
                    {
                        transform.localPosition = Vector3.Lerp(transform.localPosition, runningOffset, Time.deltaTime * AdjustSmooth);
                    }
                }
                else if (!controller.IsGrounded())
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, jumpOffset, Time.deltaTime * AdjustSmooth);
                }
                else if (controller.state == 1)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, crouchOffset, Time.deltaTime * AdjustSmooth);
                }
                else if (controller.state == 2)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, proneOffset, Time.deltaTime * AdjustSmooth);
                }
                else if (m_lt && !m_fwd && !m_bwd || m_rt && !m_fwd && !m_bwd)
                {
                    if (controller.velMagnitude >= animStartVelocity)
                    {
                        transform.localPosition = Vector3.Lerp(transform.localPosition, turnOffset, Time.deltaTime * AdjustSmooth);
                    }
                }
                else
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, originalOffset, Time.deltaTime * AdjustSmooth);
                }

                if (controller.velMagnitude >= animStartVelocity)
                {
                    blockWalk = false;

                    if(controller.velMagnitude >= SlowWalkVelocity)
                    {
                        walkSpeed = NormalWalkSpeed;
                    }
                    else
                    {
                        walkSpeed = SlowWalkSpeed;
                    }

                    /* ROTATIONS */
                    if (m_fwd && m_bwd && m_lt && m_rt)
                    {
                        //Fix angle with pressed all direction buttons
                        localEuler.y = Mathf.Lerp(localEuler.y, UpLeftAngle, Time.deltaTime * TurnSmooth);
                    }
                    else
                    {
                        if (m_rt && !m_lt)
                        {
                            //Right, Forward Right, Backward Right
                            if (m_rt && !m_fwd && !m_bwd)
                            {
                                localEuler.y = Mathf.Lerp(localEuler.y, RightAngle, Time.deltaTime * TurnSmooth);
                            }
                            else if (m_rt && m_fwd && !m_bwd)
                            {
                                localEuler.y = Mathf.Lerp(localEuler.y, UpRightAngle, Time.deltaTime * TurnSmooth);
                            }
                            else if (m_rt && !m_fwd && m_bwd)
                            {
                                localEuler.y = Mathf.Lerp(localEuler.y, BackRightAngle, Time.deltaTime * TurnSmooth);
                            }
                            else if (m_rt && m_fwd && m_bwd)
                            {
                                localEuler.y = Mathf.Lerp(localEuler.y, UpRightAngle, Time.deltaTime * TurnSmooth);
                            }
                        }
                        else if (!m_rt && m_lt)
                        {
                            //Left, Forward Left, Backward Left
                            if (m_lt && !m_fwd && !m_bwd)
                            {
                                localEuler.y = Mathf.Lerp(localEuler.y, LeftAngle, Time.deltaTime * TurnSmooth);
                            }
                            else if (m_lt && m_fwd && !m_bwd)
                            {
                                localEuler.y = Mathf.Lerp(localEuler.y, UpLeftAngle, Time.deltaTime * TurnSmooth);
                            }
                            else if (m_lt && !m_fwd && m_bwd)
                            {
                                localEuler.y = Mathf.Lerp(localEuler.y, BackLeftAngle, Time.deltaTime * TurnSmooth);
                            }
                            else if (m_lt && m_fwd && m_bwd)
                            {
                                localEuler.y = Mathf.Lerp(localEuler.y, UpLeftAngle, Time.deltaTime * TurnSmooth);
                            }
                        }
                        else if (m_lt && m_rt)
                        {
                            //Fix angle with pressed both Left Right buttons
                            if (m_fwd)
                            {
                                localEuler.y = Mathf.Lerp(localEuler.y, UpLeftAngle, Time.deltaTime * TurnSmooth);
                            }
                            else if(m_bwd)
                            {
                                localEuler.y = Mathf.Lerp(localEuler.y, BackLeftAngle, Time.deltaTime * TurnSmooth);
                            }
                            else
                            {
                                localEuler.y = Mathf.Lerp(localEuler.y, LeftAngle, Time.deltaTime * TurnSmooth);
                            }
                        }
                    }

                    //Return to Default Rotation
                    if (!m_rt && !m_lt)
                    {
                        localEuler.y = Mathf.Lerp(localEuler.y, 0, Time.deltaTime * TurnSmooth);
                    }

                    if (m_fwd || m_bwd)
                    {
                        if (m_fwd && !m_lt && !m_rt)
                        {
                            anim.SetBool("MoveForward", true);
                        }
                        else if (m_bwd && !m_lt && !m_rt)
                        {
                            anim.SetBool("MoveBackward", true);
                        }
                        else if (m_fwd && m_bwd && !m_lt && !m_rt)
                        {
                            anim.SetBool("MoveForward", true);
                            anim.SetBool("MoveBackward", false);
                        }
                        else if (m_fwd && (m_lt || m_rt))
                        {
                            anim.SetBool("MoveForward", true);
                            anim.SetBool("MoveBackward", false);
                        }
                        else if (m_bwd && (m_lt || m_rt))
                        {
                            anim.SetBool("MoveForward", false);
                            anim.SetBool("MoveBackward", true);
                        }
                        else if ((m_fwd || m_bwd) && (m_lt || m_rt))
                        {
                            anim.SetBool("MoveForward", true);
                            anim.SetBool("MoveBackward", false);
                        }
                    }
                    else if (m_lt || m_rt)
                    {
                        anim.SetBool("MoveForward", true);
                        anim.SetBool("MoveBackward", false);
                    }
                    else
                    {
                        anim.SetBool("MoveForward", false);
                        anim.SetBool("MoveBackward", false);
                    }
                }
                else if(controller.velMagnitude <= blockStopVelocity && !m_lt && !m_rt)
                {
                    localEuler.y = Mathf.Lerp(localEuler.y, 0, Time.deltaTime * TurnSmooth);
                    anim.SetBool("Run", false);
                    anim.SetBool("MoveForward", false);
                    anim.SetBool("MoveBackward", false);
                    blockWalk = true;
                }

                if (!controller.IsGrounded())
                {
                    anim.SetBool("isJumping", true);
                    anim.SetBool("Idle", false);
                    tempArmsWeight = Mathf.Lerp(tempArmsWeight, 1, Time.deltaTime * OverrideSmooth);
                }
                else
                {
                    if (!controller.MoveKeyPressed() || blockWalk)
                    {
                        anim.SetBool("Idle", true);
                    }
                    else
                    {
                        anim.SetBool("Idle", false);
                    }

                    anim.SetBool("isJumping", false);
                    anim.SetBool("Run", controller.run && !blockWalk);
                    tempArmsWeight = Mathf.Lerp(tempArmsWeight, 0, Time.deltaTime * BackOverrideSmooth);
                }

                if (controller.controllable && gameManager.IsEnabled<MouseLook>())
                {
                    if (mouseRotation > 0.1f)
                    {
                        anim.SetBool("TurningRight", true);
                        anim.SetBool("TurningLeft", false);
                    }
                    else if (mouseRotation < -0.1f)
                    {
                        anim.SetBool("TurningRight", false);
                        anim.SetBool("TurningLeft", true);
                    }
                    else if (mouseRotation == 0)
                    {
                        anim.SetBool("TurningRight", false);
                        anim.SetBool("TurningLeft", false);
                    }
                }
                else
                {
                    anim.SetBool("TurningRight", false);
                    anim.SetBool("TurningLeft", false);
                }
            }
        }
        else
        {
            waitFixed = false;

            anim.SetBool("MoveForward", false);
            anim.SetBool("MoveBackward", false);
            anim.SetBool("TurningRight", false);
            anim.SetBool("TurningLeft", false);

            if (controller.IsGrounded())
            {
                tempArmsWeight = Mathf.Lerp(tempArmsWeight, 0, Time.deltaTime * BackOverrideSmooth);
                anim.SetBool("isJumping", false);
                anim.SetBool("Idle", true);

                if (controller.state == 0)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, originalOffset, Time.deltaTime * AdjustSmooth);
                }
            }
            else
            {
                anim.SetBool("isJumping", true);
            }

            localEuler.y = Mathf.Lerp(localEuler.y, 0, Time.deltaTime * TurnSmooth);
        }

        if (proneDisableBody)
        {
            if (transform.localPosition.y <= (proneOffset.y + 0.1) && transform.localPosition.z <= (proneOffset.z + 0.1))
            {
                foreach (SkinnedMeshRenderer smr in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.enabled = false;
                }
            }
            else
            {
                foreach (SkinnedMeshRenderer smr in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.enabled = true;
                }
            }
        }
        else
        {
            foreach (SkinnedMeshRenderer smr in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.enabled = true;
            }

        }

        anim.SetLayerWeight(anim.GetLayerIndex("Arms Layer"), tempArmsWeight);
        anim.SetFloat("WalkSpeed", walkSpeed);

        transform.localEulerAngles = localEuler + bodyAngle;
        Vector3 relative = transform.InverseTransformPoint(cam.position);
        spineAngle = Mathf.RoundToInt(Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg);
        spineAngle = Mathf.Clamp(spineAngle, spineMaxRotation.x, spineMaxRotation.y);
        yRotation = new Vector3(MiddleSpine.localEulerAngles.x, spineAngle, MiddleSpine.localEulerAngles.z);
    }

    IEnumerator WaitAfterControllable()
    {
        yield return new WaitForFixedUpdate();
        waitFixed = true;
    }

    void LateUpdate()
    {
        MiddleSpine.localRotation = Quaternion.Euler(yRotation);
        anim.transform.localPosition = Vector3.zero;
    }
}
