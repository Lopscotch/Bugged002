/* ItemSwitcher.cs by ThunderWire Games - Script for Switching Items */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ThunderWire.Utility;

public class ItemSwitcher : MonoBehaviour {

    private Inventory inventory;
    private HFPS_GameManager gameManager;
    private Camera mainCamera;

	public List<GameObject> ItemList = new List<GameObject>();
    public int currentItem = -1;

    [Header("Wall Detecting")]
    public bool detectWall;
    public LayerMask HitMask;
    public float wallHitRange;

    public Animation WallDetectAnim;
    public string HideAnim;
    public string ShowAnim;

    [Header("Item On Start")]
    public bool startWithCurrentItem;
    public bool startWithoutAnimation;

    [Tooltip("The ItemID in Inventory Database. Keep -1 if it's not an Inventory Item.")]
    public int startingItemID = -1; 

    [Header("Misc")]
    [Tooltip("ID must be always light object which you currently using!")]
    public int currentLightObject = 0;

    [HideInInspector]
    public int weaponItem = -1;

    private bool hit;
    private bool handsFree;
    private bool switchItem;
    private bool isPressed;

	private int newItem = 0;
    private bool antiSpam;
    private bool spam;

    void Awake()
    {
        mainCamera = ScriptManager.Instance.MainCamera;
    }

    void Start()
    {
        inventory = transform.root.gameObject.GetComponentInChildren<ScriptManager>().GetScript<Inventory>();
        gameManager = transform.root.gameObject.GetComponentInChildren<ScriptManager>().GetScript<HFPS_GameManager>();

        if (startWithCurrentItem)
        {
            if (startingItemID > -1)
            {
                inventory.AddItem(startingItemID, 1);
            }

            if (!startWithoutAnimation)
            {
                SelectItem(currentItem);
            }
            else
            {
                ActivateItem(currentItem);
            }
        }
    }

    public void SelectItem(int id)
    {
        //if (IsBusy()) return;

        if (id != currentItem)
        {
            newItem = id;

            if (!CheckActiveItem())
            {
                SelectItem();
            }
            else
            {
                StartCoroutine(SwitchItem());
            }
        }
        else
        {
            DeselectItems();
        }
    }

    public void DeselectItems()
	{
        if (currentItem == -1) return;
        ItemList [currentItem].GetComponent<ISwitcher>().Deselect();
    }

    public void DisableItems()
    {
        if (currentItem == -1) return;
        ItemList[currentItem].GetComponent<ISwitcher>().Disable();
    }

    public int GetIDByObject(GameObject switcherObject)
    {
        return ItemList.IndexOf(switcherObject);
    }

    public GameObject GetCurrentItem()
    {
        if(currentItem != -1)
        {
            return ItemList[currentItem];
        }

        return null;
    }

    public bool IsBusy()
    {
        return transform.root.gameObject.GetComponentInChildren<ExamineManager>().isExamining || transform.root.gameObject.GetComponentInChildren<DragRigidbody>().CheckHold();
    }

    /// <summary>
    /// Check if all Items are Deactivated
    /// </summary>
	bool CheckActiveItem()
	{
		for (int i = 0; i < ItemList.Count; i++) {
            bool ACState = ItemList[i].transform.GetChild(0).gameObject.activeSelf;
			if (ACState)
				return true;
		}
		return false;
	}

	IEnumerator SwitchItem()
	{
        switchItem = true;
        ItemList [currentItem].GetComponent<ISwitcher>().Deselect();

        yield return new WaitUntil (() => ItemList[currentItem].transform.GetChild(0).gameObject.activeSelf == false);

        ItemList[newItem].GetComponent<ISwitcher>().Select();
		currentItem = newItem;
        switchItem = false;
    }

	void SelectItem()
	{
        switchItem = true;
        ItemList [newItem].GetComponent<ISwitcher>().Select();
        currentItem = newItem;
        switchItem = false;
    }

