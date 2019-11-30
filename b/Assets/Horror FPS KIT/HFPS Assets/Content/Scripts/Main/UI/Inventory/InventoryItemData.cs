/*
 * InventoryItemData.cs - script by ThunderWire Games
 * ver. 1.21
*/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryItemData : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {

	public Item item;
    public int itemID;
    public string itemTitle;
    public int m_amount;
    public int slotID;
    public int shortKey = -1;

    public CustomItemData customData = new CustomItemData();

	[HideInInspector]
	public bool selected;

    [HideInInspector]
    public bool isCombining;

    [HideInInspector]
	public bool isDisabled;

    [HideInInspector]
    public Text textAmount;

    private Inventory inventory;
    private Vector2 offset;
    private bool itemDrag;

	void Start()
	{
        inventory = transform.root.GetComponent<Inventory>();
		transform.position = transform.parent.position;
    }

	void Update()
	{
        textAmount = transform.parent.GetChild(0).GetChild(0).GetComponentInChildren<Text>();

        if (!textAmount || itemDrag) return;

        if (item.itemType == ItemType.Bullets || item.itemType == ItemType.Weapon)
        {
            textAmount.text = m_amount.ToString();
        }
        else
        {
            if (m_amount > 1)
            {
                textAmount.text = m_amount.ToString();
            }
            else if (m_amount == 1)
            {
                textAmount.text = "";
            }
        }

        itemTitle = item.Title;
        itemID = item.ID;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
		if (item != null && !isDisabled)
        {
            itemDrag = true;
            offset = eventData.position - new Vector2(transform.position.x, transform.position.y);
            transform.SetParent(transform.parent.parent.parent);
            transform.position = eventData.position - offset;
			GetComponent<CanvasGroup> ().blocksRaycasts = false;
            inventory.DeselectSelected();
            inventory.isDragging = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
		if (item != null && !isDisabled)
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
		if (!isDisabled) {
			transform.SetParent (inventory.Slots [slotID].transform);
			transform.position = inventory.Slots [slotID].transform.position;
            GetComponent<CanvasGroup> ().blocksRaycasts = true;
            itemDrag = false;
            inventory.DeselectSelected();
            inventory.isDragging = false;
        }
    }

	void OnDisable()
	{
		if(selected)
		inventory.Deselect (slotID);
	}
}

public class CustomItemData
{
    public string itemTag = "";
    public string storedValue = "";
    public string storedTexPath = "";
    public bool canUse = true;
    public bool canCombine = true;
}
