/*
 * DragRigidbody.cs - wirted by ThunderWire Games
 * ver. 1.22
*/

using UnityEngine;
using System.Collections;

public class DragRigidbody : MonoBehaviour {

    private Camera playerCam;
    private HFPS_GameManager gameManager;
    private InteractManager interact;
	private InputController inputManager;
	private PlayerFunctions pfunc;
	private DelayEffect delay;
    private ItemSwitcher itemSwitcher;

    [Header("Main")]
    public LayerMask CullLayers;
    public string GrabLayerName = "Interact";
    public string GrabTag = "Grab";

    [Header("Settings")]
	public float ThrowStrength = 2f;
    public float DragSpeed = 10f;
	public float minDistanceZoom = 1.5f;
	public float maxDistanceZoom = 3f;
	public float maxDistanceGrab = 4f;
	public float spamWaitTime = 0.5f;

    private float PickupRange = 3f;
    private float distance;
	
	[Header("Other")]
	public float rotateSpeed = 10f;
	public float rotationDeadzone = 0.1f;
	public float objectZoomSpeed = 3f;	

    [Space(7)]
	public bool FreezeRotation = true;
	public bool enableObjectPull = true;
	public bool enableObjectRotation = true;
	public bool enableObjectZooming = true;
    public bool dragHideWeapon;
    public bool fixedHold = false;

    private Ray playerAim;
    private GameObject objectHeld;	
	private GameObject objectRaycast;

	private bool GrabObject;
	private bool isObjectHeld;
	private bool isPressed;
    private bool throwPressed = false;
	private bool antiSpam;
    private Transform oldParent;
	
	private KeyCode rotateObject;
	private KeyCode GrabButton;
	private KeyCode ThrowButton;