    void Update()
    {
        if (!gameManager.scriptManager.ScriptGlobalState) return;

        if (WallDetectAnim && detectWall && !handsFree && currentItem != -1)
        {
            if (WallHit())
            {
                if (!hit)
                {
                    WallDetectAnim.Play(HideAnim);
                    if (ItemList[currentItem].GetComponent<ISwitcherWallHit>() != null)
                    {
                        ItemList[currentItem].GetComponent<ISwitcherWallHit>().OnWallHit(true);
                    }
                    hit = true;
                }
            }
            else
            {
                if (hit)
                {
                    WallDetectAnim.Play(ShowAnim);
                    if (ItemList[currentItem].GetComponent<ISwitcherWallHit>() != null)
                    {
                        ItemList[currentItem].GetComponent<ISwitcherWallHit>().OnWallHit(false);
                    }
                    hit = false;
                }
            }
        }

        if (!gameManager.isGrabbed)
        {
            if (!antiSpam)
            {
                //Mouse ScrollWheel Backward - Deselect Current Item
                if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                {
                    if (currentItem != -1)
                    {
                        DeselectItems();
                    }
                }

                //Mouse ScrollWheel Forward - Select Last Weapon Item
                if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                {
                    if (weaponItem != -1)
                    {
                        MouseWHSelectWeapon();
                    }
                }
            }
            else
            {
                if (!spam)
                {
                    StartCoroutine(AntiSwitchSpam());
                    spam = true;
                }
            }
        }
        else
        {
            antiSpam = true;
        }

        if (currentItem != -1)
        {
            ItemList[currentItem].GetComponent<ISwitcher>().OnItemBlock(gameManager.isGrabbed);
        }
    }

    void MouseWHSelectWeapon()
    {
        if (currentItem != weaponItem)
        {
            if (ItemList[weaponItem].GetComponent<WeaponController>() && inventory.CheckSWIDInventory(weaponItem))
            {
                SelectItem(weaponItem);
            }
        }
    }

    IEnumerator AntiSwitchSpam()
    {
        antiSpam = true;
        yield return new WaitForSeconds(1f);
        antiSpam = false;
        spam = false;
    }

    void FixedUpdate()
    {
        if (!CheckActiveItem() && !switchItem)
        {
            currentItem = -1;
        }

        if (!inventory.CheckSWIDInventory(weaponItem))
        {
            weaponItem = -1;
        }
    }

    bool GetItemsActive()
    {
        bool response = true;
        for (int i = 0; i < ItemList.Count; i++)
        {
            if (ItemList[i].transform.GetChild(0).gameObject.activeSelf)
            {
                response = false;
                break;
            }
        }
        return response;
    }

    /// <summary>
    /// Activate Item without playing animation.
    /// </summary>
    public void ActivateItem(int switchID)
    {
        switchItem = true;
        ItemList[switchID].GetComponent<ISwitcher>().EnableItem();
        currentItem = switchID;
        newItem = switchID;
        switchItem = false;
    }

    public void FreeHands(bool free)
    {
        if (currentItem != -1)
        {
            if (free && !handsFree)
            {
                WallDetectAnim.wrapMode = WrapMode.Once;
                WallDetectAnim.Play(HideAnim);
                if (ItemList[currentItem].GetComponent<ISwitcherWallHit>() != null)
                {
                    ItemList[currentItem].GetComponent<ISwitcherWallHit>().OnWallHit(true);
                }
                handsFree = true;
            }
            else if (!free && handsFree)
            {
                WallDetectAnim.wrapMode = WrapMode.Once;
                WallDetectAnim.Play(ShowAnim);
                if (ItemList[currentItem].GetComponent<ISwitcherWallHit>() != null)
                {
                    ItemList[currentItem].GetComponent<ISwitcherWallHit>().OnWallHit(false);
                }
                handsFree = false;
            }
        }
    }

    bool WallHit()
    {
        if(Physics.Raycast(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward), out RaycastHit hit, wallHitRange, HitMask))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (detectWall)
        {
            Camera cam = Tools.MainCamera();
            Gizmos.color = Color.red;
            Gizmos.DrawRay(cam.transform.position, cam.transform.TransformDirection(Vector3.forward * wallHitRange));
        }
    }
}