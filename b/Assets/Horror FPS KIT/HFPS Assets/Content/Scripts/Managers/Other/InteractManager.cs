/*
 * InteractManager.cs - wirted by ThunderWire Games
 * ver. 2.0
*/

using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ThunderWire.Utility;

public class InteractManager : MonoBehaviour {

    private InputController inputManager;
    private HFPS_GameManager gameManager;
	private ItemSwitcher itemSelector;
	private Inventory inventory;

    private Camera mainCamera;
    private DynamicObject dynamicObj;
    private InteractiveItem interactItem;
    private UIObjectInfo objectInfo;

    [Header("Raycast")]
	public float RaycastRange = 3;
	public LayerMask cullLayers;
	public string InteractLayer;
	
	[Header("Crosshair Textures")]
	public Sprite defaultCrosshair;
	public Sprite interactCrosshair;
	private Sprite default_interactCrosshair;
	
	[Header("Crosshair")]
	private Image CrosshairUI;
	public int crosshairSize = 5;
	public int interactSize = 10;

    [Header("Texts")]
    [Tooltip("Make sure you have included \"{0}\" to string")]
    public string PickupHintFormat = "You have taken a {0}";
    public string TakeText = "Take";
    public string UseText = "Use";
    public string UnlockText = "Unlock";
    public string GrabText = "Grab";
    public string DragText = "Drag";
    public string ExamineText = "Examine";
    public string RemoveText = "Remove";

    private int default_interactSize;
    private int default_crosshairSize;

    [HideInInspector] public bool isHeld = false;

    [HideInInspector] public bool inUse;

    [HideInInspector] public Ray playerAim;

	[HideInInspector] public GameObject RaycastObject;
	private GameObject LastRaycastObject;

    private KeyCode UseKey;
    private KeyCode PickupKey;

    private string RaycastTag;

	private bool isPressed;
    private bool correctLayer;

    void Start()
    {
        inputManager = InputController.Instance;
        gameManager = HFPS_GameManager.Instance;
        itemSelector = ScriptManager.Instance.GetScript<ItemSwitcher>();
        CrosshairUI = gameManager.Crosshair;
        default_interactCrosshair = interactCrosshair;
        default_crosshairSize = crosshairSize;
        default_interactSize = interactSize;
        mainCamera = ScriptManager.Instance.MainCamera;
        RaycastObject = null;
        dynamicObj = null;
    }
	