    private void Awake()
    {
        delay = gameObject.transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).gameObject.GetComponent<DelayEffect>();
        interact = GetComponent<InteractManager>();
        inputManager = InputController.Instance;
        gameManager = HFPS_GameManager.Instance;
        pfunc = GetComponent<PlayerFunctions>();
        playerCam = ScriptManager.Instance.MainCamera;
        PickupRange = interact.RaycastRange;
    }

	void Start () {
        itemSwitcher = GetComponent<ScriptManager>().GetScript<ItemSwitcher>();
        isObjectHeld = false;
		isPressed = false;
		objectHeld = null;
	}
	
	void Update()
	{
        if (inputManager && inputManager.HasInputs())
        {
            GrabButton = inputManager.GetInput("Pickup");
            ThrowButton = inputManager.GetInput("Throw");
            rotateObject = inputManager.GetInput("Fire");
        }

        gameManager.isHeld = objectHeld != false;
		interact.isHeld = objectHeld != false;

        if (gameManager.isPaused) return;

		if(objectRaycast && !antiSpam)
		{
			if(Input.GetKeyDown(GrabButton) && !isPressed){
				isPressed = true;
				GrabObject = !GrabObject;
			}else if(isPressed){
				isPressed = false;
			}
		}

		if (GrabObject){
			if (!isObjectHeld){
                TryPickObject();
			}else{
				HoldObject();
			}
		}else if(isObjectHeld){
			DropObject();
		}

        if (Input.GetKey(ThrowButton))
        {
            if (objectHeld)
            {
                throwPressed = true;
                isObjectHeld = false;
                ThrowObject();
            }
        }
        else if (throwPressed)
        {
            pfunc.zoomEnabled = true;
            throwPressed = false;
        }
		
		Ray playerAim = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
		RaycastHit hit;

		float rotationInputX = 0.0f;
		float rotationInputY = 0.0f;

		float x = Input.GetAxis("Mouse X");
		float y = Input.GetAxis("Mouse Y");

		if(Mathf.Abs(x) > rotationDeadzone){
			rotationInputX = -(x * rotateSpeed);
		}

		if(Mathf.Abs(y) > rotationDeadzone){
			rotationInputY = (y * rotateSpeed);
		}
		
		if (Physics.Raycast (playerAim, out hit, PickupRange, CullLayers)) {
			if (hit.collider.gameObject.layer == LayerMask.NameToLayer(GrabLayerName)) {
                if (hit.collider.tag == GrabTag) {
					objectRaycast = hit.collider.gameObject;
                    gameManager.canGrab = true;
				}
			} else {
				if (!isObjectHeld) {
					objectRaycast = null;
                    gameManager.canGrab = false;
				}
			}
		
			if (objectHeld) {
                if (Input.GetKey(rotateObject) && enableObjectRotation)
                {
                    gameManager.LockPlayerControls(false, false, false);
                    objectHeld.GetComponent<Rigidbody>().freezeRotation = true;
                    objectHeld.transform.Rotate(playerCam.transform.up, rotationInputX, Space.World);
                    objectHeld.transform.Rotate(playerCam.transform.right, rotationInputY, Space.World);
                }
                else
                {
                    gameManager.LockPlayerControls(true, false, false);
                }

				if (enableObjectZooming) {
					distance = Mathf.Clamp (distance, minDistanceZoom, maxDistanceZoom);
                    float mw = Input.GetAxis("Mouse ScrollWheel") * objectZoomSpeed;
                    if (mw > 0 && distance < maxDistanceZoom)
                    {
                        distance += mw;
                    }
                    else if(mw < 0 && distance > minDistanceZoom)
                    {
                        distance += mw;
                    }
                }
			}
		} else {
			if (!isObjectHeld) {
				objectRaycast = null;
                gameManager.canGrab = false;
			}
		}
    }
	
	private void TryPickObject(){
		StartCoroutine (AntiSpam ());

        if (objectRaycast && !objectRaycast.GetComponent<Rigidbody>())
        {
            Debug.LogError("[DragRigidbody] " + objectRaycast.name + " does not contains a Rigidbody component!");
            GrabObject = false;
            return;
        }

        objectHeld = objectRaycast;

        if (enableObjectPull) {
			if (!objectHeld.GetComponent<DragDistance> ()) {
				float dist = Vector3.Distance (transform.position, objectHeld.transform.position);
				if (dist > maxDistanceZoom - 1f) {
					distance = minDistanceZoom + 0.25f;
				} else {
					distance = dist;
				}
			} else {
                distance = objectHeld.GetComponent<DragDistance>().dragDistance;
            }
		}

		objectHeld.GetComponent<Rigidbody> ().useGravity = false;
        if (objectHeld.GetComponent<Rigidbody>().isKinematic)
        {
            objectHeld.GetComponent<Rigidbody>().isKinematic = false;
        }

        if (FreezeRotation) {
			objectHeld.GetComponent<Rigidbody> ().freezeRotation = true;
		} else {
			objectHeld.GetComponent<Rigidbody> ().freezeRotation = false;
		}

        if (fixedHold)
        {
            oldParent = objectHeld.transform.parent;
            objectHeld.transform.SetParent(playerCam.transform);
        }

        if (dragHideWeapon)
        {
            itemSwitcher.FreeHands(true);
        }

        delay.isEnabled = false;
        gameManager.UIPreventOverlap(true);
        gameManager.ShowGrabSprites();
        gameManager.HideSprites(hideType.Examine);
        gameManager.isGrabbed = true;
        interact.CrosshairVisible(false);
        pfunc.zoomEnabled = false;
        GetComponent<ScriptManager>().ScriptEnabledGlobal = false;

        Physics.IgnoreCollision (objectHeld.GetComponent<Collider>(), transform.root.GetComponent<Collider> (), true);
        objectHeld.SendMessage("OnRigidbodyDrag", SendMessageOptions.DontRequireReceiver);

        StartCoroutine(DropDistance());

		isObjectHeld = true;
	}
	
	private void HoldObject(){
        gameManager.HideSprites(hideType.Interact);

        Ray playerAim = playerCam.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
		
		Vector3 nextPos = playerCam.transform.position + playerAim.direction * distance;
		Vector3 currPos = objectHeld.transform.position;

        objectHeld.GetComponent<Rigidbody>().velocity = (nextPos - currPos) * DragSpeed;
	}

    IEnumerator DropDistance()
    {
        yield return new WaitForSeconds(1f);
        while (objectHeld)
        {
            if (Vector3.Distance(objectHeld.transform.position, playerCam.transform.position) > maxDistanceGrab)
            {
                DropObject();
            }
            yield return null;
        }
    }

	public bool CheckHold(){
		return isObjectHeld;
	}
	
    private void DropObject()
    {
        gameManager.LockPlayerControls(true, true, false);
        gameManager.UIPreventOverlap(false);
        gameManager.HideSprites (hideType.Grab);
        gameManager.isGrabbed = false;
        interact.CrosshairVisible (true);
		objectHeld.GetComponent<Rigidbody>().useGravity = true;
		objectHeld.GetComponent<Rigidbody>().freezeRotation = false;
		Physics.IgnoreCollision(objectHeld.GetComponent<Collider>(), transform.root.GetComponent<Collider>(), false);
        objectHeld.SendMessage("OnRigidbodyRelease", SendMessageOptions.DontRequireReceiver);
        GetComponent<ScriptManager>().ScriptEnabledGlobal = true;
        if (fixedHold) { objectHeld.transform.parent = oldParent; }
        if (dragHideWeapon){itemSwitcher.FreeHands(false);}
        delay.isEnabled = true;
        pfunc.zoomEnabled = true;
        objectRaycast = null;
		objectHeld = null;
		isObjectHeld = false;
		GrabObject = false;
		isPressed = false;
    }
	
    private void ThrowObject()
    {
        if (fixedHold) { objectHeld.transform.parent = oldParent; }
        if (dragHideWeapon) { itemSwitcher.FreeHands(false); }
        gameManager.LockPlayerControls(true, true, false);
        gameManager.HideSprites (hideType.Grab);
        gameManager.UIPreventOverlap(false);
        gameManager.isGrabbed = false;
        interact.CrosshairVisible (true);
        objectHeld.GetComponent<Rigidbody>().useGravity = true;
        objectHeld.GetComponent<Rigidbody>().AddForce(playerCam.transform.forward * ThrowStrength, ForceMode.Impulse);
		objectHeld.GetComponent<Rigidbody>().freezeRotation = false;
		Physics.IgnoreCollision(objectHeld.GetComponent<Collider>(), transform.root.GetComponent<Collider>(), false);
        objectHeld.SendMessage("OnRigidbodyRelease", SendMessageOptions.DontRequireReceiver);
        GetComponent<ScriptManager>().ScriptEnabledGlobal = true;
        delay.isEnabled = true;
		objectRaycast = null;
		objectHeld = null;
		GrabObject = false;
		isPressed = false;
    }

	IEnumerator AntiSpam()
	{
		antiSpam = true;
		yield return new WaitForSeconds (spamWaitTime);
		antiSpam = false;
	}
}
