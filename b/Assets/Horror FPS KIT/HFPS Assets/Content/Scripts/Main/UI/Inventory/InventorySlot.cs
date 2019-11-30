/*
 * InventorySlot.cs - script by ThunderWire Games
 * ver. 1.22
*/

using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using ThunderWire.Utility;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	public int slotID;

    [HideInInspector]
    public Inventory inventory;

    [HideInInspector]
    public Item slotItem;

    [HideInInspector]
    public InventoryItemData itemData;

    [HideInInspector]
    public bool isItemSelect;

    [HideInInspector]
	public bool isCombining;

	[HideInInspector]
	public bool isCombinable;

    [HideInInspector]
    public bool isSelected;

    [HideInInspector]
    public bool contexVisible;

    [HideInInspector]
    public bool isTriggered;

    private GameObject ShortcutObj;
    private bool isPressed;
    private bool slotEmpty;

    void Start()
    {
        ShortcutObj = transform.GetChild(0).GetChild(1).gameObject;
    }

    void OnDisable()
    {
        GetComponent<Image>().color = Color.white;
        GetComponent<Image>().sprite = inventory.slotWithItem;
        isTriggered = false;
    }

    void Update()
    {
        if (!inventory) return;

        if (GetComponentInChildren<InventoryItemData>())
        {
            itemData = GetComponentInChildren<InventoryItemData>();
            slotItem = itemData.item;
        }
        else
        {
            itemData = null;
            slotItem = null;
        }

        if (transform.childCount > 1 && itemData)
        {
            transform.GetChild(0).GetComponent<Image>().sprite = inventory.slotItemOutline;
            GetComponent<Image>().enabled = true;
            slotEmpty = false;

            if (Input.GetKeyDown(KeyCode.Mouse1) && !isCombining && isTriggered && !isPressed && !isItemSelect)
            {
                inventory.selectedSlotID = slotID;
                IsClicked();

                if (!contexVisible)
                {
                    contexVisible = true;
                }
                else
                {
                    inventory.ShowContexMenu(false, null);
                    contexVisible = false;
                }

                isPressed = true;
            }
            else if (isPressed)
            {
                isPressed = false;
            }

            if (isSelected && contexVisible)
            {
                if (!slotItem.useItemSwitcher)
                {
                    inventory.ShowContexMenu(true, slotItem, slotID, ctx_use: !inventory.isStoring, ctx_examine: !inventory.isStoring, ctx_combine: inventory.HasCombinePartner(slotItem), ctx_shortcut: !inventory.isStoring, ctx_store: inventory.isStoring);
                }
                else
                {
                    inventory.ShowContexMenu(true, slotItem, slotID, inventory.GetSwitcher().currentItem != slotItem.useSwitcherID && !inventory.isStoring, inventory.HasCombinePartner(slotItem) && !inventory.isStoring, ctx_shortcut: !inventory.isStoring, ctx_store: inventory.isStoring);
                }
            }
            else if(contexVisible)
            {
                inventory.ShowContexMenu(false, null);
                contexVisible = false;
            }

            if (itemData.selected)
            {
                GetComponent<Image>().color = Color.white;
                GetComponent<Image>().sprite = inventory.slotItemSelect;
            }
            else if (!isCombining)
            {
                GetComponent<Image>().color = Color.white;
                GetComponent<Image>().sprite = inventory.slotWithItem;
            }

            if (isCombining)
            {
                itemData.isDisabled = true;
                inventory.ShowContexMenu(false, null);
            }
            else
            {
                itemData.isDisabled = false;
            }

            if (ShortcutObj)
            {
                if(itemData.shortKey >= 0)
                {
                    ShortcutObj.SetActive(true);
                    ShortcutObj.GetComponentInChildren<Text>().text = itemData.shortKey.ToString();
                }
                else
                {
                    ShortcutObj.SetActive(false);
                }
            }
        }
        else if (transform.childCount < 2)
        {
            contexVisible = false;
            transform.GetChild(0).GetComponent<Image>().sprite = inventory.slotsNormal;
            transform.GetChild(0).GetComponent<Image>().color = Color.white;
            transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "";
            GetComponent<Image>().enabled = false;
            ShortcutObj.SetActive(false);

            if (!slotEmpty)
            {
                inventory.ShowContexMenu(false, null);
                slotEmpty = true;
            }
        }

        if (!inventory.contexMenu.activeSelf)
        {
            contexVisible = false;
        }

        if (!isSelected)
        {
            contexVisible = false;
        }

        if (itemData)
        {
            itemData.isCombining = isCombining;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventoryItemData itemDrop = eventData.pointerDrag.GetComponent<InventoryItemData>();
        itemData = itemDrop;

        if (!isCombining)
        {
            if (inventory.Slots[slotID].transform.childCount < 2)
            {
                itemDrop.slotID = slotID;

                if(itemDrop.shortKey != -1)
                {
                    inventory.UpdateShortcut(itemDrop.shortKey, slotID);
                }
            }
            else if (itemDrop.slotID != slotID)
            {
                Transform item = transform.GetChild(1);
                item.GetComponent<InventoryItemData>().slotID = itemDrop.slotID;
                item.transform.SetParent(inventory.Slots[itemDrop.slotID].transform);
                item.transform.position = inventory.Slots[itemDrop.slotID].transform.position;

                if (item.GetComponent<InventoryItemData>().shortKey != -1)
                {
                    inventory.UpdateShortcut(item.GetComponent<InventoryItemData>().shortKey, itemDrop.slotID);
                }

                itemDrop.slotID = slotID;
                itemDrop.transform.SetParent(transform);
                itemDrop.transform.position = transform.position;

                if (itemDrop.shortKey != -1)
                {
                    inventory.UpdateShortcut(itemDrop.shortKey, slotID);
                }
            }
            if (itemDrop.selected)
            {
                inventory.selectedSlotID = itemDrop.slotID;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (transform.childCount > 1 && isTriggered)
        {
            if (isCombinable)
            {
                if (isItemSelect)
                {
                    if (!isSelected)
                    {
                        IsClicked();
                    }
                    else
                    {
                        inventory.ResetSlotProperties();
                        inventory.CombineWith(itemData.item, slotID);
                    }
                }
                else
                {
                    inventory.ResetSlotProperties();
                    inventory.CombineWith(itemData.item, slotID);
                }

            }
            else if(!isCombining)
            {
                IsClicked();
            }
        }
    }

    void ResetSlotGraphic()
    {
        if (slotItem != null)
        {
            if (isCombinable)
            {
                GetComponent<Image>().color = Color.white;
                GetComponent<Image>().sprite = inventory.slotWithItem;
            }
            else
            {
                GetComponent<Image>().color = inventory.slotDisabled;
                GetComponent<Image>().sprite = inventory.slotWithItem;
            }
        }
        else
        {
            transform.GetChild(0).GetComponent<Image>().sprite = inventory.slotsNormal;
            transform.GetChild(0).GetComponent<Image>().color = Color.white;
        }

        isSelected = false;
    }

    void IsClicked()
    {
        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            if (inventory.Slots[i].transform.childCount > 1)
            {
                InventorySlot slot = inventory.Slots[i].GetComponent<InventorySlot>();

                slot.GetComponentInChildren<InventoryItemData>().selected = false;
                slot.GetComponent<InventorySlot>().isSelected = false;

                if (isItemSelect)
                {
                    slot.GetComponent<InventorySlot>().ResetSlotGraphic();
                }
            }
        }
        
        GetComponent<Image>().color = Color.white;
        GetComponent<Image>().sprite = inventory.slotItemSelect;

        inventory.ItemLabel.text = itemData.item.Title;
        string description = itemData.item.Description;

        if (description.RegexMatch('{', '}', "value"))
        {
            if (float.TryParse(itemData.customData.storedValue, out float value))
            {
                inventory.ItemDescription.text = description.RegexReplaceTag('{', '}', "value", Mathf.Round(value).ToString());
            }
            else
            {
                inventory.ItemDescription.text = description.RegexReplaceTag('{', '}', "value", itemData.customData.storedValue);
            }
        }
        else
        {
            inventory.ItemDescription.text = itemData.item.Description;
        }

        inventory.selectedSlotID = slotID;

        inventory.ItemInfoPanel.SetActive(true);

        itemData.selected = true;
        isSelected = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isTriggered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isTriggered = false;
    }
}