	void Update () {
		inventory = GetComponent<ScriptManager>().GetScript<Inventory>();

		if(inputManager.HasInputs())
		{
            UseKey = inputManager.GetInput("Use");
            PickupKey = inputManager.GetInput("Pickup");
        }

		if(Input.GetKey(UseKey) && RaycastObject && !isPressed && !isHeld && !inUse){
			Interact(RaycastObject);
			isPressed = true;
		}

		if(Input.GetKeyUp(UseKey) && isPressed){
			isPressed = false;
		}
			
        Ray playerAim = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(playerAim, out RaycastHit hit, RaycastRange, cullLayers))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer(InteractLayer))
            {
                if (hit.collider.gameObject != RaycastObject)
                {
                    gameManager.HideSprites(hideType.Interact);
                }

                RaycastObject = hit.collider.gameObject;
                RaycastTag = hit.collider.tag;
                correctLayer = true;

                if (RaycastObject.GetComponent<InteractiveItem>())
                {
                    interactItem = RaycastObject.GetComponent<InteractiveItem>();
                }
                else
                {
                    interactItem = null;
                }

                if (RaycastObject.GetComponent<DynamicObject>())
                {
                    dynamicObj = RaycastObject.GetComponent<DynamicObject>();
                }
                else
                {
                    dynamicObj = null;
                }

                if (RaycastObject.GetComponent<UIObjectInfo>())
                {
                    objectInfo = RaycastObject.GetComponent<UIObjectInfo>();
                }
                else
                {
                    objectInfo = null;
                }

                if (RaycastObject.GetComponent<CrosshairReticle>())
                {
                    CrosshairReticle ChangeReticle = RaycastObject.GetComponent<CrosshairReticle>();
                    if (dynamicObj)
                    {
                        if (dynamicObj.useType != Type_Use.Locked)
                        {
                            interactCrosshair = ChangeReticle.interactSprite;
                            interactSize = ChangeReticle.size;
                        }
                    }
                    else
                    {
                        interactCrosshair = ChangeReticle.interactSprite;
                        interactSize = ChangeReticle.size;
                    }
                }

                if (LastRaycastObject)
                {
                    if (!(LastRaycastObject == RaycastObject))
                    {
                        ResetCrosshair();
                    }
                }
                LastRaycastObject = RaycastObject;

                if (objectInfo && !string.IsNullOrEmpty(objectInfo.objectTitle))
                {
                    gameManager.ShowInteractInfo(objectInfo.objectTitle);
                }

                if (!inUse)
                {
                    if (dynamicObj)
                    {
                        if (dynamicObj.useType == Type_Use.Locked)
                        {
                            if (dynamicObj.CheckHasKey())
                            {
                                gameManager.ShowInteractSprite(1, UnlockText, UseKey);
                            }
                            else
                            {
                                if (dynamicObj.interactType == Type_Interact.Mouse)
                                {
                                    gameManager.ShowInteractSprite(1, DragText, UseKey);
                                }
                                else
                                {
                                    gameManager.ShowInteractSprite(1, UseText, UseKey);
                                }
                            }
                        }
                        else
                        {
                            if (dynamicObj.interactType == Type_Interact.Mouse)
                            {
                                gameManager.ShowInteractSprite(1, DragText, UseKey);
                            }
                            else
                            {
                                gameManager.ShowInteractSprite(1, UseText, UseKey);
                            }
                        }
                    }
                    else
                    {
                        if (interactItem)
                        {
                            if(interactItem.showItemName && !string.IsNullOrEmpty(interactItem.ItemName))
                            {
                                gameManager.ShowInteractInfo(interactItem.ItemName);
                            }

                            if (RaycastTag != "GrabUse")
                            {
                                if (interactItem.ItemType == InteractiveItem.Type.OnlyExamine)
                                {
                                    gameManager.ShowInteractSprite(1, ExamineText, PickupKey);
                                }
                                else if (interactItem.ItemType == InteractiveItem.Type.InteractObject)
                                {
                                    if (interactItem.examineType != InteractiveItem.ExamineType.None)
                                    {
                                        gameManager.ShowInteractSprite(1, UseText, UseKey);
                                        gameManager.ShowInteractSprite(2, ExamineText, PickupKey);
                                    }
                                    else
                                    {
                                        gameManager.ShowInteractSprite(1, UseText, UseKey);
                                    }
                                }
                                else if (interactItem.examineType != InteractiveItem.ExamineType.None && interactItem.ItemType != InteractiveItem.Type.InteractObject)
                                {
                                    gameManager.ShowInteractSprite(1, TakeText, UseKey);
                                    gameManager.ShowInteractSprite(2, ExamineText, PickupKey);
                                }
                                else if (interactItem.examineType == InteractiveItem.ExamineType.Paper)
                                {
                                    gameManager.ShowInteractSprite(1, ExamineText, PickupKey);
                                }
                                else
                                {
                                    gameManager.ShowInteractSprite(1, TakeText, UseKey);
                                }
                            }
                            else
                            {
                                if (interactItem.ItemType != InteractiveItem.Type.OnlyExamine)
                                {
                                    gameManager.ShowInteractSprite(1, TakeText, UseKey);
                                    gameManager.ShowInteractSprite(2, GrabText, PickupKey);
                                }
                            }
                        }
                        else if (RaycastObject.GetComponent<DynamicObjectPlank>())
                        {
                            gameManager.ShowInteractSprite(1, RemoveText, UseKey);
                        }
                        else if (RaycastTag == "Grab")
                        {
                            gameManager.ShowInteractSprite(1, GrabText, PickupKey);
                        }
                        else if(objectInfo)
                        {
                            gameManager.ShowInteractSprite(1, objectInfo.useText, UseKey);
                        }
                        else
                        {
                            gameManager.ShowInteractSprite(1, UseText, UseKey);
                        }
                    }
                }

                CrosshairChange(true);
            }
            else if (RaycastObject)
            {
                correctLayer = false;
            }
        }
        else if (RaycastObject)
        {
            correctLayer = false;
        }

		if(!correctLayer){
			ResetCrosshair ();
            CrosshairChange(false);
            gameManager.HideSprites(hideType.Interact);
            interactItem = null;
            RaycastObject = null;
            dynamicObj = null;
		}
		
		if(!RaycastObject)
		{
            gameManager.HideSprites(hideType.Interact);
            CrosshairChange(false);
            dynamicObj = null;
		}
    }

    void CrosshairChange(bool useTexture)
    {
        if(useTexture && CrosshairUI.sprite != interactCrosshair)
        {
            CrosshairUI.sprite = interactCrosshair;
            CrosshairUI.GetComponent<RectTransform>().sizeDelta = new Vector2(interactSize, interactSize);
        }
        else if(!useTexture && CrosshairUI.sprite != defaultCrosshair)
        {
            CrosshairUI.sprite = defaultCrosshair;
            CrosshairUI.GetComponent<RectTransform>().sizeDelta = new Vector2(crosshairSize, crosshairSize);
        }

        CrosshairUI.DisableSpriteOptimizations();
    }

	private void ResetCrosshair(){
		crosshairSize = default_crosshairSize;
		interactSize = default_interactSize;
		interactCrosshair = default_interactCrosshair;
	}

	public void CrosshairVisible(bool state)
	{
		switch (state) 
		{
		case true:
			CrosshairUI.enabled = true;
			break;
		case false:
			CrosshairUI.enabled = false;
			break;
		}
	}

	public bool GetInteractBool()
	{
		if (RaycastObject) {
			return true;
		} else {
			return false;
		}
	}

    public void Interact(GameObject InteractObject)
    {
        InteractiveItem interactiveItem = interactItem;

        if(!interactItem && !interactiveItem && InteractObject.GetComponent<InteractiveItem>())
        {
            interactiveItem = InteractObject.GetComponent<InteractiveItem>();
        }

        if (interactiveItem && interactiveItem.ItemType == InteractiveItem.Type.OnlyExamine) return;

        if (InteractObject.GetComponent<Message>())
        {
            Message message = InteractObject.GetComponent<Message>();

            if (message.messageType == Message.MessageType.Hint)
            {
                char[] messageChars = message.message.ToCharArray();

                if (messageChars.Contains('{') && messageChars.Contains('}'))
                {
                    string key = inputManager.GetInput(message.message.GetBetween('{', '}')).ToString();
                    message.message = message.message.ReplacePart('{', '}', key);
                }

                gameManager.ShowHint(message.message, message.messageTime);
            }
            else if(message.messageType == Message.MessageType.PickupHint)
            {
                gameManager.ShowHint(string.Format(PickupHintFormat, message.message), message.messageTime);
            }
            else if (message.messageType == Message.MessageType.Message)
            {
                gameManager.AddMessage(message.message);
            }
            else if(message.messageType == Message.MessageType.ItemName)
            {
                gameManager.AddPickupMessage(message.message);
            }
        }

        if (interactiveItem)
        {
            Item item = new Item();
            bool showMessage = true;

            if (interactiveItem.ItemType == InteractiveItem.Type.InventoryItem)
            {
                item = inventory.GetItem(interactiveItem.InventoryID);
            }

            if (interactiveItem.markLightObject)
            {
                itemSelector.currentLightObject = item.useSwitcherID;
            }

            if (interactiveItem.ItemType == InteractiveItem.Type.GenericItem)
            {
                InteractEvent(InteractObject);
            }
            else if (interactiveItem.ItemType == InteractiveItem.Type.BackpackExpand)
            {
                inventory.ExpandSlots(interactiveItem.BackpackExpand);
                InteractEvent(InteractObject);
            }
            else if (interactiveItem.ItemType == InteractiveItem.Type.InventoryItem)
            {
                if (inventory.CheckInventorySpace() || inventory.CheckItemInventoryStack(interactiveItem.InventoryID))
                {
                    if (inventory.GetItemAmount(item.ID) < item.maxItemCount || item.maxItemCount == 0)
                    {
                        inventory.AddItem(interactiveItem.InventoryID, interactiveItem.Amount, interactiveItem.customData);
                        InteractEvent(InteractObject);
                    }
                    else if (inventory.GetItemAmount(item.ID) >= item.maxItemCount)
                    {
                        gameManager.AddSingleMessage("You cannot carry more " + item.Title, "MaxItemCount");
                        showMessage = false;
                    }
                }
                else
                {
                    gameManager.AddSingleMessage("No Inventory Space!", "NoSpace");
                    showMessage = false;
                }
            }
            else if (interactiveItem.ItemType == InteractiveItem.Type.ArmsItem)
            {
                if (inventory.CheckInventorySpace() || inventory.CheckItemInventoryStack(interactiveItem.InventoryID))
                {
                    if (inventory.GetItemAmount(item.ID) < item.maxItemCount || item.maxItemCount == 0)
                    {
                        inventory.AddItem(interactiveItem.InventoryID, interactiveItem.Amount);

                        if (interactiveItem.pickupSwitch)
                        {
                            itemSelector.SelectItem(interactiveItem.WeaponID);
                        }

                        if (item.itemType == ItemType.Weapon)
                        {
                            itemSelector.weaponItem = interactiveItem.WeaponID;
                        }

                        InteractEvent(InteractObject);
                    }
                    else if (inventory.GetItemAmount(item.ID) >= item.maxItemCount)
                    {
                        gameManager.AddSingleMessage("You cannot carry more " + item.Title, "MaxItemCount");
                        showMessage = false;
                    }
                }
                else
                {
                    gameManager.AddSingleMessage("No Inventory Space!", "NoSpace");
                    showMessage = false;
                }
            }
            else if(interactItem.ItemType == InteractiveItem.Type.InteractObject)
            {
                InteractEvent(InteractObject);
            }

            if (showMessage)
            {
                if (interactiveItem.messageType == InteractiveItem.MessageType.Hint)
                {
                    char[] messageChars = interactiveItem.Message.ToCharArray();

                    if (messageChars.Contains('{') && messageChars.Contains('}'))
                    {
                        string key = inputManager.GetInput(Tools.GetBetween(interactiveItem.Message, '{', '}')).ToString();
                        interactiveItem.Message = interactiveItem.Message.ReplacePart('{', '}', key);
                    }

                    gameManager.ShowHint(interactiveItem.Message, interactiveItem.MessageTime, interactiveItem.MessageTips);
                }
                if (interactiveItem.messageType == InteractiveItem.MessageType.PickupHint)
                {
                    gameManager.ShowHint(string.Format(PickupHintFormat, interactiveItem.Message), interactiveItem.MessageTime, interactiveItem.MessageTips);
                }
                if (interactiveItem.messageType == InteractiveItem.MessageType.Message)
                {
                    gameManager.AddMessage(interactiveItem.Message);
                }
                if (interactiveItem.messageType == InteractiveItem.MessageType.ItemName)
                {
                    gameManager.AddPickupMessage(interactiveItem.Message);
                }
            }
        }
        else
        {
            InteractEvent(InteractObject);
        }

        interactiveItem = null;
    }

	void InteractEvent(GameObject InteractObject)
	{
        gameManager.HideSprites (hideType.Interact);
        InteractObject.SendMessage ("UseObject", SendMessageOptions.DontRequireReceiver);
	}
}
