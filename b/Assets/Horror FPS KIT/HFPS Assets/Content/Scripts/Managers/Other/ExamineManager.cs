/*
 * ExamineManager.cs - wirted by ThunderWire Games
 * ver. 2.0
*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ExamineManager : MonoBehaviour {

    public class RigidbodyExamine
    {
        public class RigidbodyParams
        {
            public bool isKinematic;
            public bool useGravity;
        }

        public RigidbodyExamine(GameObject obj, RigidbodyParams rbp)
        {
            rbObject = obj;
            rbParameters = rbp;
        }

        public GameObject rbObject;
        public RigidbodyParams rbParameters;
    }

    private HFPS_GameManager gameManager;
    private InputController inputManager;
	private InteractManager interact;
	private PlayerFunctions pfunc;
    private FloatingIconManager floatingItem;
	private DelayEffect delay;

    private GameObject paperUI;
    private Text paperText;

    private List<RigidbodyExamine> ExamineRBs = new List<RigidbodyExamine>();

    public delegate void ActionDelegate(ExamineManager examine);
    public event ActionDelegate onDropObject;

    [HideInInspector]
    public bool isExamining;

    [Header("Raycast")]
	public LayerMask CullLayers;
	public string InteractLayer = "Interact";
    public float pickupSpeed = 10;
	public float rotationDeadzone = 0.1f;
	public float rotateSpeed = 10f;
    public float timeToExamine = 1f;
	public float spamWaitTime = 0.5f;

    private float pickupRange = 3f;

    [Header("Lighting")]
    public Light examineLight;

    [Header("Layering")]
    public LayerMask ExamineMainCamMask;
    public LayerMask ExamineArmsCamMask;

	private bool isPressedRead;
	private bool isReading;
    private bool isPaper;
    private bool antiSpam;
	private bool isPressed;
	private bool isObjectHeld;
	private bool tryExamine;
    private bool otherHeld;
    private bool isInspect;
    private bool cursorShown;
    private float distance;

    private Camera ArmsCam;
    private Camera PlayerCam;
    private Ray PlayerAim;

    private LayerMask DefaultMainCamMask;
    private LayerMask DefaultArmsCamMask;

    private GameObject objectRaycast;
    private GameObject objectHeld;

    private InteractiveItem firstExamine;
    private InteractiveItem secondExamine;
    private InteractiveItem priorityObject;

    private Transform oldSecondObjT;
    private Vector3 oldSecondObjPos;
    private Quaternion oldSecondRot;

    private Vector3 objectPosition;
    private Quaternion objectRotation;

    private KeyCode useKey;
	private KeyCode rotateKey;
	private KeyCode grabKey;

    private void Start()
    {
        if (GetComponent<InteractManager>() && GetComponent<PlayerFunctions>())
        {
            inputManager = InputController.Instance;
            gameManager = HFPS_GameManager.Instance;
            floatingItem = FloatingIconManager.Instance;
            interact = GetComponent<InteractManager>();
            pfunc = GetComponent<PlayerFunctions>();
            paperUI = gameManager.PaperTextUI;
            paperText = gameManager.PaperReadText;
        }
        else
        {
            Debug.LogError("Missing one or more scripts in " + gameObject.name);
            return;
        }

        if (examineLight)
        {
            examineLight.enabled = false;
        }

        delay = transform.root.gameObject.GetComponentInChildren<DelayEffect>();
        PlayerCam = ScriptManager.Instance.MainCamera;
        ArmsCam = ScriptManager.Instance.ArmsCamera;
        DefaultMainCamMask = ScriptManager.Instance.MainCamera.cullingMask;
        DefaultArmsCamMask = ArmsCam.cullingMask;
        pickupRange = interact.RaycastRange;
    }
	

	void Update ()
	{
		if(inputManager.HasInputs())
		{
            useKey = inputManager.GetInput("Use");
            grabKey = inputManager.GetInput("Pickup");
            rotateKey = inputManager.GetInput("Fire");
        }

		//Prevent Interact Dynamic Object when player is holding other object
		otherHeld = GetComponent<DragRigidbody> ().CheckHold ();

        if (gameManager.isPaused) return;

        if (objectRaycast && !antiSpam && firstExamine && firstExamine.examineType != InteractiveItem.ExamineType.None) {
			if (Input.GetKeyDown (grabKey) && !isPressed && !otherHeld) {
				isPressed = true;
                isExamining = !isExamining;
			} else if (isPressed) {			
				isPressed = false;
			}
		}

		if (isExamining){
			if (!isObjectHeld){
				FirstPhase();
				tryExamine = true;
			}else{
				HoldObject();
			}
		}else if(isObjectHeld){
            if (!secondExamine)
            {
                DropObject();
            }
            else
            {
                SecondExaminedObject(false);
                isExamining = true;
            }
		}

        PlayerAim = PlayerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (!isInspect)
        {
            if (Physics.Raycast(PlayerAim, out RaycastHit hit, pickupRange, CullLayers))
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer(InteractLayer))
                {
                    if (hit.collider.gameObject.GetComponent<InteractiveItem>())
                    {
                        objectRaycast = hit.collider.gameObject;
                        firstExamine = objectRaycast.GetComponent<InteractiveItem>();
                    }
                    else
                    {
                        if (!tryExamine)
                        {
                            objectRaycast = null;
                            firstExamine = null;
                        }
                    }
                }
                else
                {
                    if (!tryExamine)
                    {
                        objectRaycast = null;
                        firstExamine = null;
                    }
                }
            }
            else
            {
                if (!tryExamine)
                {
                    objectRaycast = null;
                    firstExamine = null;
                }
            }
        }

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
			
		if (priorityObject && isObjectHeld) {
			if (Input.GetKey (rotateKey) && !isReading && !cursorShown && priorityObject.examineRotate != InteractiveItem.ExamineRotate.None) {
                if (priorityObject.examineRotate == InteractiveItem.ExamineRotate.Both)
                {
                    priorityObject.transform.Rotate(PlayerCam.transform.up, rotationInputX, Space.World);
                    priorityObject.transform.Rotate(PlayerCam.transform.right, rotationInputY, Space.World);
                }
                else if (priorityObject.examineRotate == InteractiveItem.ExamineRotate.Horizontal)
                {
                    priorityObject.transform.Rotate(PlayerCam.transform.up, rotationInputX, Space.World);
                }
                else if (priorityObject.examineRotate == InteractiveItem.ExamineRotate.Vertical)
                {
                    priorityObject.transform.Rotate(PlayerCam.transform.right, rotationInputY, Space.World);
                }
            }

			if (isPaper) {
				if(Input.GetKeyDown(useKey) && !isPressedRead){
					isPressedRead = true;
					isReading = !isReading;
				}else if(isPressedRead){
					isPressedRead = false;
				}

				if (isReading) {
                    paperText.text = priorityObject.paperReadText;
                    paperText.fontSize = priorityObject.textSize;
					paperUI.SetActive (true);
				} else {
					paperUI.SetActive (false);
				}
            }
            else if(priorityObject.ItemType != InteractiveItem.Type.OnlyExamine && priorityObject.ItemType != InteractiveItem.Type.InteractObject && !isInspect)
            {
                if (Input.GetKeyDown(useKey) && !isPressed)
                {
                    isPressed = true;
                    TakeObject(secondExamine, priorityObject.gameObject);
                }
                else if (isPressed)
                {
                    isPressed = false;
                }
            }

            if (priorityObject.enableCursor)
            {
                if (Input.GetMouseButtonDown(1) && !isPressed)
                {
                    isPressed = true;
                    cursorShown = !cursorShown;
                    gameManager.ShowCursor(cursorShown);
                }
                else if (isPressed)
                {
                    isPressed = false;
                }
            }
            else
            {
                cursorShown = false;
                gameManager.ShowCursor(cursorShown);
            }

            if (cursorShown)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = PlayerCam.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out RaycastHit mouseHit, 5, CullLayers))
                    {
                        if (mouseHit.collider.GetComponent<InteractiveItem>() && mouseHit.collider.GetComponent<InteractiveItem>().examineCollect)
                        {
                            interact.Interact(mouseHit.collider.gameObject);
                        }
                        else if (mouseHit.collider.GetComponent<ExamineObjectAnimation>() && mouseHit.collider.GetComponent<ExamineObjectAnimation>().isEnabled)
                        {
                            mouseHit.collider.GetComponent<ExamineObjectAnimation>().PlayAnimation();
                        }
                        else if (mouseHit.collider.GetComponent<InteractiveItem>())
                        {
                            if (mouseHit.collider.GetComponent<InteractiveItem>() != firstExamine && !secondExamine)
                            {
                                secondExamine = mouseHit.collider.GetComponent<InteractiveItem>();
                                SecondExaminedObject(true);
                            }
                        }
                        else
                        {
                            mouseHit.collider.gameObject.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
                        }
                    }
                }
            }
        }
	}

    void SecondExaminedObject(bool isExamined)
    {
        if (secondExamine)
        {
            if (isExamined)
            {
                priorityObject = secondExamine;

                ShowExamineUI(true);

                secondExamine.floatingIconEnabled = false;

                oldSecondObjT = secondExamine.transform.parent;
                oldSecondObjPos = secondExamine.transform.position;
                oldSecondRot = secondExamine.transform.rotation;

                secondExamine.transform.parent = null;

                if (secondExamine.faceToCamera)
                {
                    Vector3 rotation = secondExamine.faceRotation;
                    secondExamine.transform.rotation = Quaternion.LookRotation(PlayerCam.transform.forward, PlayerCam.transform.up) * Quaternion.Euler(rotation);
                }

                foreach (MeshFilter mesh in objectHeld.GetComponentsInChildren<MeshFilter>())
                {
                    mesh.gameObject.layer = LayerMask.NameToLayer("Interact");
                }

                if (secondExamine.examineType == InteractiveItem.ExamineType.AdvancedObject)
                {
                    if (secondExamine.CollidersDisable.Length > 0)
                    {
                        foreach (var col in secondExamine.CollidersDisable)
                        {
                            col.enabled = false;
                        }
                    }

                    if (secondExamine.CollidersEnable.Length > 0)
                    {
                        foreach (var col in secondExamine.CollidersEnable)
                        {
                            col.enabled = true;
                        }
                    }
                }
            }
            else
            {
                priorityObject = firstExamine;

                ShowExamineUI(false);

                secondExamine.transform.SetParent(oldSecondObjT);
                secondExamine.transform.position = oldSecondObjPos;
                secondExamine.transform.rotation = oldSecondRot;

                secondExamine.floatingIconEnabled = true;

                if (secondExamine.examineType == InteractiveItem.ExamineType.AdvancedObject)
                {
                    if (secondExamine.CollidersDisable.Length > 0)
                    {
                        foreach (var col in secondExamine.CollidersDisable)
                        {
                            col.enabled = true;
                        }
                    }

                    if (secondExamine.CollidersEnable.Length > 0)
                    {
                        foreach (var col in secondExamine.CollidersEnable)
                        {
                            col.enabled = false;
                        }
                    }
                }

                secondExamine = null;

                foreach (MeshFilter mesh in objectHeld.GetComponentsInChildren<MeshFilter>())
                {
                    mesh.gameObject.layer = LayerMask.NameToLayer("Examine");
                }
            }
        }
        else
        {
            priorityObject = firstExamine;
            ShowExamineUI(false);
            foreach (MeshFilter mesh in objectHeld.GetComponentsInChildren<MeshFilter>())
            {
                mesh.gameObject.layer = LayerMask.NameToLayer("Examine");
            }
        }
    }

    void ShowExamineUI(bool SecondItem = false)
    {
        if (!SecondItem)
        {
            if (priorityObject.ItemType != InteractiveItem.Type.OnlyExamine)
            {
                if (!isInspect)
                {
                    if (priorityObject.ItemType == InteractiveItem.Type.InteractObject)
                    {
                        gameManager.ShowExamineSprites(btn2: false, btn3: priorityObject.examineRotate != InteractiveItem.ExamineRotate.None, btn4: priorityObject.enableCursor);
                    }
                    else
                    {
                        gameManager.ShowExamineSprites(btn3: priorityObject.examineRotate != InteractiveItem.ExamineRotate.None, btn4: priorityObject.enableCursor);
                    }
                }
                else
                {
                    gameManager.ShowExamineSprites(PutDownText: "Put Away", btn2: false, btn3: priorityObject.examineRotate != InteractiveItem.ExamineRotate.None, btn4: priorityObject.enableCursor);
                }
            }
            else
            {
                gameManager.ShowExamineSprites(btn2: false, btn3: priorityObject.examineRotate != InteractiveItem.ExamineRotate.None, btn4: priorityObject.enableCursor);
            }
        }
        else
        {
            InteractiveItem secondItem = secondExamine.GetComponent<InteractiveItem>();
            gameManager.ShowExamineSprites(PutDownText: "Put Away", btn2: secondItem.ItemType != InteractiveItem.Type.OnlyExamine, btn3: priorityObject.examineRotate != InteractiveItem.ExamineRotate.None, btn4: priorityObject.enableCursor);
        }
    }

    void FirstPhase()
    {
        StartCoroutine(AntiSpam());

        distance = 0;

        priorityObject = firstExamine;
        objectHeld = objectRaycast.gameObject;
        objectPosition = objectHeld.transform.position;
        objectRotation = objectHeld.transform.rotation;

        isPaper = firstExamine.examineType == InteractiveItem.ExamineType.Paper;

        if (examineLight)
        {
            examineLight.enabled = true;
        }

        if (isInspect) { firstExamine.isExamined = true; }

        if (!isPaper)
        {
            if (!string.IsNullOrEmpty(firstExamine.ItemName))
            {
                if (!firstExamine.isExamined)
                {
                    ShowExamineText(firstExamine.ItemName);
                }
                else
                {
                    gameManager.isExamining = true;
                    gameManager.ShowExamineText(firstExamine.ItemName);
                }
            }

            ShowExamineUI();
        }
        else
        {
            gameManager.ShowExamineSprites(useKey, "Read");
        }

        if (firstExamine.ExamineSound)
        {
            AudioSource.PlayClipAtPoint(firstExamine.ExamineSound, objectHeld.transform.position, 0.75f);
        }

        foreach (MeshFilter mesh in objectHeld.GetComponentsInChildren<MeshFilter>())
        {
            mesh.gameObject.layer = LayerMask.NameToLayer("Examine");
        }

        foreach (MeshRenderer renderer in objectHeld.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        foreach (Collider col in objectHeld.GetComponentsInChildren<Collider>())
        {
            if (col.GetType() != typeof(MeshCollider))
            {
                col.isTrigger = true;
            }
        }

        if (firstExamine.examineType == InteractiveItem.ExamineType.AdvancedObject)
        {
            if (firstExamine.CollidersDisable.Length > 0)
            {
                foreach (var col in objectHeld.GetComponent<InteractiveItem>().CollidersDisable)
                {
                    col.enabled = false;
                }
            }

            if (firstExamine.CollidersEnable.Length > 0)
            {
                foreach (var col in objectHeld.GetComponent<InteractiveItem>().CollidersEnable)
                {
                    col.enabled = true;
                }
            }
        }

        foreach (Rigidbody rb in objectHeld.GetComponentsInChildren<Rigidbody>())
        {
            ExamineRBs.Add(new RigidbodyExamine(rb.gameObject, new RigidbodyExamine.RigidbodyParams() { isKinematic = rb.isKinematic, useGravity = rb.useGravity}));
        }

        foreach (var col in objectHeld.GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(col, PlayerCam.transform.root.gameObject.GetComponent<CharacterController>(), true);
        }

        PlayerCam.cullingMask = ExamineMainCamMask;
        ArmsCam.cullingMask = ExamineArmsCamMask;

        if (firstExamine.faceToCamera)
        {
            Vector3 rotation = objectHeld.GetComponent<InteractiveItem>().faceRotation;
            objectHeld.transform.rotation = Quaternion.LookRotation(PlayerCam.transform.forward, PlayerCam.transform.up) * Quaternion.Euler(rotation);
        }

        SetFloatingIconsVisible(false);

        delay.isEnabled = false;
        pfunc.enabled = false;
        gameManager.UIPreventOverlap(true);
        gameManager.HideSprites(hideType.Interact);
        gameManager.LockPlayerControls(false, false, false, 1, true, true, 1);
        GetComponent<ScriptManager>().ScriptEnabledGlobal = false;
        distance = firstExamine.ExamineDistance;

        isObjectHeld = true;
    }

    /*
    IEnumerator SmoothRotateObject()
    {
        while ()
        {
            Vector3 rotation = objectHeld.GetComponent<InteractiveItem>().faceRotation;
            objectHeld.transform.rotation = Quaternion.RotateTowards(PlayerCam.transform.forward, PlayerCam.transform.up, Time.deltaTime * 2f) * Quaternion.Euler(rotation);
        }

        yield return null;
    }
    */

    void SetFloatingIconsVisible(bool visible)
    {
        GameObject SecondItem = null;

        if (!firstExamine) return;

        if (firstExamine.examineType == InteractiveItem.ExamineType.AdvancedObject)
        {
            if (objectHeld.GetComponentsInChildren<Transform>().Count(obj => floatingItem.ContainsFloatingIcon(obj.gameObject)) > 0)
            {
                foreach (var item in objectHeld.GetComponentsInChildren<Transform>().Where(obj => floatingItem.ContainsFloatingIcon(obj.gameObject)).ToArray())
                {
                    if (item.GetComponent<InteractiveItem>())
                    {
                        item.GetComponent<InteractiveItem>().floatingIconEnabled = !visible;
                        SecondItem = item.gameObject;
                    }
                }
            }
        }

        foreach (var item in floatingItem.FloatingIconCache)
        {
            if(item.FollowObject != SecondItem)
            {
                if (item.FollowObject.GetComponent<InteractiveItem>())
                {
                    item.FollowObject.GetComponent<InteractiveItem>().floatingIconEnabled = visible;
                }
            }
        }
    }

    void HoldObject()
    {
        interact.CrosshairVisible(false);

        Vector3 nextPos = PlayerCam.transform.position + PlayerAim.direction * distance;
        Vector3 currPos = objectHeld.transform.position;

        if (secondExamine)
        {
            Vector3 second_nextPos = PlayerCam.transform.position + PlayerAim.direction * secondExamine.GetComponent<InteractiveItem>().ExamineDistance;
            secondExamine.transform.position = Vector3.Lerp(secondExamine.transform.position, second_nextPos, Time.deltaTime * pickupSpeed);
        }

        if (objectHeld.GetComponent<Rigidbody>())
        {
            objectHeld.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
            objectHeld.GetComponent<Rigidbody>().isKinematic = true;
            objectHeld.GetComponent<Rigidbody>().useGravity = false;
        }

        objectHeld.transform.position = Vector3.Lerp(objectHeld.transform.position, nextPos, Time.deltaTime * pickupSpeed);
    }

    public void ExamineObject(GameObject @object)
    {
        InteractiveItem item = @object.GetComponent<InteractiveItem>();
        firstExamine = item;
        priorityObject = item;

        float inspectDist = item.ExamineDistance;
        Vector3 nextPos = PlayerCam.transform.position + PlayerAim.direction * inspectDist;

        @object.name = "Inspect: " + item.ItemName;
        if (@object.GetComponent<Rigidbody>())
        {
            @object.GetComponent<Rigidbody>().isKinematic = false;
            @object.GetComponent<Rigidbody>().useGravity = false;
        }
        @object.transform.position = nextPos;

        if (@object.GetComponent<SaveObject>())
        {
            Destroy(@object.GetComponent<SaveObject>());
        }

        foreach (var col in @object.GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(col, PlayerCam.transform.root.gameObject.GetComponent<CharacterController>(), true);
        }

        objectRaycast = @object;
        isExamining = true; //Examine Object
        isInspect = true;
    }

    public void CancelExamine()
    {
        isExamining = false;
        isPressed = false;
    }

    void DropObject()
	{
        SetFloatingIconsVisible(true);
        SecondExaminedObject(false);
        distance = 0;

        if (examineLight)
        {
            examineLight.enabled = false;
        }

        foreach (MeshFilter mesh in objectHeld.GetComponentsInChildren<MeshFilter>())
        {
            mesh.gameObject.layer = LayerMask.NameToLayer("Interact");
        }

        foreach (MeshRenderer renderer in objectHeld.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        foreach (Collider col in objectHeld.GetComponentsInChildren<Collider>())
        {
            if (col.GetType() != typeof(MeshCollider))
            {
                col.isTrigger = false;
            }
        }

        if (firstExamine.examineType == InteractiveItem.ExamineType.AdvancedObject)
        {
            if (firstExamine.CollidersDisable.Length > 0)
            {
                foreach (var col in objectHeld.GetComponent<InteractiveItem>().CollidersDisable)
                {
                    col.enabled = true;
                }
            }

            if (firstExamine.CollidersEnable.Length > 0)
            {
                foreach (var col in objectHeld.GetComponent<InteractiveItem>().CollidersEnable)
                {
                    col.enabled = false;
                }
            }
        }

        if (!isInspect)
        {
            objectHeld.transform.position = objectPosition;
            objectHeld.transform.rotation = objectRotation;
        }
        else
        {
            Destroy(objectHeld);
            isInspect = false;
        }

        foreach (var item in ExamineRBs)
        {
            item.rbObject.GetComponent<Rigidbody>().isKinematic = item.rbParameters.isKinematic;
            item.rbObject.GetComponent<Rigidbody>().useGravity = item.rbParameters.useGravity;
        }

        if (!isPaper)
        {
            if (objectHeld.GetComponent<Collider>().GetType() != typeof(MeshCollider))
            {
                objectHeld.GetComponent<Collider>().isTrigger = false;
            }
        }

        StopAllCoroutines();
        GetComponent<ScriptManager>().ScriptEnabledGlobal = true;
        gameManager.UIPreventOverlap(false);
        gameManager.HideExamine();
        gameManager.HideSprites(hideType.Examine);
        floatingItem.SetAllIconsVisible(true);
        delay.isEnabled = true;
        PlayerCam.cullingMask = DefaultMainCamMask;
        ArmsCam.cullingMask = DefaultArmsCamMask;

        paperUI.SetActive(false);
        pfunc.enabled = true;
        isObjectHeld = false;
        isExamining = false;
        isReading = false;
        tryExamine = false;

        firstExamine = null;
        priorityObject = null;
        objectRaycast = null;
        objectHeld = null;
        ExamineRBs.Clear();

        onDropObject?.Invoke(this);

        StartCoroutine(AntiSpam());
        StartCoroutine(UnlockPlayer());
    }

    void TakeObject(bool takeSecond, GameObject take)
    {
        SecondExaminedObject(false);

        if (!takeSecond)
        {
            StopAllCoroutines();
            GetComponent<ScriptManager>().ScriptEnabledGlobal = true;
            gameManager.UIPreventOverlap(false);
            gameManager.HideExamine();
            gameManager.HideSprites(hideType.Examine);
            floatingItem.SetAllIconsVisible(true);
            delay.isEnabled = true;
            PlayerCam.cullingMask = DefaultMainCamMask;
            ArmsCam.cullingMask = DefaultArmsCamMask;
            paperUI.SetActive(false);
            pfunc.enabled = true;
            firstExamine = null;
            isObjectHeld = false;
            isExamining = false;
            isReading = false;
            tryExamine = false;
            objectRaycast = null;
            objectHeld = null;
            ExamineRBs.Clear();
            StartCoroutine(AntiSpam());
            StartCoroutine(UnlockPlayer());
        }

        interact.Interact(take);
    }

    void ShowExamineText(string ExamineName)
    {
        gameManager.isExamining = true;
        StopCoroutine(DoExamine());
        StartCoroutine(DoExamine(ExamineName));
    }

    IEnumerator DoExamine(string ExamineName = "")
    {
        InteractiveItem[] examineItems = FindObjectsOfType<InteractiveItem>().Where(i => i.ItemName == ExamineName).ToArray();
        yield return new WaitForSeconds(timeToExamine);

        gameManager.ShowExamineText(ExamineName);

        foreach (var inst in examineItems)
        {
            inst.isExamined = true;
        }
    }

    IEnumerator UnlockPlayer()
    {
        yield return new WaitForFixedUpdate();
        gameManager.LockPlayerControls(true, true, false, 1, false, false, 2);
        interact.CrosshairVisible(true);
    }

	IEnumerator AntiSpam()
	{
		antiSpam = true;
		yield return new WaitForSeconds (spamWaitTime);
		antiSpam = false;
	}
}